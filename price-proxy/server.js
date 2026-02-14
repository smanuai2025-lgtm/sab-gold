/**
 * 🏆 Gold Price Proxy Server
 * خادم وسيط لجلب أسعار الذهب اللحظية من TradingView مباشرة
 * 
 * يحل مشكلة CORS ويوفر سعر موثوق للذهب
 */

const express = require('express');
const cors = require('cors');
const fetch = require('node-fetch');
const cheerio = require('cheerio');
const rateLimit = require('express-rate-limit');

const app = express();
const PORT = process.env.PORT || 3001;

// تمكين CORS لجميع المصادر
app.use(cors());
app.use(express.json());

// ═══════════════════════════════════════════════════════════════════
// 🛡️ Rate Limiting - حماية من الطلبات المفرطة
// ═══════════════════════════════════════════════════════════════════

// Rate Limiter عام - 100 طلب في الدقيقة لكل IP
const generalLimiter = rateLimit({
    windowMs: 60 * 1000, // دقيقة واحدة
    max: 100, // 100 طلب كحد أقصى
    message: {
        success: false,
        error: 'طلبات كثيرة جداً! الرجاء الانتظار دقيقة.',
        retryAfter: '60 seconds'
    },
    standardHeaders: true,
    legacyHeaders: false,
});

// Rate Limiter للأسعار - 30 طلب في الدقيقة (أكثر صرامة)
const priceLimiter = rateLimit({
    windowMs: 60 * 1000, // دقيقة واحدة
    max: 30, // 30 طلب كحد أقصى للأسعار
    message: {
        success: false,
        error: 'طلبات الأسعار كثيرة! الرجاء الانتظار.',
        retryAfter: '60 seconds'
    },
    standardHeaders: true,
    legacyHeaders: false,
});

// تطبيق Rate Limiter العام
app.use(generalLimiter);

// 🔴 متغيرات الحالة
let cachedPrice = null;
let lastFetchTime = null;
let priceSource = null;
const CACHE_DURATION = 5000; // 5 ثوانٍ فقط للحصول على أحدث سعر

/**
 * 🏆 المصدر الرئيسي: TradingView (مباشرة)
 * جلب السعر من صفحة TradingView
 */
async function fetchFromTradingView() {
    try {
        // 🔴 جلب صفحة سعر الذهب من TradingView
        const response = await fetch('https://www.tradingview.com/symbols/XAUUSD/', {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
                'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
                'Accept-Language': 'en-US,en;q=0.5',
                'Connection': 'keep-alive',
            }
        });

        if (!response.ok) {
            console.warn('⚠️ TradingView response not OK:', response.status);
            return null;
        }

        const html = await response.text();
        const $ = cheerio.load(html);

        // 🔴 البحث عن السعر في عدة أماكن
        let price = null;

        // طريقة 1: البحث في الـ JSON-LD
        const jsonLdScript = $('script[type="application/ld+json"]').text();
        if (jsonLdScript) {
            try {
                const jsonData = JSON.parse(jsonLdScript);
                if (jsonData.price) {
                    price = parseFloat(jsonData.price);
                }
            } catch (e) { }
        }

        // طريقة 2: البحث في العناصر المعروفة
        if (!price) {
            const priceSelectors = [
                '[data-symbol-last]',
                '.tv-symbol-price-quote__value',
                '.js-symbol-last',
                '[class*="lastPrice"]',
                '[class*="price-"]'
            ];

            for (const selector of priceSelectors) {
                const el = $(selector).first();
                if (el.length) {
                    const text = el.attr('data-symbol-last') || el.text();
                    const match = text.match(/[\d,]+\.\d{2,3}/);
                    if (match) {
                        price = parseFloat(match[0].replace(/,/g, ''));
                        if (price > 2000 && price < 10000) break;
                    }
                }
            }
        }

        // طريقة 3: البحث في كل النص عن أنماط سعر الذهب
        if (!price) {
            const bodyText = $('body').text();
            const priceMatches = bodyText.match(/\b([4-6],?\d{3}\.\d{2,3})\b/g);
            if (priceMatches) {
                for (const match of priceMatches) {
                    const p = parseFloat(match.replace(/,/g, ''));
                    if (p > 2000 && p < 10000) {
                        price = p;
                        break;
                    }
                }
            }
        }

        if (price && price > 2000 && price < 10000) {
            console.log('📊 TradingView Direct Price:', price);
            return price;
        }
    } catch (e) {
        console.warn('⚠️ TradingView fetch failed:', e.message);
    }
    return null;
}

/**
 * 🏆 المصدر الأول: TradingView Scanner API
 * محاولة جلب OANDA:XAUUSD من عدة أسواق
 */
