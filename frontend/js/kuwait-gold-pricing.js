/**
 * ═══════════════════════════════════════════════════════════════════════════════
 * 🇰🇼 نظام حساب سعر الذهب الكويتي - دار السبائك
 * Kuwait Gold Pricing System - Dar Al-Sabaaek Style
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * المعادلة الأساسية:
 * سعر الجرام = (سعر الأونصة العالمي ÷ 31.1035) × سعر صرف الدولار + العمولة
 * 
 * - سعر الأونصة: لحظي من TradingView
 * - سعر الصرف: قيمة مخزنة (قابلة للتعديل من لوحة التحكم)
 * - العمولة: نسبة مئوية أو قيمة ثابتة (قابلة للتعديل)
 * 
 * ═══════════════════════════════════════════════════════════════════════════════
 */

const KuwaitGoldPricing = {
    // ═══════════════════════════════════════════════════════════════════
    // الإعدادات الافتراضية
    // ═══════════════════════════════════════════════════════════════════
    
    STORAGE_KEY: 'kuwait_gold_settings',
    
    // الإعدادات الافتراضية (تُحمّل من localStorage أو لوحة التحكم)
    settings: {
        exchangeRate: 0.3075,           // سعر صرف الدولار مقابل الدينار
        commissionPercent: 1.5,         // نسبة العمولة/المصنعية (%)
        commissionFixed: 0,             // عمولة ثابتة (د.ك) - إضافية
        refreshInterval: 14,            // فترة التحديث بالثواني (كما في دار السبائك)
        spreadBuySell: 1.5,             // الفرق بين سعر الشراء والبيع (%)
    },
    
    // نسب نقاء العيارات
    PURITY: {
        '24': 0.999,    // 99.9% نقاء
        '22': 0.916,    // 91.6% نقاء
        '21': 0.875,    // 87.5% نقاء
        '18': 0.750,    // 75.0% نقاء
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // حالة النظام
    // ═══════════════════════════════════════════════════════════════════
    
    state: {
        currentOuncePrice: 0,           // سعر الأونصة الحالي (USD)
        previousOuncePrice: 0,          // سعر الأونصة السابق (للمقارنة)
        lastUpdate: null,               // وقت آخر تحديث
        isRunning: false,               // هل النظام يعمل
        intervalId: null,               // معرّف الـ interval
        priceHistory: [],               // تاريخ الأسعار (آخر 100 قيمة)
        listeners: [],                  // المستمعين للتحديثات
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // التهيئة
    // ═══════════════════════════════════════════════════════════════════
    
    init() {
        console.log('🇰🇼 تهيئة نظام أسعار الذهب الكويتي...');
        
        // تحميل الإعدادات المحفوظة
        this.loadSettings();
        
        // بدء مراقبة الأسعار
        this.startPriceMonitoring();
        
        console.log('✅ نظام الأسعار جاهز');
        console.log('📊 سعر الصرف:', this.settings.exchangeRate, 'د.ك/$');
        console.log('💰 العمولة:', this.settings.commissionPercent, '%');
        
        return this;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إدارة الإعدادات
    // ═══════════════════════════════════════════════════════════════════
    
    loadSettings() {
        try {
            const saved = localStorage.getItem(this.STORAGE_KEY);
            if (saved) {
                const parsed = JSON.parse(saved);
                this.settings = { ...this.settings, ...parsed };
                console.log('📂 تم تحميل الإعدادات المحفوظة');
            }
        } catch (e) {
            console.warn('⚠️ فشل تحميل الإعدادات، استخدام الافتراضية');
        }
    },
    
    saveSettings() {
        try {
            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.settings));
            console.log('💾 تم حفظ الإعدادات');
        } catch (e) {
            console.error('❌ فشل حفظ الإعدادات');
        }
    },
    
    updateSettings(newSettings) {
        this.settings = { ...this.settings, ...newSettings };
        this.saveSettings();
        
        // إعادة تشغيل المراقبة بالفترة الجديدة
        if (this.state.isRunning) {
            this.stopPriceMonitoring();
            this.startPriceMonitoring();
        }
        
        // إشعار المستمعين بالتحديث
        this.notifyListeners();
        
        console.log('🔄 تم تحديث الإعدادات');
    },
    
    getSettings() {
        return { ...this.settings };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 🔥 المعادلة الأساسية - حساب سعر الذهب الكويتي
    // ═══════════════════════════════════════════════════════════════════
    
    /**
     * حساب سعر الجرام بالدينار الكويتي
     * @param {number} ouncePrice - سعر الأونصة بالدولار
     * @param {string} karat - العيار ('24', '22', '21', '18')
     * @returns {object} - { buyPrice, sellPrice, basePrice }
     */
    calculateGramPrice(ouncePrice, karat = '24') {
        if (!ouncePrice || ouncePrice <= 0) {
            return { buyPrice: 0, sellPrice: 0, basePrice: 0 };
        }
        
        const purity = this.PURITY[String(karat)] || this.PURITY['24'];
        const { exchangeRate, commissionPercent, commissionFixed, spreadBuySell } = this.settings;
        
        // الخطوة 1: تحويل سعر الأونصة إلى سعر الجرام بالدولار
        // (الأونصة = 31.1035 جرام)
        const gramUSD = ouncePrice / 31.1035;
        
        // الخطوة 2: ضرب في نسبة النقاء للعيار
        const pureGramUSD = gramUSD * purity;
        
        // الخطوة 3: تحويل إلى الدينار الكويتي
        const gramKWD = pureGramUSD * exchangeRate;
        
        // الخطوة 4: إضافة العمولة/المصنعية
        const commission = (gramKWD * commissionPercent / 100) + commissionFixed;
        const sellPrice = gramKWD + commission;
        
        // الخطوة 5: حساب سعر الشراء (أقل من سعر البيع)
        const buyPrice = sellPrice * (1 - spreadBuySell / 100);
        
        return {
            basePrice: gramKWD,         // السعر الأساسي بدون عمولة
            sellPrice: sellPrice,       // سعر البيع (ما يدفعه العميل)
            buyPrice: buyPrice,         // سعر الشراء (ما يحصل عليه العميل)
            commission: commission,     // قيمة العمولة
            purity: purity,
            karat: karat
        };
    },
    
    /**
     * حساب سعر الكيلو بالدينار الكويتي
     */
    calculateKiloPrice(ouncePrice, karat = '24') {
        const gram = this.calculateGramPrice(ouncePrice, karat);
        return {
            basePrice: gram.basePrice * 1000,
            sellPrice: gram.sellPrice * 1000,
            buyPrice: gram.buyPrice * 1000,
            commission: gram.commission * 1000,
            purity: gram.purity,
            karat: karat
        };
    },
    
    /**
     * حساب جميع العيارات دفعة واحدة
     */
    calculateAllKarats(ouncePrice) {
        const karats = ['24', '22', '21', '18'];
        const prices = {};
        
        karats.forEach(karat => {
            prices[karat] = this.calculateGramPrice(ouncePrice, karat);
        });
        
        return prices;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // مراقبة الأسعار (Snapshot كل X ثانية)
    // ═══════════════════════════════════════════════════════════════════
    
    startPriceMonitoring() {
        if (this.state.isRunning) return;
        
        this.state.isRunning = true;
        console.log(`🔄 بدء مراقبة الأسعار (كل ${this.settings.refreshInterval} ثانية)`);
        
        // تحديث فوري
        this.fetchAndUpdatePrice();
        
        // تحديث دوري كل 14 ثانية
        this.state.intervalId = setInterval(async () => {
            await this.fetchAndUpdatePrice();
        }, this.settings.refreshInterval * 1000);
    },
    
    stopPriceMonitoring() {
        if (this.state.intervalId) {
            clearInterval(this.state.intervalId);
            this.state.intervalId = null;
        }
        this.state.isRunning = false;
        console.log('⏹️ إيقاف مراقبة الأسعار');
    },
    
    /**
     * جلب السعر من TradingView وتحديث الحالة
     */
    async fetchAndUpdatePrice() {
        // حفظ السعر السابق للمقارنة
        this.state.previousOuncePrice = this.state.currentOuncePrice;
        
        // محاولة جلب السعر من مصادر متعددة
        let newPrice = 0;
        
        // 🔴 المصدر 1: جلب من الخادم الوسيط (Proxy Server)
        try {
            newPrice = await this.fetchFromProxyServer();
        } catch (e) {
            console.warn('⚠️ فشل جلب السعر من Proxy:', e.message);
        }
        
        // 🔴 المصدر 2: قراءة من widget المضمن
        if (!newPrice || newPrice <= 0) {
            newPrice = this.getOuncePriceFromTradingView();
        }
        
        // 🔴 المصدر 3: localStorage كمصدر احتياطي
        if (!newPrice || newPrice <= 0) {
            newPrice = this.getOuncePriceFromLocalStorage();
        }
        
        if (!newPrice || newPrice <= 0) {
            console.warn('⚠️ لم يتم العثور على سعر الأونصة');
            return;
        }
        
        // تحديث الحالة
        this.state.currentOuncePrice = newPrice;
        this.state.lastUpdate = new Date();
        
        // تحديث window.currentGoldPrice للصفحات الأخرى
        window.currentGoldPrice = newPrice;
        
        // إضافة للتاريخ
        this.state.priceHistory.push({
            price: newPrice,
            time: this.state.lastUpdate
        });
        
        // الاحتفاظ بآخر 100 قيمة فقط
        if (this.state.priceHistory.length > 100) {
            this.state.priceHistory.shift();
        }
        
        // حفظ في localStorage للصفحات الأخرى
        localStorage.setItem('current_gold_ounce_price', newPrice.toString());
        
        // إشعار المستمعين
        this.notifyListeners();
        
        console.log(`💰 سعر الأونصة: $${newPrice.toFixed(2)} | 24K: ${this.calculateGramPrice(newPrice, '24').sellPrice.toFixed(3)} د.ك`);
    },
    
    /**
     * 🔴 جلب السعر من الخادم الوسيط (Proxy Server)
     * يتجاوز مشكلة CORS ويجلب السعر الحقيقي
     */
    async fetchFromProxyServer() {
        // 🔴 عناوين الخادم الوسيط (محلي أو production)
        const proxyUrls = [
            'http://localhost:3001/api/gold/price',           // محلي
            '/api/gold/price',                                 // نفس المجال
            'https://sabeekagold-proxy.onrender.com/api/gold/price' // production
        ];
        
        for (const url of proxyUrls) {
            try {
                const response = await fetch(url, { 
                    timeout: 5000,
                    headers: { 'Accept': 'application/json' }
                });
                
                if (response.ok) {
                    const data = await response.json();
                    if (data.success && data.data?.ounce > 2000) {
                        console.log(`📊 Proxy Server Price: $${data.data.ounce}`);
                        return data.data.ounce;
                    }
                }
            } catch (e) {
                // محاولة العنوان التالي
                continue;
            }
        }
        
        return 0;
    },
    
    /**
     * 🔴 جلب السعر من TradingView widget مباشرة
     * يقرأ السعر الفعلي من نافذة TradingView
     */
    getOuncePriceFromTradingView() {
        // المصدر 1: App.state.tradingViewPrice (السعر المُقروء من TradingView)
        if (typeof App !== 'undefined' && App.state && App.state.tradingViewPrice > 0) {
            return App.state.tradingViewPrice;
        }
        
        // المصدر 2: window.currentGoldPrice
        if (window.currentGoldPrice && window.currentGoldPrice > 0) {
            return window.currentGoldPrice;
        }
        
        // المصدر 3: البحث المباشر في TradingView widget
        const tvPrice = this.readTradingViewWidget();
        if (tvPrice > 0) {
            return tvPrice;
        }
        
        // المصدر 4: قراءة من عنصر العرض
        const priceEl = document.getElementById('global-ounce-price');
        if (priceEl) {
            const text = priceEl.textContent.replace(/[^0-9.]/g, '');
            const price = parseFloat(text);
            if (price > 0) return price;
        }
        
        return 0;
    },
    
    /**
     * 🔴 قراءة السعر مباشرة من TradingView Widget
     * يبحث في DOM عن السعر الظاهر في الـ widget
     */
    readTradingViewWidget() {
        try {
            // البحث عن جميع iframes الخاصة بـ TradingView
            const iframes = document.querySelectorAll('iframe[src*="tradingview"]');
            
            // البحث في النص الظاهر في الصفحة
            const allText = document.body.innerText || '';
            
            // البحث عن أنماط سعر الذهب (2,XXX.XX أو 4,XXX.XX أو XXXX.XX)
            // سعر الذهب عادة بين 1500 و 10000 دولار
            const patterns = [
                /(\d{1,2},\d{3}\.\d{2})/g,  // 2,750.50 أو 4,836.46
                /(\d{4}\.\d{2})/g,          // 2750.50
            ];
            
            for (const pattern of patterns) {
                const matches = allText.match(pattern);
                if (matches) {
                    for (const match of matches) {
                        const cleanNum = match.replace(/,/g, '');
                        const price = parseFloat(cleanNum);
                        // سعر الذهب المعقول (بين 2000 و 10000) - يناير 2026: ~$5000
                        if (price >= 2000 && price <= 10000) {
                            return price;
                        }
                    }
                }
            }
            
            return 0;
        } catch (e) {
            console.warn('⚠️ خطأ في قراءة TradingView:', e);
            return 0;
        }
    },
    
    /**
     * جلب السعر من localStorage (كمصدر احتياطي)
     */
    getOuncePriceFromLocalStorage() {
        const saved = localStorage.getItem('current_gold_ounce_price');
        if (saved) {
            const price = parseFloat(saved);
            if (price > 0) return price;
        }
        return 0;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // نظام الاشتراك (للتحديثات التلقائية)
    // ═══════════════════════════════════════════════════════════════════
    
    /**
     * الاشتراك في تحديثات الأسعار
     * @param {function} callback - الدالة التي تُستدعى عند التحديث
     * @returns {function} - دالة إلغاء الاشتراك
     */
    subscribe(callback) {
        if (typeof callback !== 'function') return () => {};
        
        this.state.listeners.push(callback);
        
        // إرسال القيمة الحالية فوراً
        if (this.state.currentOuncePrice > 0) {
            callback(this.getPriceData());
        }
        
        // إرجاع دالة إلغاء الاشتراك
        return () => {
            const index = this.state.listeners.indexOf(callback);
            if (index > -1) {
                this.state.listeners.splice(index, 1);
            }
        };
    },
    
    /**
     * إشعار جميع المستمعين بالتحديث
     */
    notifyListeners() {
        const data = this.getPriceData();
        this.state.listeners.forEach(callback => {
            try {
                callback(data);
            } catch (e) {
                console.error('خطأ في مستمع الأسعار:', e);
            }
        });
    },
    
    /**
     * الحصول على بيانات السعر الحالية
     */
    getPriceData() {
        const ounce = this.state.currentOuncePrice;
        const prevOunce = this.state.previousOuncePrice;
        const prices = this.calculateAllKarats(ounce);
        
        // حساب التغير
        const change = ounce - prevOunce;
        const changePercent = prevOunce > 0 ? (change / prevOunce) * 100 : 0;
        const direction = change > 0 ? 'up' : change < 0 ? 'down' : 'stable';
        
        return {
            ounce: {
                current: ounce,
                previous: prevOunce,
                change: change,
                changePercent: changePercent,
                direction: direction
            },
            prices: prices,
            settings: this.getSettings(),
            lastUpdate: this.state.lastUpdate,
            history: this.state.priceHistory.slice(-20) // آخر 20 قيمة
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // دوال مساعدة للعرض
    // ═══════════════════════════════════════════════════════════════════
    
    /**
     * تنسيق السعر للعرض
     */
    formatPrice(price, decimals = 3) {
        if (!price || isNaN(price)) return '--';
        return price.toFixed(decimals);
    },
    
    /**
     * الحصول على لون التغير
     */
    getChangeColor(direction) {
        switch (direction) {
            case 'up': return { text: 'text-green-400', bg: 'bg-green-500/20', arrow: '▲' };
            case 'down': return { text: 'text-red-400', bg: 'bg-red-500/20', arrow: '▼' };
            default: return { text: 'text-yellow-400', bg: 'bg-yellow-500/20', arrow: '●' };
        }
    },
    
    /**
     * الحصول على إحصائيات من التاريخ
     */
    getStatistics() {
        const history = this.state.priceHistory;
        if (history.length === 0) return null;
        
        const prices = history.map(h => h.price);
        const currentKWD = this.calculateGramPrice(this.state.currentOuncePrice, '24').sellPrice;
        
        // تحويل كل الأسعار إلى KWD
        const pricesKWD = prices.map(p => this.calculateGramPrice(p, '24').sellPrice);
        
        return {
            high: Math.max(...pricesKWD),
            low: Math.min(...pricesKWD),
            average: pricesKWD.reduce((a, b) => a + b, 0) / pricesKWD.length,
            current: currentKWD,
            count: history.length
        };
    }
};

// ═══════════════════════════════════════════════════════════════════════════════
// 🎨 مكون تحديث العرض التلقائي
// ═══════════════════════════════════════════════════════════════════════════════

const KuwaitGoldDisplay = {
    /**
     * تحديث جميع عناصر العرض في الصفحة
     */
    updateAllDisplays(data) {
        if (!data || !data.prices) return;
        
        const { ounce, prices } = data;
        const colors = KuwaitGoldPricing.getChangeColor(ounce.direction);
        
        // تحديث سعر الأونصة
        this.updateElement('global-ounce-price', `$${ounce.current.toFixed(2)}`);
        
        // تحديث أسعار جميع العيارات
        ['24', '22', '21', '18'].forEach(karat => {
            const price = prices[karat];
            if (!price) return;
            
            // سعر البيع
            this.updateElement(`kw-${karat}k-price`, `${price.sellPrice.toFixed(3)} د.ك`, colors);
            this.updateElement(`karat-${karat}-price`, price.sellPrice.toFixed(3), colors);
            
            // سعر الشراء
            this.updateElement(`kw-${karat}k-buy`, `${price.buyPrice.toFixed(3)} د.ك`);
            
            // السهم
            this.updateArrow(`kw-${karat}k-change`, ounce.changePercent, colors);
            this.updateArrow(`karat-${karat}-arrow`, ounce.changePercent, colors);
        });
        
        // تحديث العناصر الرئيسية (عيار 24)
        const price24 = prices['24'];
        if (price24) {
            this.updateElement('hero-price-value', price24.sellPrice.toFixed(3), colors);
            this.updateElement('main-kw-price', `${price24.sellPrice.toFixed(3)} د.ك`, colors);
            this.updateElement('desktop-hero-price-value', price24.sellPrice.toFixed(3), colors);
            this.updateElement('desktop-chart-price', `${price24.sellPrice.toFixed(3)} د.ك`, colors);
            
            // تحديث نسبة التغير
            this.updateArrow('hero-price-change', ounce.changePercent, colors);
            this.updateArrow('desktop-hero-price-change', ounce.changePercent, colors);
        }
        
        // تحديث الإحصائيات
        const stats = KuwaitGoldPricing.getStatistics();
        if (stats) {
            this.updateElement('price-high', `${stats.high.toFixed(3)} د.ك`);
            this.updateElement('price-low', `${stats.low.toFixed(3)} د.ك`);
            this.updateElement('price-average', `${stats.average.toFixed(3)} د.ك`);
        }
        
        // تحديث وقت التحديث
        if (data.lastUpdate) {
            const timeStr = data.lastUpdate.toLocaleTimeString('ar-KW');
            this.updateElement('last-update-time', timeStr);
            this.updateElement('price-update-time', timeStr);
        }
    },
    
    /**
     * تحديث عنصر مع تأثير بصري
     */
    updateElement(id, value, colors = null) {
        const el = document.getElementById(id);
        if (!el) return;
        
        const oldValue = el.textContent;
        el.textContent = value;
        
        // تأثير الوميض عند التغير
        if (oldValue !== value && colors) {
            el.classList.add('price-flash', colors.text);
            setTimeout(() => {
                el.classList.remove('price-flash');
            }, 500);
        }
    },
    
    /**
     * تحديث سهم التغير
     */
    updateArrow(id, changePercent, colors) {
        const el = document.getElementById(id);
        if (!el) return;
        
        const arrow = colors.arrow;
        const percent = Math.abs(changePercent).toFixed(2);
        
        el.innerHTML = `<span class="${colors.text}">${arrow} ${percent}%</span>`;
    },
    
    /**
     * إضافة CSS للتأثيرات البصرية
     */
    injectStyles() {
        if (document.getElementById('kuwait-gold-styles')) return;
        
        const style = document.createElement('style');
        style.id = 'kuwait-gold-styles';
        style.textContent = `
            @keyframes priceFlash {
                0%, 100% { opacity: 1; }
                50% { opacity: 0.5; transform: scale(1.02); }
            }
            
            .price-flash {
                animation: priceFlash 0.3s ease-out;
            }
            
            .price-up {
                color: #4ade80 !important;
                text-shadow: 0 0 10px rgba(74, 222, 128, 0.3);
            }
            
            .price-down {
                color: #f87171 !important;
                text-shadow: 0 0 10px rgba(248, 113, 113, 0.3);
            }
        `;
        document.head.appendChild(style);
    }
};

// ═══════════════════════════════════════════════════════════════════════════════
// التهيئة التلقائية عند تحميل الصفحة
// ═══════════════════════════════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', () => {
    // إضافة الأنماط
    KuwaitGoldDisplay.injectStyles();
    
    // تهيئة نظام الأسعار
    KuwaitGoldPricing.init();
    
    // الاشتراك في التحديثات لتحديث العرض
    KuwaitGoldPricing.subscribe((data) => {
        KuwaitGoldDisplay.updateAllDisplays(data);
    });
});

// تصدير للاستخدام الخارجي
window.KuwaitGoldPricing = KuwaitGoldPricing;
window.KuwaitGoldDisplay = KuwaitGoldDisplay;