async function fetchFromTradingViewAPI() {
    // 🔴 محاولة 1: سوق CFD
    const markets = ['cfd', 'forex', 'crypto'];

    for (const market of markets) {
        try {
            const response = await fetch(`https://scanner.tradingview.com/${market}/scan`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
                    'Origin': 'https://www.tradingview.com'
                },
                body: JSON.stringify({
                    symbols: { tickers: ['OANDA:XAUUSD', 'TVC:GOLD', 'FOREXCOM:XAUUSD', 'FX:XAUUSD'] },
                    columns: ['close', 'open', 'high', 'low', 'change', 'change_abs']
                })
            });

            if (response.ok) {
                const data = await response.json();
                if (data.data && data.data.length > 0) {
                    for (const item of data.data) {
                        const price = item?.d?.[0];
                        if (price && price > 2000 && price < 10000) {
                            const symbol = item?.s || market;
                            console.log(`📊 TradingView ${symbol}: $${price}`);
                            return price;
                        }
                    }
                }
            }
        } catch (e) {
            // تجربة السوق التالي
        }
    }

    // 🔴 محاولة 2: TradingView Quotes API
    try {
        const response = await fetch('https://quotes-api.tradingview.com/quotes?symbols=OANDA:XAUUSD', {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
                'Accept': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            if (data?.quotes?.[0]?.lp) {
                const price = data.quotes[0].lp;
                if (price > 2000 && price < 10000) {
                    console.log('📊 TradingView Quotes API:', price);
                    return price;
                }
            }
        }
    } catch (e) {
        console.warn('⚠️ Quotes API failed:', e.message);
    }

    return null;
}

/**
 * 🏆 المصدر 2: TradingView OANDA Quote Page
 * جلب السعر من صفحة OANDA:XAUUSD مباشرة
 */
async function fetchFromTradingViewQuote() {
    // 🔴 روابط مختلفة لـ OANDA:XAUUSD
    const urls = [
        'https://www.tradingview.com/symbols/OANDA-XAUUSD/',
        'https://ar.tradingview.com/symbols/OANDA-XAUUSD/',
        'https://www.tradingview.com/chart/?symbol=OANDA:XAUUSD'
    ];

    for (const url of urls) {
        try {
            const response = await fetch(url, {
                headers: {
                    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
                    'Accept': 'text/html,application/xhtml+xml',
                    'Accept-Language': 'en-US,en;q=0.9,ar;q=0.8',
                }
            });

            if (!response.ok) continue;

            const html = await response.text();

            // 🔴 البحث عن السعر في JSON المضمن
            const jsonPatterns = [
                /\"last(?:_price)?\":\s*(\d+\.?\d*)/g,
                /\"close\":\s*(\d+\.?\d*)/g,
                /\"lp\":\s*(\d+\.?\d*)/g,
                /\"price\":\s*(\d+\.?\d*)/g
            ];

            for (const pattern of jsonPatterns) {
                let match;
                while ((match = pattern.exec(html)) !== null) {
                    const price = parseFloat(match[1]);
                    if (price > 4500 && price < 6000) { // نطاق سعر الذهب الحالي
                        console.log(`📊 TradingView OANDA Page: $${price}`);
                        return price;
                    }
                }
            }

            // 🔴 البحث في النص المرئي
            const $ = cheerio.load(html);
            const bodyText = $('body').text();

            // البحث عن أنماط السعر المحددة
            const priceMatches = bodyText.match(/\b5[,.]?\d{3}\.\d{2,3}\b/g);
            if (priceMatches) {
                for (const match of priceMatches) {
                    const p = parseFloat(match.replace(/,/g, ''));
                    if (p > 4500 && p < 6000) {
                        console.log(`📊 TradingView Text: $${p}`);
                        return p;
                    }
                }
            }
        } catch (e) {
            // تجربة الرابط التالي
        }
    }

    return null;
}

/**
 * 🥇 المصدر 3: Kitco (الأكثر موثوقية للذهب)
 */
async function fetchFromKitco() {
    try {
        const response = await fetch('https://www.kitco.com/gold-price-today-usa/', {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
            }
        });

        if (!response.ok) return null;

        const html = await response.text();
        const $ = cheerio.load(html);

        // البحث عن سعر الذهب الفوري
        const priceText = $('[data-field="gold-spot-price"]').text() ||
            $('.price-value').first().text() ||
            $('span:contains("$")').filter((i, el) => {
                const text = $(el).text();
                return /\$[\d,]+\.\d{2}/.test(text);
            }).first().text();

        const match = priceText.match(/[\d,]+\.\d{2}/);
        if (match) {
            const price = parseFloat(match[0].replace(/,/g, ''));
            if (price > 2000 && price < 10000) {
                console.log('📊 Kitco Price:', price);
                return price;
            }
        }
    } catch (e) {
        console.warn('⚠️ Kitco failed:', e.message);
    }
    return null;
}

/**
 * 🥈 المصدر 2: Gold Price (goldprice.org)
 */
async function fetchFromGoldPrice() {
    try {
        const response = await fetch('https://goldprice.org/', {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
            }
        });

        if (!response.ok) return null;

        const html = await response.text();
        const $ = cheerio.load(html);

        // البحث عن سعر الأونصة
        const priceText = $('#gpxtickerLeft_price').text() ||
            $('.price').first().text() ||
            $('span:contains("$")').filter((i, el) => {
                const text = $(el).text();
                return /[\d,]+\.\d{2}/.test(text);
            }).first().text();

        const match = priceText.match(/[\d,]+\.\d{2}/);
        if (match) {
            const price = parseFloat(match[0].replace(/,/g, ''));
            if (price > 2000 && price < 10000) {
                console.log('📊 GoldPrice.org:', price);
                return price;
            }
        }
    } catch (e) {
        console.warn('⚠️ GoldPrice failed:', e.message);
    }
    return null;
}

/**
 * 🥉 المصدر 3: Metals API (مجاني)
 */
async function fetchFromMetalsAPI() {
    try {
        // استخدام API مجاني
        const response = await fetch('https://api.metals.live/v1/spot/gold');

        if (!response.ok) return null;

        const data = await response.json();
        if (Array.isArray(data) && data.length > 0) {
            const price = data[0]?.price;
            if (price && price > 2000 && price < 10000) {
                console.log('📊 Metals.live:', price);
                return price;
            }
        }
    } catch (e) {
        console.warn('⚠️ Metals API failed:', e.message);
    }
    return null;
}

/**
 * 🏅 المصدر 4: Yahoo Finance
 */
async function fetchFromYahoo() {
    try {
        const response = await fetch('https://query1.finance.yahoo.com/v8/finance/chart/GC=F?interval=1m&range=1d', {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
            }
        });

        if (!response.ok) return null;

        const data = await response.json();
        const price = data?.chart?.result?.[0]?.meta?.regularMarketPrice;

        if (price && price > 2000 && price < 10000) {
            console.log('📊 Yahoo Finance:', price);
            return price;
        }
    } catch (e) {
        console.warn('⚠️ Yahoo failed:', e.message);
    }
    return null;
}

/**
 * 🔄 جلب السعر من جميع المصادر
 * الأولوية: TradingView OANDA:XAUUSD أولاً (نفس Widget)
 */
async function fetchGoldPrice() {
    // التحقق من الـ cache (5 ثوانٍ فقط)
    if (cachedPrice && lastFetchTime && (Date.now() - lastFetchTime < CACHE_DURATION)) {
        return {
            price: cachedPrice,
            source: priceSource,
            cached: true,
            timestamp: lastFetchTime
        };
    }

    console.log('🔄 Fetching OANDA:XAUUSD from TradingView...');

    // 🥇 الأولوية 1: TradingView Scanner API (OANDA:XAUUSD)
    let price = await fetchFromTradingViewAPI();
    if (price) {
        priceSource = 'TradingView OANDA:XAUUSD';
    }

    // 🥈 الأولوية 2: TradingView Quote Page
    if (!price) {
        price = await fetchFromTradingViewQuote();
        if (price) priceSource = 'TradingView Quote';
    }

    // 🥉 الأولوية 3: TradingView صفحة عامة
    if (!price) {
        price = await fetchFromTradingView();
        if (price) priceSource = 'TradingView Web';
    }

    // 🏅 الأولوية 4: المصادر البديلة
    if (!price) {
        console.log('⚠️ TradingView unavailable, trying alternatives...');

        const results = await Promise.allSettled([
            fetchFromYahoo(),
            fetchFromMetalsAPI(),
            fetchFromKitco(),
            fetchFromGoldPrice()
        ]);

        for (const result of results) {
            if (result.status === 'fulfilled' && result.value) {
                price = result.value;
                priceSource = 'Alternative';
                break;
            }
        }
    }

    if (price && price > 2000 && price < 10000) {
        // تقريب لأقرب سنت
        const finalPrice = Math.round(price * 100) / 100;

        // تحديث الـ cache
        cachedPrice = finalPrice;
        lastFetchTime = Date.now();

        console.log(`✅ Price: $${finalPrice} (${priceSource})`);

        return {
            price: finalPrice,
            source: priceSource,
            cached: false,
            timestamp: lastFetchTime
        };
    }

    // إذا فشلت جميع المصادر
    if (cachedPrice) {
        return {
            price: cachedPrice,
            source: priceSource,
            cached: true,
            stale: true,
            timestamp: lastFetchTime
        };
    }

    return {
        error: 'Failed to fetch price',
        price: null
    };
}

// ═══════════════════════════════════════════════════════════════════
// API Endpoints
// ═══════════════════════════════════════════════════════════════════

/**
 * 📊 GET /api/gold/price
 * جلب سعر الذهب الحالي
 */
app.get('/api/gold/price', priceLimiter, async (req, res) => {
    try {
        const result = await fetchGoldPrice();

        if (result.price) {
            res.json({
                success: true,
                data: {
                    ounce: result.price,
                    gram: result.price / 31.1035,
                    kilo: (result.price / 31.1035) * 1000,
                    currency: 'USD',
                    source: result.source || 'Unknown',
                    cached: result.cached || false,
                    timestamp: result.timestamp || Date.now()
                }
            });
        } else {
            res.status(503).json({
                success: false,
                error: result.error || 'Unable to fetch gold price'
            });
        }
    } catch (error) {
        console.error('❌ Error:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * 🇰🇼 GET /api/gold/kuwait
 * جلب سعر الذهب بالدينار الكويتي
 */
app.get('/api/gold/kuwait', priceLimiter, async (req, res) => {
    try {
        const result = await fetchGoldPrice();

        if (!result.price) {
            return res.status(503).json({
                success: false,
                error: 'Unable to fetch gold price'
            });
        }

        // إعدادات الحساب (يمكن تخصيصها من Query Parameters)
        const exchangeRate = parseFloat(req.query.rate) || 0.3075;
        const commission = parseFloat(req.query.commission) || 1.5;

        const ounceUSD = result.price;
        const gramUSD = ounceUSD / 31.1035;

        // حساب أسعار الكويت لجميع العيارات
        const karats = {
            '24': 0.999,
            '22': 0.916,
            '21': 0.875,
            '18': 0.750
        };

        const prices = {};
        for (const [karat, purity] of Object.entries(karats)) {
            const gramKWD = gramUSD * purity * exchangeRate;
            const commissionAmount = gramKWD * commission / 100;
            const sellPrice = gramKWD + commissionAmount;
            const buyPrice = sellPrice * 0.985; // فرق 1.5% للشراء

            prices[karat] = {
                sell: Math.round(sellPrice * 1000) / 1000,
                buy: Math.round(buyPrice * 1000) / 1000
            };
        }

        res.json({
            success: true,
            data: {
                ounce: {
                    usd: ounceUSD,
                    kwd: Math.round(ounceUSD * exchangeRate * 100) / 100
                },
                gram: prices,
                settings: {
                    exchangeRate,
                    commission
                },
                cached: result.cached || false,
                timestamp: result.timestamp || Date.now()
            }
        });
    } catch (error) {
        console.error('❌ Error:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * 💓 GET /health
 * فحص صحة الخادم
 */
app.get('/health', (req, res) => {
    res.json({
        status: 'ok',
        timestamp: Date.now(),
        cachedPrice: cachedPrice,
        lastFetch: lastFetchTime
    });
});

/**
 * 🏠 GET /
 * الصفحة الرئيسية
 */
app.get('/', (req, res) => {
    res.json({
        name: 'Gold Price Proxy Server',
        version: '1.0.0',
        endpoints: {
            price: 'GET /api/gold/price',
            kuwait: 'GET /api/gold/kuwait?rate=0.3075&commission=1.5',
            health: 'GET /health'
        }
    });
});

// ═══════════════════════════════════════════════════════════════════
// بدء الخادم
// ═══════════════════════════════════════════════════════════════════

app.listen(PORT, () => {
    console.log('═══════════════════════════════════════════════════════════');
    console.log('🏆 Gold Price Proxy Server');
    console.log('═══════════════════════════════════════════════════════════');
    console.log(`📡 Server running on http://localhost:${PORT}`);
    console.log(`📊 Price endpoint: http://localhost:${PORT}/api/gold/price`);
    console.log(`🇰🇼 Kuwait endpoint: http://localhost:${PORT}/api/gold/kuwait`);
    console.log('═══════════════════════════════════════════════════════════');

    // جلب السعر فوراً عند البدء
    fetchGoldPrice().then(result => {
        if (result.price) {
            console.log(`✅ Initial price: $${result.price}`);
        }
    });
});
