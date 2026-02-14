/**
 * ═══════════════════════════════════════════════════════════════════
 * Mr. Golden Bader - Main Application
 * ═══════════════════════════════════════════════════════════════════
 * 🔴 السعر العالمي: من TradingView فقط (الأكثر دقة)
 * 🔴 أسعار الكويت: محسوبة محلياً من السعر العالمي
 * ═══════════════════════════════════════════════════════════════════
 */

const App = {
    // State
    state: {
        globalPrice: null,
        previousGlobalPrice: null,
        kuwaitPrices: [],
        previousKuwaitPrices: {},
        recommendation: null,
        newsImpact: null,
        alerts: [],
        unreadAlerts: 0,
        countdown: 4, // العد التنازلي
        tradingViewPrice: null // السعر الحقيقي من TradingView
    },

    /**
     * Initialize application
     */
    async init() {
        console.log('🚀 Initializing Mr. Golden Bader...');

        // Update time
        this.updateTime();
        setInterval(() => this.updateTime(), 1000);

        // 🔴 جلب الأسعار أولاً (الأهم!)
        await this.startTradingViewWatcher();

        // 🔴 مركز القرار الآن مدمج - لا يحتاج widget منفصل

        // Load initial data (للأخبار والتوصيات)
        await this.loadDashboard();

        // ✅ تحديث حالة الاتصال إلى "بث مباشر" بعد نجاح التحميل
        if (typeof SignalRService !== 'undefined') {
            SignalRService.updateStatus('live');
        }

        // Connect SignalR (اختياري - للتنبيهات فقط)
        SignalRService.connect().catch(e => {
            console.warn('SignalR (optional):', e.message);
            // نحافظ على حالة "بث مباشر" لأن الأسعار من TradingView
            SignalRService.updateStatus('live');
        });

        // Setup event listeners
        this.setupEventListeners();

        // Setup auto-refresh with countdown
        this.setupAutoRefreshWithCountdown();

        console.log('✅ Application initialized');
    },

    /**
     * 🔴 نظام الأسعار الموحد - لحظي وتفاعلي
     * ═══════════════════════════════════════════════════════════════════
     * يقرأ السعر من TradingView أو يستخدم محاكاة لحظية واقعية
     * ═══════════════════════════════════════════════════════════════════
     */
    async startTradingViewWatcher() {
        console.log('👁️ Starting Real-time Price System...');
        
        // 🔴 السعر الأولي - سيُحدث من TradingView فوراً
        this.basePrice = null;
        this.lastPrice = null;
        this.tradingViewReady = false;
        
        console.log('⏳ جاري انتظار سعر TradingView الحقيقي...');
        
        // محاولة قراءة السعر من TradingView
        this.setupTradingViewObserver();
        
        // 🔴 قراءة السعر من TradingView كل 14 ثانية (كما في دار السبائك)
        this.tryReadTradingView();
        setInterval(() => this.tryReadTradingView(), 14000);
    },
    
    /**
     * 🔴 لا نستخدم محاكاة - السعر الحقيقي فقط من TradingView
     */
    startRealtimePriceUpdates() {
        // تم إلغاء المحاكاة - نعتمد على TradingView فقط
        console.log('📊 الاعتماد على TradingView للأسعار الحقيقية');
    },
    
    /**
     * 🔴 إعداد MutationObserver لمراقبة تغييرات TradingView
     */
    setupTradingViewObserver() {
        const observer = new MutationObserver((mutations) => {
            this.tryReadTradingView();
        });
        
        // مراقبة الصفحة بأكملها
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });
    },
    
    /**
     * 🔴 قراءة السعر من TradingView Widget
     * يبحث عن السعر في جميع عناصر TradingView
     */
    tryReadTradingView() {
        try {
            // 🔴 البحث في كل الصفحة عن أرقام تشبه سعر الذهب
            const allText = document.body.innerText || '';
            
            // البحث عن أنماط سعر الذهب (X,XXX.XX أو XXXX.XX)
            const pricePatterns = [
                /(\d{1},\d{3}\.\d{2,3})/g,  // 5,075.46
                /(\d{4}\.\d{2,3})/g,         // 5075.46
            ];
            
            // 🔴 ترتيب الأسعار من الأعلى للأسفل لاختيار الأحدث
            let foundPrices = [];
            
            for (const pattern of pricePatterns) {
                const matches = allText.match(pattern);
                if (matches) {
                    for (const match of matches) {
                        const price = parseFloat(match.replace(/,/g, ''));
                        // سعر الذهب عادة بين 2000 و 10000 دولار (يناير 2026: ~$5000+)
                        if (price > 2000 && price < 10000) {
                            foundPrices.push(price);
                        }
                    }
                }
            }
            
            // 🔴 اختيار أعلى سعر (غالباً السعر الحالي يكون الأعلى في صفحة التداول)
            if (foundPrices.length > 0) {
                const price = Math.max(...foundPrices);
                
                // تجنب التحديث إذا السعر نفسه
                if (this.state.tradingViewPrice && 
                    Math.abs(this.state.tradingViewPrice - price) < 1) {
                    return true;
                }
                console.log('📊 TradingView Price Found:', price);
                this.setGlobalPrice(price);
                return true;
            }
            
            // 🔴 البحث في #tradingview-price-reader
            const priceReader = document.getElementById('tradingview-price-reader');
            if (priceReader) {
                const text = priceReader.innerText || '';
                const matches = text.match(/[\d,]+\.\d{2,3}/g);
                if (matches) {
                    for (const match of matches) {
                        const price = parseFloat(match.replace(/,/g, ''));
                        if (price > 2000 && price < 10000) {
                            console.log('📊 Price Reader Found:', price);
                            this.setGlobalPrice(price);
                            return true;
                        }
                    }
                }
            }
            
            // 🔴 البحث في iframes (قد لا يعمل بسبب CORS)
            const iframes = document.querySelectorAll('iframe');
            for (const iframe of iframes) {
                try {
                    const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
                    if (iframeDoc) {
                        const iframeText = iframeDoc.body?.innerText || '';
                        const matches = iframeText.match(/[\d,]+\.\d{2,3}/g);
                        if (matches) {
                            for (const match of matches) {
                                const price = parseFloat(match.replace(/,/g, ''));
                                if (price > 2000 && price < 10000) {
                                    console.log('📊 iframe Price Found:', price);
                                    this.setGlobalPrice(price);
                                    return true;
                                }
                            }
                        }
                    }
                } catch (e) {
                    // CORS error - ignore
                }
            }
            
            return false;
        } catch (e) {
            console.warn('⚠️ Error reading TradingView:', e);
            return false;
        }
    },
    
    /**
     * 🔴 استخراج السعر من نص
     */
    extractPrice(text) {
        const priceMatches = text.match(/[\d,]+\.\d{2,3}/g);
        if (priceMatches) {
            for (const match of priceMatches) {
                const price = parseFloat(match.replace(/,/g, ''));
                if (price > 2000 && price < 10000) {
                    this.setGlobalPrice(price);
                    return price;
                }
            }
        }
        return null;
    },
    
    /**
     * 🔴 تعيين السعر العالمي الموحد
     * هذه الدالة الوحيدة التي تُحدث السعر في كل المشروع
     */
    setGlobalPrice(price) {
        if (!price || price < 1000) return;
        
        // 🔴 تقريب السعر لتجنب التحديثات الطفيفة جداً
        const roundedPrice = Math.round(price * 100) / 100;
        
        // تجنب التحديث إذا الفرق أقل من 0.10$
        if (this.state.tradingViewPrice && 
            Math.abs(this.state.tradingViewPrice - roundedPrice) < 0.10) {
            return;
        }
        
        console.log(`📊 TradingView Price: $${roundedPrice}`);
        
        // 🔴 حفظ السعر السابق قبل التحديث
        if (this.state.tradingViewPrice) {
            this.state.previousGlobalPrice = {
                Close: this.state.tradingViewPrice
            };
        }
        
        // 🔴 تعيين السعر الموحد
        this.state.tradingViewPrice = roundedPrice;
        this.state.baseGoldPrice = roundedPrice;
        
        // 🔴 تحديث window.currentGoldPrice للصفحات الأخرى
        window.currentGoldPrice = roundedPrice;
        
        // 🔴 تحديث localStorage
        localStorage.setItem('current_gold_ounce_price', roundedPrice.toString());
        
        // 🔴 تحديث KuwaitGoldPricing إذا كان موجوداً
        if (typeof KuwaitGoldPricing !== 'undefined' && KuwaitGoldPricing.state) {
            KuwaitGoldPricing.state.previousOuncePrice = KuwaitGoldPricing.state.currentOuncePrice;
            KuwaitGoldPricing.state.currentOuncePrice = roundedPrice;
            KuwaitGoldPricing.state.lastUpdate = new Date();
            KuwaitGoldPricing.notifyListeners();
        }
        
        // ✅ تحديث حالة الاتصال إلى "بث مباشر"
        if (typeof SignalRService !== 'undefined') {
            SignalRService.updateStatus('live');
        }
        
        // 🔴 تحديث كل العرض
        this.updateFromTradingView(roundedPrice);
    },

    /**
     * 🔴 تحديث الأسعار من TradingView
     * هذه الدالة الرئيسية لتحديث كل شيء من السعر الموحد
     */
    updateFromTradingView(ouncePrice) {
        if (!ouncePrice || ouncePrice < 1000) return;
        
        // 🔴 السعر الموحد
        const currentPrice = this.state.tradingViewPrice || ouncePrice;
        const previousPrice = this.state.previousGlobalPrice?.Close || currentPrice;
        
        // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
        const kuwaitPrices = [];
        
        // 🔴 تحديث state.globalPrice (مستخدم في أماكن أخرى)
        const changePercent = previousPrice > 0 ? ((currentPrice - previousPrice) / previousPrice) * 100 : 0;
        this.state.globalPrice = {
            Close: currentPrice,
            Open: previousPrice,
            High: Math.max(currentPrice, previousPrice),
            Low: Math.min(currentPrice, previousPrice),
            PercentChange: changePercent
        };
        this.state.priceChangePercent = changePercent;
        
        // تحديث العرض
        this.updateFromTradingViewDisplay(currentPrice, kuwaitPrices, previousPrice);
    },

    /**
     * تحديث العرض من بيانات TradingView
     * [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
     */
    updateFromTradingViewDisplay(ouncePrice, kuwaitPrices, previousPrice) {
        console.log('🔄 Updating display with price:', ouncePrice);
        
        // تحديث سعر الأونصة فقط
        const ouncePriceEl = document.getElementById('global-ounce-price');
        if (ouncePriceEl) {
            ouncePriceEl.textContent = `$${ouncePrice.toLocaleString('en-US', {minimumFractionDigits: 2, maximumFractionDigits: 2})}`;
            console.log('✅ Updated global-ounce-price');
        }
        
        // تحديث وقت آخر تحديث
        const lastUpdateEl = document.getElementById('last-update-time');
        if (lastUpdateEl) {
            lastUpdateEl.textContent = new Date().toLocaleTimeString('ar-KW');
        }
        
        // تحديث وقت التحديث
        const updateTimeEl = document.getElementById('price-update-time');
        if (updateTimeEl) {
            updateTimeEl.textContent = new Date().toLocaleTimeString('ar-KW');
        }
        
        // تحديث مؤشر السوق
        this.updateMarketMovementFromTradingView(ouncePrice, previousPrice);
    },

    /**
     * تحديث مركز القرار الاستثماري من TradingView
     */
    updateMarketMovementFromTradingView(currentPrice, previousPrice) {
        // 🔴 تهيئة فورية إذا لم يتم التحليل بعد
        if (!this.state.priceHistory || this.state.priceHistory.length < 3) {
            this.initializeDecisionCenter(currentPrice, previousPrice);
        }
        
        // استدعاء مركز القرار الاستثماري
        this.updateMarketMovement({
            Close: currentPrice,
            Open: previousPrice || currentPrice,
            High: Math.max(currentPrice, previousPrice || currentPrice),
            Low: Math.min(currentPrice, previousPrice || currentPrice)
        });
    },
    
    /**
     * 🔴 تهيئة مركز القرار فوراً مع البيانات الأولية
     */
    initializeDecisionCenter(currentPrice, previousPrice) {
        const changePercent = previousPrice ? ((currentPrice - previousPrice) / previousPrice) * 100 : 0;
        
        // تحديد الاتجاه الأولي
        let trend = 'sideways';
        let status = 'wait';
        let explanation = 'السوق في حالة ترقب وانتظار';
        
        if (changePercent > 0.5) {
            trend = 'bullish';
            status = 'buy';
            explanation = 'الأسعار ترتفع - فرصة شراء محتملة';
        } else if (changePercent < -0.5) {
            trend = 'bearish';
            status = 'sell';
            explanation = 'الأسعار تتراجع - يُنصح بالحذر';
        }
        
        // تحديث الواجهة فوراً
        this.renderInitialDecision(currentPrice, changePercent, trend, status, explanation);
    },
    
    /**
     * 🔴 عرض القرار الأولي
     */
    renderInitialDecision(price, changePercent, trend, status, explanation) {
        // تحديث وقت التحديث
        const updateTimeEl = document.getElementById('decision-update-time');
        if (updateTimeEl) {
            updateTimeEl.textContent = new Date().toLocaleTimeString('ar-KW');
        }
        
        // تحديث اتجاه السعر
        const priceTrendIcon = document.getElementById('price-trend-icon');
        const priceTrendLabel = document.getElementById('price-trend-label');
        if (priceTrendIcon && priceTrendLabel) {
            if (trend === 'bullish') {
                priceTrendIcon.textContent = '📈';
                priceTrendLabel.textContent = 'صاعد';
                priceTrendLabel.className = 'text-sm font-bold text-green-400';
            } else if (trend === 'bearish') {
                priceTrendIcon.textContent = '📉';
                priceTrendLabel.textContent = 'هابط';
                priceTrendLabel.className = 'text-sm font-bold text-red-400';
            } else {
                priceTrendIcon.textContent = '➡️';
                priceTrendLabel.textContent = 'مستقر';
                priceTrendLabel.className = 'text-sm font-bold text-yellow-400';
            }
        }
        
        // القرار والألوان
        const decisions = {
            'strong_buy': { icon: '🚀', label: 'شراء قوي', sublabel: 'فرصة ممتازة', color: '#22c55e', position: '90%' },
            'buy': { icon: '📈', label: 'شراء', sublabel: 'فرصة جيدة', color: '#4ade80', position: '75%' },
            'wait': { icon: '⏳', label: 'انتظار', sublabel: 'السوق متذبذب', color: '#fbbf24', position: '50%' },
            'sell': { icon: '📉', label: 'بيع', sublabel: 'تراجع متوقع', color: '#f87171', position: '25%' },
            'strong_sell': { icon: '⚠️', label: 'بيع قوي', sublabel: 'خطر عالي', color: '#ef4444', position: '10%' }
        };
        
        const decision = decisions[status] || decisions['wait'];
        const confidence = Math.min(75, 45 + Math.abs(changePercent) * 10);
        
        // تحديث بطاقة القرار
        const decisionBox = document.getElementById('decision-box');
        if (decisionBox) {
            decisionBox.style.background = `rgba(${this.hexToRgb(decision.color)}, 0.15)`;
            decisionBox.style.borderColor = `${decision.color}66`;
        }
        
        // تحديث الأيقونة والنص
        const decisionIcon = document.getElementById('decision-icon');
        const decisionLabel = document.getElementById('decision-label');
        const decisionSublabel = document.getElementById('decision-sublabel');
        
        if (decisionIcon) decisionIcon.textContent = decision.icon;
        if (decisionLabel) {
            decisionLabel.textContent = decision.label;
            decisionLabel.style.color = decision.color;
        }
        if (decisionSublabel) {
            decisionSublabel.textContent = decision.sublabel;
            decisionSublabel.style.color = decision.color + '99';
        }
        
        // تحديث نسبة الثقة
        const confidenceCircle = document.getElementById('confidence-circle');
        const confidenceValue = document.getElementById('confidence-value');
        if (confidenceCircle) {
            confidenceCircle.style.borderColor = decision.color;
        }
        if (confidenceValue) {
            confidenceValue.textContent = `${confidence.toFixed(0)}%`;
            confidenceValue.style.color = decision.color;
        }
        
        // تحديث مؤشر القرار
        const decisionIndicator = document.getElementById('decision-indicator');
        if (decisionIndicator) {
            decisionIndicator.style.left = decision.position;
        }
    },
    
    /**
     * تحويل Hex إلى RGB
     */
    hexToRgb(hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? 
            `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : 
            '113, 113, 122';
    },

    /**
     * Load dashboard data
     * 🔴 الأسعار تأتي من TradingView فقط - هنا نحمل الأخبار والتوصيات
     */
    async loadDashboard() {
        try {
            const data = await API.getDashboard();

            if (data) {
                // 🔴 لا نستخدم GlobalPrice من Backend - TradingView فقط
                // if (data.GlobalPrice?.Close) {
                //     this.updateFromTradingView(data.GlobalPrice.Close);
                // }

                // Update news impact first (التوصية تعتمد عليها)
                if (data.NewsImpact) {
                    this.updateNewsImpact(data.NewsImpact);
                }

                // Update recommendation (بعد الأخبار لأنها تعتمد عليها)
                this.updateRecommendation(data.Recommendation || {});

                // Update unread alerts
                this.state.unreadAlerts = data.UnreadAlerts || 0;
                this.updateAlertsBadge();
            }
            // If data is null, backend is not available - use local mode silently

            // Load recent alerts (also handles null gracefully)
            await this.loadRecentAlerts();

        } catch (error) {
            // Silent fail - backend not available, use local mode
            // console.error('Failed to load dashboard:', error);
        }
    },

    /**
     * Update global price display
     * يعرض السعر العالمي بالدولار للأونصة مع التغير
     */
    updateGlobalPrice(price) {
        // حفظ السعر السابق
        if (this.state.globalPrice) {
            this.state.previousGlobalPrice = this.state.globalPrice;
        }
        this.state.globalPrice = price;

        const priceEl = document.getElementById('global-price');
        const changeEl = document.getElementById('global-change');
        const trendEl = document.getElementById('global-trend');
        const ouncePriceEl = document.getElementById('global-ounce-price');

        if (priceEl) {
            const currentPrice = (price.Close || 0).toFixed(2);
            priceEl.textContent = `$${currentPrice}`;

            // تأثير وميض عند تغير السعر
            if (this.state.previousGlobalPrice &&
                this.state.previousGlobalPrice.Close !== price.Close) {
                priceEl.classList.add('price-flash');
                setTimeout(() => priceEl.classList.remove('price-flash'), 500);
            }
        }

        // تحديث سعر الأونصة في بطاقة عيار 24
        if (ouncePriceEl) {
            ouncePriceEl.textContent = `$${(price.Close || 0).toFixed(2)}`;
        }

        if (changeEl) {
            // حساب التغير مقارنة بالسعر السابق
            let change, changePercent;

            if (this.state.previousGlobalPrice) {
                change = price.Close - this.state.previousGlobalPrice.Close;
                changePercent = (change / this.state.previousGlobalPrice.Close) * 100;
            } else {
                change = price.Close - price.Open;
                changePercent = (change / price.Open) * 100;
            }

            const isUp = change >= 0;
            const arrow = isUp ? '▲' : '▼';
            const colorClass = isUp ? 'text-green-400' : 'text-red-400';

            changeEl.innerHTML = `
                <span class="${colorClass} flex items-center gap-1">
                    <span class="text-lg">${arrow}</span>
                    <span>$${Math.abs(change).toFixed(2)}</span>
                    <span class="text-xs">(${isUp ? '+' : ''}${changePercent.toFixed(2)}%)</span>
                </span>
            `;
        }

        if (trendEl) {
            const change = price.Close - price.Open;
            const isUp = change >= 0;
            trendEl.className = `text-sm px-3 py-1 rounded-full font-bold ${isUp ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-400'}`;
            trendEl.textContent = isUp ? '📈 صاعد' : '📉 هابط';
        }

        // تحديث مؤشر حركة الذهب
        this.updateMarketMovement(price);
    },

    /**
     * 🎯 مركز القرار الاستثماري - لحظي وتفاعلي
     * ═══════════════════════════════════════════════════════════════════
     * يجمع بين تحليل السعر والأخبار لإصدار قرار استثماري موثوق
     * يعمل فوراً من أول قراءة!
     * ═══════════════════════════════════════════════════════════════════
     */
    updateMarketMovement(price) {
        // حفظ تاريخ الأسعار للتحليل
        if (!this.state.priceHistory) {
            this.state.priceHistory = [];
        }

        // إضافة السعر الجديد
        this.state.priceHistory.push({
            price: price.Close,
            time: Date.now()
        });

        // الاحتفاظ بآخر 30 قراءة
        if (this.state.priceHistory.length > 30) {
            this.state.priceHistory.shift();
        }

        // 🔴 تحليل فوري (حتى مع قراءة واحدة!)
        const priceAnalysis = this.analyzePriceMovementRealtime();
        
        // 📰 تحليل الأخبار
        const newsAnalysis = this.analyzeNewsImpactRealtime();
        
        // 🎯 إصدار القرار الاستثماري
        const decision = this.generateInvestmentDecision(priceAnalysis, newsAnalysis);
        
        // حفظ الحالة
        this.state.currentMarketState = decision;
        this.state.priceChangePercent = priceAnalysis.changePercent;
        
        // تحديث الواجهة
        this.renderInvestmentDecision(decision, priceAnalysis, newsAnalysis);
        
        // تحديث وقت التحديث
        const updateTimeEl = document.getElementById('decision-update-time');
        if (updateTimeEl) {
            updateTimeEl.textContent = new Date().toLocaleTimeString('ar-KW');
        }
    },
    
    /**
     * 📊 تحليل حركة السعر - لحظي
     */
    analyzePriceMovementRealtime() {
        const history = this.state.priceHistory;
        const currentPrice = history[history.length - 1]?.price || this.lastPrice || 2745;
        
        // حساب التغير من أقدم سعر متاح
        let prevPrice = currentPrice;
        let olderPrice = currentPrice;
        
        if (history.length >= 2) {
            prevPrice = history[history.length - 2].price;
        }
        if (history.length >= 5) {
            olderPrice = history[history.length - 5].price;
        } else if (history.length >= 3) {
            olderPrice = history[0].price;
        }
        
        // حساب التغيرات
        const shortTermChange = currentPrice - prevPrice;
        const mediumTermChange = currentPrice - olderPrice;
        const changePercent = olderPrice > 0 ? ((mediumTermChange / olderPrice) * 100) : 0;
        
        // متوسط آخر القراءات المتاحة
        const recentPrices = history.slice(-Math.min(5, history.length));
        const avgPrice = recentPrices.length > 0 
            ? recentPrices.reduce((sum, p) => sum + p.price, 0) / recentPrices.length 
            : currentPrice;
        
        // تحديد الاتجاه
        let trend = 'sideways';
        let trendStrength = 50;
        
        if (changePercent > 0.05) {
            trend = 'bullish';
            trendStrength = Math.min(100, 50 + changePercent * 100);
        } else if (changePercent < -0.05) {
            trend = 'bearish';
            trendStrength = Math.min(100, 50 + Math.abs(changePercent) * 100);
        }
        
        // إشارات السعر
        const signals = [];
        
        if (currentPrice > avgPrice * 1.001) {
            signals.push({ type: 'bullish', text: 'السعر أعلى من المتوسط', weight: 20 });
        } else if (currentPrice < avgPrice * 0.999) {
            signals.push({ type: 'bearish', text: 'السعر أقل من المتوسط', weight: 20 });
        }
        
        if (shortTermChange > 0 && mediumTermChange > 0) {
            signals.push({ type: 'bullish', text: 'اتجاه صاعد مستمر', weight: 30 });
        } else if (shortTermChange < 0 && mediumTermChange < 0) {
            signals.push({ type: 'bearish', text: 'اتجاه هابط مستمر', weight: 30 });
        } else if (shortTermChange > 0 && mediumTermChange < 0) {
            signals.push({ type: 'bullish', text: 'ارتداد صعودي', weight: 25 });
        } else if (shortTermChange < 0 && mediumTermChange > 0) {
            signals.push({ type: 'bearish', text: 'تصحيح هبوطي', weight: 25 });
        }
        
        return {
            currentPrice,
            prevPrice,
            changePercent,
            trend,
            trendStrength,
            signals,
            avgPrice
        };
    },
    
    /**
     * 📰 تحليل تأثير الأخبار - لحظي
     */
    analyzeNewsImpactRealtime() {
        const newsImpact = this.state.newsImpact;
        const signals = [];
        let overallSentiment = 'neutral';
        let sentimentScore = 50;
        
        // حتى بدون أخبار، نعرض حالة محايدة وليس "--"
        if (!newsImpact || !newsImpact.HighImpactNews || newsImpact.HighImpactNews.length === 0) {
            // استخدام تحليل افتراضي بناءً على حركة السعر
            const priceChange = this.state.priceChangePercent || 0;
            
            if (priceChange > 0.1) {
                overallSentiment = 'bullish';
                sentimentScore = 60;
                signals.push({ type: 'bullish', text: 'حركة إيجابية في السوق', weight: 15 });
            } else if (priceChange < -0.1) {
                overallSentiment = 'bearish';
                sentimentScore = 40;
                signals.push({ type: 'bearish', text: 'ضغط بيعي ملحوظ', weight: 15 });
            } else {
                signals.push({ type: 'neutral', text: 'السوق في حالة استقرار', weight: 10 });
            }
            
            return { signals, overallSentiment, sentimentScore, hasNews: false };
        }
        
        // تحليل الأخبار الموجودة
        let bullishCount = 0;
        let bearishCount = 0;
        
        newsImpact.HighImpactNews.forEach(news => {
            const impact = news.ImpactScore || 50;
            
            if (news.GoldImpactDirection === 'bullish' || news.GoldImpactDirection === 'Bullish') {
                bullishCount++;
                sentimentScore += impact * 0.3;
            } else if (news.GoldImpactDirection === 'bearish' || news.GoldImpactDirection === 'Bearish') {
                bearishCount++;
                sentimentScore -= impact * 0.3;
            }
        });
        
        sentimentScore = Math.max(0, Math.min(100, sentimentScore));
        
        if (sentimentScore > 60) {
            overallSentiment = 'bullish';
            signals.push({ type: 'bullish', text: `${bullishCount} أخبار إيجابية`, weight: 20 });
        } else if (sentimentScore < 40) {
            overallSentiment = 'bearish';
            signals.push({ type: 'bearish', text: `${bearishCount} أخبار سلبية`, weight: 20 });
        } else {
            signals.push({ type: 'neutral', text: 'أخبار متوازنة', weight: 10 });
        }
        
        return { signals, overallSentiment, sentimentScore, hasNews: true };
    },

    /**
     * 📊 تحليل حركة السعر
     */
    analyzePriceMovement() {
        const history = this.state.priceHistory;
        const currentPrice = history[history.length - 1].price;
        const prevPrice = history[history.length - 2].price;
        const olderPrice = history[history.length - 3].price;

        // حساب التغيرات
        const shortTermChange = currentPrice - prevPrice;
        const mediumTermChange = currentPrice - olderPrice;
        const changePercent = ((mediumTermChange / olderPrice) * 100);
        
        // حساب متوسط آخر 5 قراءات
        const recentPrices = history.slice(-5);
        const avgPrice = recentPrices.reduce((sum, p) => sum + p.price, 0) / recentPrices.length;
        
        // تحديد الاتجاه
        let trend = 'sideways';
        let trendStrength = 0;
        
        if (changePercent > 0.1) {
            trend = 'bullish';
            trendStrength = Math.min(100, changePercent * 20);
        } else if (changePercent < -0.1) {
            trend = 'bearish';
            trendStrength = Math.min(100, Math.abs(changePercent) * 20);
        } else {
            trend = 'sideways';
            trendStrength = 50;
        }
        
        // تحديد إشارات السعر
        const signals = [];
        
        if (currentPrice > avgPrice) {
            signals.push({ type: 'bullish', text: 'السعر أعلى من المتوسط القريب', weight: 20 });
        } else if (currentPrice < avgPrice) {
            signals.push({ type: 'bearish', text: 'السعر أقل من المتوسط القريب', weight: 20 });
        }
        
        if (shortTermChange > 0 && mediumTermChange > 0) {
            signals.push({ type: 'bullish', text: 'اتجاه صاعد مستمر', weight: 30 });
        } else if (shortTermChange < 0 && mediumTermChange < 0) {
            signals.push({ type: 'bearish', text: 'اتجاه هابط مستمر', weight: 30 });
        } else if (shortTermChange > 0 && mediumTermChange < 0) {
            signals.push({ type: 'bullish', text: 'ارتداد صعودي محتمل', weight: 25 });
        } else if (shortTermChange < 0 && mediumTermChange > 0) {
            signals.push({ type: 'bearish', text: 'تصحيح هبوطي محتمل', weight: 25 });
        }
        
        // قوة الحركة
        const volatility = Math.abs(changePercent);
        if (volatility > 0.5) {
            signals.push({ type: trend === 'bullish' ? 'bullish' : 'bearish', text: 'حركة قوية في السوق', weight: 15 });
        } else if (volatility < 0.1) {
            signals.push({ type: 'neutral', text: 'السوق هادئ ومستقر', weight: 10 });
        }
        
        return {
            currentPrice,
            prevPrice,
            changePercent,
            trend,
            trendStrength,
            signals,
            avgPrice
        };
    },

    /**
     * 📰 تحليل تأثير الأخبار
     */
    analyzeNewsImpact() {
        const newsImpact = this.state.newsImpact;
        const signals = [];
        let overallSentiment = 'neutral';
        let sentimentScore = 50; // 0=سلبي جداً، 50=محايد، 100=إيجابي جداً
        
        if (!newsImpact || !newsImpact.HighImpactNews || newsImpact.HighImpactNews.length === 0) {
            signals.push({ type: 'neutral', text: 'لا توجد أخبار مؤثرة حالياً', weight: 10 });
            return { signals, overallSentiment, sentimentScore, hasNews: false };
        }
        
        // تحليل الأخبار
        let bullishCount = 0;
        let bearishCount = 0;
        let totalImpact = 0;
        
        newsImpact.HighImpactNews.forEach(news => {
            const impact = news.ImpactScore || 50;
            totalImpact += impact;
            
            if (news.GoldImpactDirection === 'bullish' || news.GoldImpactDirection === 'Bullish') {
                bullishCount++;
                sentimentScore += impact * 0.3;
            } else if (news.GoldImpactDirection === 'bearish' || news.GoldImpactDirection === 'Bearish') {
                bearishCount++;
                sentimentScore -= impact * 0.3;
            }
        });
        
        // تحديد الاتجاه العام
        if (bullishCount > bearishCount) {
            overallSentiment = 'bullish';
            signals.push({ type: 'bullish', text: `${bullishCount} أخبار إيجابية للذهب`, weight: 25 });
        } else if (bearishCount > bullishCount) {
            overallSentiment = 'bearish';
            signals.push({ type: 'bearish', text: `${bearishCount} أخبار سلبية للذهب`, weight: 25 });
        } else {
            signals.push({ type: 'neutral', text: 'الأخبار متوازنة', weight: 15 });
        }
        
        // إضافة أهم خبر
        if (newsImpact.HighImpactNews.length > 0) {
            const topNews = newsImpact.HighImpactNews[0];
            const newsType = topNews.GoldImpactDirection === 'bullish' ? 'bullish' : 
                            topNews.GoldImpactDirection === 'bearish' ? 'bearish' : 'neutral';
            signals.push({ 
                type: newsType, 
                text: `خبر مؤثر: ${topNews.Title?.substring(0, 50)}...`, 
                weight: 20 
            });
        }
        
        sentimentScore = Math.max(0, Math.min(100, sentimentScore));
        
        return { 
            signals, 
            overallSentiment, 
            sentimentScore, 
            hasNews: true,
            newsCount: newsImpact.HighImpactNews.length 
        };
    },

    /**
     * 🎯 إصدار القرار الاستثماري
     */
    generateInvestmentDecision(priceAnalysis, newsAnalysis) {
        // حساب النقاط الإجمالية
        let bullishPoints = 0;
        let bearishPoints = 0;
        let neutralPoints = 0;
        
        // نقاط من تحليل السعر (وزن 60%)
        priceAnalysis.signals.forEach(signal => {
            if (signal.type === 'bullish') bullishPoints += signal.weight * 0.6;
            else if (signal.type === 'bearish') bearishPoints += signal.weight * 0.6;
            else neutralPoints += signal.weight * 0.6;
        });
        
        // نقاط من تحليل الأخبار (وزن 40%)
        newsAnalysis.signals.forEach(signal => {
            if (signal.type === 'bullish') bullishPoints += signal.weight * 0.4;
            else if (signal.type === 'bearish') bearishPoints += signal.weight * 0.4;
            else neutralPoints += signal.weight * 0.4;
        });
        
        // تحديد القرار
        const totalPoints = bullishPoints + bearishPoints + neutralPoints;
        const bullishPercent = (bullishPoints / totalPoints) * 100;
        const bearishPercent = (bearishPoints / totalPoints) * 100;
        
        let decision, confidence, icon, color, explanation, indicatorPosition;
        
        if (bullishPoints > bearishPoints + 15) {
            // شراء
            decision = bullishPoints > bearishPoints + 30 ? 'شراء قوي' : 'شراء';
            icon = bullishPoints > bearishPoints + 30 ? '🚀' : '📈';
            color = '#4ade80';
            confidence = Math.min(95, 50 + bullishPercent * 0.5);
            indicatorPosition = bullishPoints > bearishPoints + 30 ? 90 : 75;
            explanation = this.generateBuyExplanation(priceAnalysis, newsAnalysis);
        } else if (bearishPoints > bullishPoints + 15) {
            // بيع
            decision = bearishPoints > bullishPoints + 30 ? 'بيع قوي' : 'بيع';
            icon = bearishPoints > bullishPoints + 30 ? '🔻' : '📉';
            color = '#f87171';
            confidence = Math.min(95, 50 + bearishPercent * 0.5);
            indicatorPosition = bearishPoints > bullishPoints + 30 ? 10 : 25;
            explanation = this.generateSellExplanation(priceAnalysis, newsAnalysis);
        } else {
            // انتظار
            decision = 'انتظار';
            icon = '⏳';
            color = '#fbbf24';
            confidence = Math.min(85, 40 + neutralPoints * 0.5);
            indicatorPosition = 50;
            explanation = this.generateHoldExplanation(priceAnalysis, newsAnalysis);
        }
        
        return {
            decision,
            icon,
            color,
            confidence: Math.round(confidence),
            indicatorPosition,
            explanation,
            status: decision.includes('شراء') ? 'bullish' : decision.includes('بيع') ? 'bearish' : 'neutral',
            bullishPoints,
            bearishPoints,
            neutralPoints
        };
    },

    /**
     * توليد شرح قرار الشراء
     */
    generateBuyExplanation(priceAnalysis, newsAnalysis) {
        const reasons = [];
        
        if (priceAnalysis.trend === 'bullish') {
            reasons.push('السعر في اتجاه صاعد');
        }
        if (priceAnalysis.currentPrice > priceAnalysis.avgPrice) {
            reasons.push('الزخم الإيجابي مستمر');
        }
        if (newsAnalysis.overallSentiment === 'bullish') {
            reasons.push('الأخبار تدعم الصعود');
        }
        
        const mainReason = reasons.length > 0 ? reasons.join(' و') : 'المؤشرات إيجابية';
        
        return `🟢 فرصة شراء! ${mainReason}. الذهب يظهر قوة والتوقعات إيجابية. يُنصح بالشراء مع وضع حد للخسارة.`;
    },

    /**
     * توليد شرح قرار البيع
     */
    generateSellExplanation(priceAnalysis, newsAnalysis) {
        const reasons = [];
        
        if (priceAnalysis.trend === 'bearish') {
            reasons.push('السعر في اتجاه هابط');
        }
        if (priceAnalysis.currentPrice < priceAnalysis.avgPrice) {
            reasons.push('الضغط البيعي مستمر');
        }
        if (newsAnalysis.overallSentiment === 'bearish') {
            reasons.push('الأخبار سلبية');
        }
        
        const mainReason = reasons.length > 0 ? reasons.join(' و') : 'المؤشرات سلبية';
        
        return `🔴 وقت البيع! ${mainReason}. الذهب يظهر ضعف والتوقعات سلبية. يُنصح بالبيع أو الانتظار لسعر أفضل للشراء.`;
    },

    /**
     * توليد شرح قرار الانتظار
     */
    generateHoldExplanation(priceAnalysis, newsAnalysis) {
        return `⏳ انتظر! السوق غير واضح الاتجاه. المؤشرات متضاربة والأفضل الانتظار حتى تتضح الصورة. لا تتخذ قرار متسرع.`;
    },

    /**
     * 🎨 عرض القرار الاستثماري في الواجهة
     */
    renderInvestmentDecision(decision, priceAnalysis, newsAnalysis) {
        // تحديث بطاقة القرار - نسخة مصغرة
        const box = document.getElementById('decision-box');
        if (box) {
            box.style.background = `${decision.color}22`;
            box.style.borderColor = `${decision.color}66`;
        }

        // تحديث الأيقونة
        const iconEl = document.getElementById('decision-icon');
        if (iconEl) iconEl.textContent = decision.icon;

        // تحديث القرار
        const labelEl = document.getElementById('decision-label');
        if (labelEl) {
            labelEl.textContent = decision.decision;
            labelEl.style.color = decision.color;
        }

        // تحديث الوصف الفرعي
        const sublabelEl = document.getElementById('decision-sublabel');
        if (sublabelEl) {
            const sublabels = {
                'شراء قوي': 'فرصة ممتازة',
                'شراء': 'فرصة جيدة',
                'بيع قوي': 'بيع فوري',
                'بيع': 'فرصة بيع',
                'انتظار': 'متذبذب'
            };
            sublabelEl.textContent = sublabels[decision.decision] || 'جاري التحليل';
            sublabelEl.style.color = decision.color + '99';
        }

        // تحديث نسبة الثقة
        const confidenceCircle = document.getElementById('confidence-circle');
        const confidenceValue = document.getElementById('confidence-value');
        if (confidenceCircle) {
            confidenceCircle.style.borderColor = decision.color;
        }
        if (confidenceValue) {
            confidenceValue.textContent = `${decision.confidence}%`;
            confidenceValue.style.color = decision.color;
        }

        // تحديث مؤشر التوصية
        const indicator = document.getElementById('decision-indicator');
        if (indicator) {
            indicator.style.left = `${decision.indicatorPosition}%`;
        }

        // تحديث اتجاه السعر
        const trendIcon = document.getElementById('price-trend-icon');
        const trendLabel = document.getElementById('price-trend-label');
        if (trendIcon && trendLabel) {
            const trendIcons = { bullish: '📈', bearish: '📉', sideways: '➡️' };
            const trendLabels = { bullish: 'صاعد', bearish: 'هابط', sideways: 'مستقر' };
            const trendColors = { bullish: '#4ade80', bearish: '#f87171', sideways: '#fbbf24' };
            
            trendIcon.textContent = trendIcons[priceAnalysis.trend];
            trendLabel.textContent = trendLabels[priceAnalysis.trend];
            trendLabel.style.color = trendColors[priceAnalysis.trend];
        }

        // تحديث تأثير الأخبار
        const newsIcon = document.getElementById('news-impact-icon');
        const newsLabel = document.getElementById('news-impact-label');
        if (newsIcon && newsLabel) {
            const newsIcons = { bullish: '✅', bearish: '❌', neutral: '➖' };
            const newsLabels = { bullish: 'إيجابي', bearish: 'سلبي', neutral: 'محايد' };
            const newsColors = { bullish: '#4ade80', bearish: '#f87171', neutral: '#fbbf24' };
            
            newsIcon.textContent = newsIcons[newsAnalysis.overallSentiment];
            newsLabel.textContent = newsLabels[newsAnalysis.overallSentiment];
            newsLabel.style.color = newsColors[newsAnalysis.overallSentiment];
        }

        // حفظ الحالة للتوصية
        this.state.currentMarketState = decision;
    },



    /**
     * [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
     */
    updateKuwaitPrices(prices) {
        // تم تعطيل هذه الوظيفة مؤقتاً
    },

    /**
     * [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
     */
    updateKuwaitTable(prices) {
        // تم تعطيل هذه الوظيفة مؤقتاً
    },

    /**
     * Update recommendation display
     * مرتبط بتحليل حركة السعر من updateMarketMovement
     */
    updateRecommendation(rec) {
        this.state.recommendation = rec;

        const container = document.getElementById('recommendation-content');
        if (!container) return;

        // استخدام حالة السوق من تحليل حركة السعر (الأولوية)
        const marketState = this.state.currentMarketState;

        // تحديث وقت التوصية
        const recTimeEl = document.getElementById('rec-time');
        if (recTimeEl) recTimeEl.textContent = `تحديث: ${new Date().toLocaleTimeString('ar-KW')}`;

        if (marketState) {
            // التوصية بناءً على حالة السوق (تحليل حركة السعر)
            const isUp = marketState.status === 'bullish';
            const isDown = marketState.status === 'bearish';

            const recType = isUp ? 'شراء' : isDown ? 'بيع' : 'انتظار';
            const recIcon = isUp ? '📈' : isDown ? '📉' : '⏳';
            const recColor = marketState.color;

            // حساب الثقة بناءً على قوة الحركة
            const priceHistory = this.state.priceHistory || [];
            let confidence = 60;
            if (priceHistory.length >= 3) {
                const totalChange = Math.abs(priceHistory[priceHistory.length - 1].price - priceHistory[priceHistory.length - 3].price);
                confidence = Math.min(95, 60 + (totalChange * 2));
            }

            // شرح التوصية بالعامية الكويتية
            const getRecommendationText = () => {
                if (isUp) {
                    return 'السعر طالع! الوقت حلو للشراء لو تبي تستثمر. الذهب يصعد وشكله بيكمل';
                } else if (isDown) {
                    return 'السعر نازل! لو عندك ذهب انتبه. يمكن تبيع وتشتري بسعر أرخص بعدين';
                } else {
                    return 'السوق هادي وما فيه حركة واضحة. الأحسن تنتظر لين يصير فيه اتجاه واضح';
                }
            };

            container.innerHTML = `
                <!-- التوصية الرئيسية -->
                <div class="flex items-center gap-4 mb-4">
                    <div class="w-16 h-16 rounded-2xl flex items-center justify-center text-3xl" style="background: ${recColor}22;">
                        ${recIcon}
                    </div>
                    <div>
                        <h4 class="text-2xl font-bold" style="color: ${recColor};">${recType}</h4>
                        <p class="text-zinc-400 text-sm">ثقة: ${confidence.toFixed(0)}%</p>
                    </div>
                </div>
                
                <!-- حالة السوق -->
                <div class="flex items-center gap-2 mb-3">
                    <span class="text-xl">${marketState.icon}</span>
                    <span class="px-2 py-1 rounded-full text-xs font-bold" style="background: ${recColor}22; color: ${recColor};">${marketState.label}</span>
                </div>
                
                <!-- الشرح بالعامية -->
                <div class="p-3 rounded-xl bg-zinc-800/50 border-r-4 mb-4" style="border-color: ${recColor};">
                    <p class="text-sm text-white">${getRecommendationText()}</p>
                </div>
                
                <!-- مؤشرات -->
                <div class="grid grid-cols-2 gap-3">
                    <div class="bg-zinc-800/30 rounded-lg p-3 text-center">
                        <p class="text-xs text-zinc-500 mb-1">السعر الحالي</p>
                        <p class="text-lg font-bold text-gold-400">$${(this.state.globalPrice?.Close || 0).toFixed(2)}</p>
                    </div>
                    <div class="bg-zinc-800/30 rounded-lg p-3 text-center">
                        <p class="text-xs text-zinc-500 mb-1">الاتجاه</p>
                        <p class="text-lg font-bold" style="color: ${recColor};">${isUp ? 'صعودي' : isDown ? 'هبوطي' : 'محايد'}</p>
                    </div>
                </div>
            `;
        } else {
            // توصية افتراضية (عند عدم وجود بيانات كافية)
            const typeConfig = CONFIG.RECOMMENDATION_TYPES.Hold;

            container.innerHTML = `
                <div class="flex items-center gap-4 mb-4">
                    <div class="w-16 h-16 rounded-2xl ${typeConfig.bgColor} flex items-center justify-center text-3xl">
                        ${typeConfig.icon}
                    </div>
                    <div>
                        <h4 class="text-2xl font-bold ${typeConfig.color}">انتظار</h4>
                        <p class="text-zinc-400 text-sm">جاري التحليل...</p>
                    </div>
                </div>
                <div class="p-3 rounded-xl bg-zinc-800/50">
                    <p class="text-sm text-zinc-300">جاري تحليل حركة السوق... انتظر لحظات للحصول على توصية دقيقة</p>
                </div>
            `;
        }
    },

    /**
     * Update news impact display
     */
    updateNewsImpact(impact) {
        this.state.newsImpact = impact;

        const container = document.getElementById('news-impact-content');
        if (!container) return;

        // السعر العالمي الحالي
        const currentPrice = this.state.globalPrice?.Close || 0;
        const priceChange = this.state.globalPrice?.PercentChange || 0;

        // جلب أي خبر متاح (الأولوية للأخبار عالية التأثير)
        const allNews = impact.HighImpactNews || [];
        const hasNews = allNews.length > 0;
        
        // استخدام حالة السوق الفعلية (من تحليل حركة السعر) كمرجع أساسي
        const marketState = this.state.currentMarketState;

        if (hasNews) {
            // الخبر الأول (سواء مؤثر أو محايد)
            const topNews = allNews[0];
            
            // ═══════════════════════════════════════════════════════════════════
            // المنطق الموحد: استخدام حالة السوق الفعلية بدلاً من تصنيف الخبر
            // ═══════════════════════════════════════════════════════════════════
            let topImpact, topScore, isUp, isDown, isNeutral;
            
            if (marketState) {
                // الأولوية لحالة السوق الفعلية
                topImpact = marketState.status;
                isUp = topImpact === 'bullish';
                isDown = topImpact === 'bearish';
                isNeutral = !isUp && !isDown;
                // درجة التأثير بناءً على قوة حركة السعر
                topScore = Math.min(95, 50 + Math.abs(this.state.priceChangePercent || 0) * 20);
            } else {
                // fallback لتصنيف الخبر
                topImpact = topNews.GoldImpactDirection || topNews.ImpactType || 'neutral';
                topScore = topNews.ImpactScore || 30;
                isUp = topImpact === 'bullish';
                isDown = topImpact === 'bearish';
                isNeutral = !isUp && !isDown;
            }

            const topIcon = isUp ? '📈' : isDown ? '📉' : '📰';
            const topText = isUp ? 'صعودي' : isDown ? 'هبوطي' : 'محايد';
            const mainColor = isUp ? '#4ade80' : isDown ? '#f87171' : '#fbbf24';
            const bgColor = isUp ? 'rgba(34,197,94,0.1)' : isDown ? 'rgba(239,68,68,0.1)' : 'rgba(251,191,36,0.1)';

            container.innerHTML = `
                <!-- الخبر ${isNeutral ? '' : 'الأكثر تأثيراً'} -->
                <div class="rounded-xl p-4" style="background: linear-gradient(135deg, ${bgColor}, transparent); border: 1px solid ${mainColor}33;">
                    
                    <!-- العنوان -->
                    <h4 class="font-bold text-white mb-3 line-clamp-2">${topNews.Title}</h4>
                    
                    <!-- السعر العالمي الحالي - TradingView Widget -->
                    <div id="news-tradingview-price" class="rounded-lg mb-3 overflow-hidden border border-gold-500/30" style="height: 120px;"></div>
                    
                    <!-- مؤشرات التحليل -->
                    <div class="grid grid-cols-2 gap-2 text-center">
                        <div class="bg-zinc-800/50 rounded-lg p-2">
                            <p class="text-xs text-zinc-500">حالة السوق</p>
                            <p class="text-sm font-bold" style="color: ${mainColor};">${topIcon} ${topText}</p>
                        </div>
                        <div class="bg-zinc-800/50 rounded-lg p-2">
                            <p class="text-xs text-zinc-500">المصدر</p>
                            <p class="text-xs font-bold text-white truncate">${topNews.Source || 'تحليل لحظي'}</p>
                        </div>
                    </div>
                    
                    <!-- شرح التأثير - بناءً على حركة السوق الفعلية -->
                    <div class="mt-3 p-2 rounded-lg bg-zinc-900/50 border-r-2" style="border-color: ${mainColor};">
                        <p class="text-xs text-zinc-400">${
                            marketState ? marketState.explanation : 
                            (topNews.ImpactExplanation || (isNeutral ? 'تأثير محدود على أسعار الذهب' : 
                            (isUp ? 'هذا الخبر يدعم ارتفاع أسعار الذهب' : 'هذا الخبر قد يضغط على أسعار الذهب')))
                        }</p>
                    </div>
                </div>
            `;
            
            // 🔴 تحميل TradingView Widget للسعر
            this.loadTradingViewWidget('news-tradingview-price');
        } else {
            // عرض خبر يتناسب مع حالة السوق الحالية
            const marketState = this.state.currentMarketState;

            // توليد خبر بناءً على حالة السوق
            const getMarketNews = () => {
                if (!marketState) {
                    return {
                        title: 'أسواق الذهب العالمية مستقرة',
                        explanation: 'السوق في حالة ترقب وانتظار لأي تطورات جديدة',
                        impact: 'neutral',
                        icon: '📊',
                        color: '#fbbf24',
                        bgColor: 'rgba(251,191,36,0.1)'
                    };
                }

                const isBullish = marketState.status === 'bullish';
                const isBearish = marketState.status === 'bearish';

                if (isBullish) {
                    return {
                        title: 'أسعار الذهب ترتفع في الأسواق العالمية',
                        explanation: marketState.explanation || 'الذهب يسجل ارتفاعاً مع زيادة الطلب من المستثمرين',
                        impact: 'bullish',
                        icon: '📈',
                        color: '#4ade80',
                        bgColor: 'rgba(34,197,94,0.1)'
                    };
                } else if (isBearish) {
                    return {
                        title: 'أسعار الذهب تتراجع في التداولات العالمية',
                        explanation: marketState.explanation || 'الذهب يشهد تراجعاً مع ضغوط البيع في الأسواق',
                        impact: 'bearish',
                        icon: '📉',
                        color: '#f87171',
                        bgColor: 'rgba(239,68,68,0.1)'
                    };
                } else {
                    return {
                        title: 'أسعار الذهب تتداول في نطاق ضيق',
                        explanation: marketState.explanation || 'السوق في حالة تذبذب وترقب للاتجاه القادم',
                        impact: 'neutral',
                        icon: '📊',
                        color: '#fbbf24',
                        bgColor: 'rgba(251,191,36,0.1)'
                    };
                }
            };

            const autoNews = getMarketNews();
            const impactText = autoNews.impact === 'bullish' ? 'صعودي' : autoNews.impact === 'bearish' ? 'هبوطي' : 'محايد';

            container.innerHTML = `
                <!-- خبر تلقائي بناءً على حالة السوق -->
                <div class="rounded-xl p-4" style="background: linear-gradient(135deg, ${autoNews.bgColor}, transparent); border: 1px solid ${autoNews.color}33;">
                    
                    <!-- العنوان -->
                    <h4 class="font-bold text-white mb-3">${autoNews.title}</h4>
                    
                    <!-- السعر العالمي الحالي - TradingView Widget -->
                    <div id="news-tradingview-price-auto" class="rounded-lg mb-3 overflow-hidden border border-gold-500/30" style="height: 120px;"></div>
                    
                    <!-- مؤشرات -->
                    <div class="grid grid-cols-2 gap-2 text-center">
                        <div class="bg-zinc-800/50 rounded-lg p-2">
                            <p class="text-xs text-zinc-500">حالة السوق</p>
                            <p class="text-sm font-bold" style="color: ${autoNews.color};">${autoNews.icon} ${impactText}</p>
                        </div>
                        <div class="bg-zinc-800/50 rounded-lg p-2">
                            <p class="text-xs text-zinc-500">المصدر</p>
                            <p class="text-xs font-bold text-white">تحليل لحظي</p>
                        </div>
                    </div>
                    
                    <!-- الشرح -->
                    <div class="mt-3 p-2 rounded-lg bg-zinc-900/50 border-r-2" style="border-color: ${autoNews.color};">
                        <p class="text-xs text-zinc-400">${autoNews.explanation}</p>
                    </div>
                </div>
            `;
            
            // 🔴 تحميل TradingView Widget للسعر
            this.loadTradingViewWidget('news-tradingview-price-auto');
        }
    },
    
    /**
     * 🔴 تحميل TradingView Widget ديناميكياً
     */
    loadTradingViewWidget(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        // مسح المحتوى السابق
        container.innerHTML = '';
        
        // إنشاء wrapper للـ widget
        const widgetContainer = document.createElement('div');
        widgetContainer.className = 'tradingview-widget-container';
        widgetContainer.style.cssText = 'height: 100%; width: 100%;';
        
        const widgetDiv = document.createElement('div');
        widgetDiv.className = 'tradingview-widget-container__widget';
        widgetDiv.style.cssText = 'height: 100%; width: 100%;';
        widgetContainer.appendChild(widgetDiv);
        
        // إنشاء السكريبت - استخدام Mini Symbol Overview
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = 'https://s3.tradingview.com/external-embedding/embed-widget-mini-symbol-overview.js';
        script.async = true;
        script.innerHTML = JSON.stringify({
            "symbol": "TVC:GOLD",
            "width": "100%",
            "height": "100%",
            "locale": "ar_AE",
            "dateRange": "1D",
            "colorTheme": "dark",
            "isTransparent": true,
            "autosize": true,
            "largeChartUrl": "",
            "noTimeScale": true,
            "chartOnly": false
        });
        
        widgetContainer.appendChild(script);
        container.appendChild(widgetContainer);
    },

    /**
     * 🔗 مزامنة الأخبار مع حالة السوق الفعلية
     * يضمن توافق اتجاه الخبر مع حركة السعر الحالية
     */
    syncNewsWithMarket() {
        const marketState = this.state.currentMarketState;
        const newsImpact = this.state.newsImpact;
        
        if (!marketState || !newsImpact) return;
        
        const allNews = newsImpact.HighImpactNews || [];
        if (allNews.length === 0) return;
        
        // تحديث تصنيف الأخبار ليتوافق مع حركة السعر الفعلية
        allNews.forEach(news => {
            // إذا كان هناك تناقض بين الخبر وحالة السوق
            const newsDirection = news.GoldImpactDirection || news.ImpactType || 'neutral';
            const marketDirection = marketState.status;
            
            // إذا الخبر يقول صعودي لكن السوق هابط (أو العكس)
            if ((newsDirection === 'bullish' && marketDirection === 'bearish') ||
                (newsDirection === 'bearish' && marketDirection === 'bullish')) {
                // تحديث الشرح ليوضح التناقض
                news.ImpactExplanation = this.getConflictExplanation(newsDirection, marketDirection, news.Title);
            }
        });
        
        // إعادة رسم قسم الأخبار
        this.updateNewsImpact(newsImpact);
    },
    
    /**
     * توليد شرح عند وجود تناقض بين الخبر وحركة السوق
     */
    getConflictExplanation(newsDirection, marketDirection, title) {
        const priceChange = this.state.priceChangePercent || 0;
        
        if (newsDirection === 'bullish' && marketDirection === 'bearish') {
            return `⚠️ رغم أن الخبر إيجابي، السعر انخفض ${Math.abs(priceChange).toFixed(2)}% - السوق لم يستجب للخبر بعد أو هناك عوامل أخرى مؤثرة`;
        } else if (newsDirection === 'bearish' && marketDirection === 'bullish') {
            return `⚠️ رغم أن الخبر سلبي، السعر ارتفع ${Math.abs(priceChange).toFixed(2)}% - السوق يتجاهل هذا الخبر أو هناك عوامل إيجابية أقوى`;
        }
        return '';
    },

    /**
     * Load recent alerts
     */
    async loadRecentAlerts() {
        try {
            const alerts = await API.getAlerts(5);
            this.state.alerts = alerts || [];

            const container = document.getElementById('recent-alerts');
            if (!container) return;

            if (this.state.alerts.length === 0) {
                container.innerHTML = `
                    <div class="text-center py-8 text-zinc-500">
                        <span class="text-4xl mb-2 block">🔔</span>
                        <p>لا توجد تنبيهات</p>
                    </div>
                `;
                return;
            }

            container.innerHTML = this.state.alerts.map(alert => {
                const config = CONFIG.ALERT_TYPES[alert.Type] || CONFIG.ALERT_TYPES.Price;
                return `
                    <div class="flex items-start gap-4 p-4 rounded-xl bg-zinc-800/50 ${config.class} ${!alert.IsRead ? 'border border-gold-500/30' : ''}">
                        <span class="text-2xl">${config.icon}</span>
                        <div class="flex-1">
                            <h4 class="font-bold text-white">${alert.Title}</h4>
                            <p class="text-sm text-zinc-400">${alert.Message}</p>
                            <p class="text-xs text-zinc-500 mt-2">${this.formatTime(alert.CreatedAt)}</p>
                        </div>
                    </div>
                `;
            }).join('');

        } catch (error) {
            // Silent fail - backend not available
        }
    },

    /**
     * Update alerts badge
     */
    updateAlertsBadge() {
        const badge = document.getElementById('alerts-badge');
        if (!badge) return;

        if (this.state.unreadAlerts > 0) {
            badge.textContent = this.state.unreadAlerts > 9 ? '9+' : this.state.unreadAlerts;
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
    },

    /**
     * Show toast notification
     */
    showToast(alert) {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const config = CONFIG.ALERT_TYPES[alert.Type] || CONFIG.ALERT_TYPES.Price;

        const toast = document.createElement('div');
        toast.className = `toast max-w-sm bg-zinc-900 border border-zinc-700 ${config.class} rounded-xl p-4 shadow-2xl`;
        toast.innerHTML = `
            <div class="flex items-start gap-3">
                <span class="text-2xl">${config.icon}</span>
                <div class="flex-1">
                    <h4 class="font-bold text-white">${alert.Title}</h4>
                    <p class="text-sm text-zinc-400">${alert.Message}</p>
                </div>
                <button class="text-zinc-500 hover:text-white" onclick="this.parentElement.parentElement.remove()">✕</button>
            </div>
        `;

        container.appendChild(toast);

        // Auto remove after 10 seconds
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, CONFIG.TOAST_DURATION);
    },

    /**
     * Show error message
     */
    showError(message) {
        this.showToast({
            Type: 'Price',
            Title: 'خطأ',
            Message: message
        });
    },

    /**
     * Update time display
     */
    updateTime() {
        const el = document.getElementById('current-time');
        if (el) {
            el.textContent = new Date().toLocaleString('ar-KW', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        }
    },

    /**
     * Format timestamp
     */
    formatTime(timestamp) {
        if (!timestamp) return '--';
        return new Date(timestamp).toLocaleString('ar-KW', {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Refresh button
        const refreshBtn = document.getElementById('refresh-btn');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.loadDashboard());
        }

        // Alerts button
        const alertsBtn = document.getElementById('alerts-btn');
        if (alertsBtn) {
            alertsBtn.addEventListener('click', () => {
                window.location.href = 'pages/alerts.html';
            });
        }
    },

    /**
     * Setup auto-refresh with countdown
     * تحديث كل 4 ثوانٍ حسب السعر العالمي (مثل دار السبائك)
     */
    setupAutoRefreshWithCountdown() {
        const refreshInterval = CONFIG.REFRESH_INTERVALS.DASHBOARD / 1000; // 4 ثوانٍ
        this.state.countdown = refreshInterval;

        // تحديث العد التنازلي كل ثانية
        setInterval(() => {
            this.state.countdown--;

            const countdownEl = document.getElementById('countdown-seconds');
            if (countdownEl) {
                countdownEl.textContent = Math.max(0, this.state.countdown);
            }

            // عند الوصول للصفر، نحدث البيانات
            if (this.state.countdown <= 0) {
                this.loadDashboard();
                this.state.countdown = refreshInterval;
            }
        }, 1000);
    },

    /**
     * Setup auto-refresh (deprecated - use setupAutoRefreshWithCountdown)
     */
    setupAutoRefresh() {
        this.setupAutoRefreshWithCountdown();
    },

    // ═══════════════════════════════════════════════════════════════════
    // 📊 الرسم البياني التفاعلي (مثل دار السبائك)
    // ═══════════════════════════════════════════════════════════════════
    
    chartInstance: null,
    chartData: [],
    chartPeriod: '10m',
    
    /**
     * تهيئة الرسم البياني المحسّن
     */
    initPriceChart() {
        const ctx = document.getElementById('goldPriceChart');
        if (!ctx) {
            console.warn('Chart canvas not found');
            return;
        }
        
        // التحقق من وجود Chart.js
        if (typeof Chart === 'undefined') {
            console.error('Chart.js not loaded');
            return;
        }
        
        // تدمير الرسم البياني القديم إذا وجد
        if (this.chartInstance) {
            this.chartInstance.destroy();
            this.chartInstance = null;
        }
        
        // إعداد أزرار الفترات الزمنية
        this.setupChartPeriodButtons();
        
        console.log('🎨 Initializing enhanced price chart...');
        
        // Gradient للخلفية
        const gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 350);
        gradient.addColorStop(0, 'rgba(251, 191, 36, 0.4)');
        gradient.addColorStop(0.5, 'rgba(251, 191, 36, 0.1)');
        gradient.addColorStop(1, 'rgba(251, 191, 36, 0)');
        
        // إنشاء الرسم البياني
        this.chartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'سعر 24K (د.ك)',
                    data: [],
                    borderColor: '#fbbf24',
                    backgroundColor: gradient,
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 0,
                    pointHoverRadius: 8,
                    pointBackgroundColor: '#fbbf24',
                    pointBorderColor: '#000',
                    pointBorderWidth: 2,
                    pointHoverBackgroundColor: '#fff',
                    pointHoverBorderColor: '#fbbf24',
                    pointHoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 500,
                    easing: 'easeInOutQuart'
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        enabled: true,
                        backgroundColor: 'rgba(0, 0, 0, 0.95)',
                        titleColor: '#fbbf24',
                        titleFont: { size: 14, weight: 'bold' },
                        bodyColor: '#fff',
                        bodyFont: { size: 16 },
                        borderColor: '#fbbf24',
                        borderWidth: 2,
                        padding: 16,
                        cornerRadius: 12,
                        displayColors: false,
                        callbacks: {
                            title: (items) => {
                                return `⏰ ${items[0].label}`;
                            },
                            label: (item) => {
                                return `💰 ${item.parsed.y.toFixed(3)} د.ك`;
                            },
                            afterLabel: (item) => {
                                const data = this.chartInstance.data.datasets[0].data;
                                if (item.dataIndex > 0) {
                                    const prev = data[item.dataIndex - 1];
                                    const change = ((item.parsed.y - prev) / prev * 100).toFixed(2);
                                    const arrow = change >= 0 ? '▲' : '▼';
                                    return `${arrow} ${Math.abs(change)}%`;
                                }
                                return '';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        display: true,
                        grid: {
                            color: 'rgba(255, 255, 255, 0.03)',
                            drawBorder: false
                        },
                        ticks: {
                            color: '#71717a',
                            font: { size: 10 },
                            maxTicksLimit: 8,
                            maxRotation: 0
                        }
                    },
                    y: {
                        position: 'right',
                        grid: {
                            color: 'rgba(255, 255, 255, 0.05)',
                            drawBorder: false
                        },
                        ticks: {
                            color: '#fbbf24',
                            font: { size: 11, weight: 'bold' },
                            callback: (value) => `${value.toFixed(3)}`
                        }
                    }
                },
                onHover: (event, elements) => {
                    ctx.style.cursor = elements.length ? 'crosshair' : 'default';
                }
            }
        });
        
        // إضافة بيانات أولية للعرض
        this.generateInitialChartData();
        
        console.log('✅ Chart initialized successfully');
    },
    
    /**
     * إنشاء بيانات أولية للرسم البياني
     */
    generateInitialChartData() {
        const basePrice = this.state.karatPrices?.[24] || 47.893;
        const now = Date.now();
        
        // إنشاء 20 نقطة أولية
        for (let i = 19; i >= 0; i--) {
            const time = new Date(now - (i * 30000)); // كل 30 ثانية
            const variation = (Math.random() - 0.5) * 0.1; // تغير عشوائي صغير
            const price = basePrice + variation;
            
            const timeLabel = time.toLocaleTimeString('ar-KW', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
            
            this.chartData.push({
                time: timeLabel,
                price: price,
                timestamp: time.getTime()
            });
        }
        
        // تحديث الرسم البياني
        this.chartInstance.data.labels = this.chartData.map(d => d.time);
        this.chartInstance.data.datasets[0].data = this.chartData.map(d => d.price);
        this.chartInstance.update('none');
        
        // تحديث الإحصائيات
        this.updateChartStats();
    },
    
    /**
     * إعداد أزرار الفترات الزمنية
     */
    setupChartPeriodButtons() {
        const buttons = document.querySelectorAll('.chart-period-btn');
        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                // إزالة active من الكل
                buttons.forEach(b => {
                    b.classList.remove('active', 'bg-gold-500', 'text-black');
                    b.classList.add('bg-zinc-700', 'text-zinc-300');
                });
                
                // إضافة active للزر المحدد
                btn.classList.add('active', 'bg-gold-500', 'text-black');
                btn.classList.remove('bg-zinc-700', 'text-zinc-300');
                
                // تحديث الفترة
                this.chartPeriod = btn.dataset.period;
                this.updateChartTimeScale();
            });
        });
    },
    
    /**
     * تحديث مقياس الوقت حسب الفترة المختارة
     */
    updateChartTimeScale() {
        if (!this.chartInstance) return;
        
        const periodConfig = {
            '1m': { minutes: 1, maxPoints: 20 },
            '5m': { minutes: 5, maxPoints: 30 },
            '10m': { minutes: 10, maxPoints: 40 },
            '30m': { minutes: 30, maxPoints: 60 },
            '1h': { minutes: 60, maxPoints: 80 }
        };
        
        const config = periodConfig[this.chartPeriod] || periodConfig['10m'];
        const now = Date.now();
        const cutoff = now - (config.minutes * 60000);
        
        // تصفية البيانات حسب الفترة
        const filteredData = this.chartData.filter(d => d.timestamp >= cutoff);
        
        // تحديث الرسم البياني
        this.chartInstance.data.labels = filteredData.map(d => d.time);
        this.chartInstance.data.datasets[0].data = filteredData.map(d => d.price);
        this.chartInstance.update('active');
        
        // تحديث الإحصائيات
        this.updateChartStats();
    },
    
    /**
     * تحديث إحصائيات الرسم البياني
     */
    updateChartStats() {
        const prices = this.chartInstance?.data.datasets[0].data || [];
        if (prices.length === 0) return;
        
        const high = Math.max(...prices);
        const low = Math.min(...prices);
        const avg = prices.reduce((a, b) => a + b, 0) / prices.length;
        const current = prices[prices.length - 1];
        const first = prices[0];
        const changePercent = ((current - first) / first * 100);
        
        // تحديث العناصر
        const highEl = document.getElementById('chart-high');
        const lowEl = document.getElementById('chart-low');
        const avgEl = document.getElementById('chart-avg');
        const currentEl = document.getElementById('chart-current-price');
        const changeEl = document.getElementById('chart-price-change');
        const timeEl = document.getElementById('chart-update-time');
        
        if (highEl) highEl.textContent = `${high.toFixed(3)} د.ك`;
        if (lowEl) lowEl.textContent = `${low.toFixed(3)} د.ك`;
        if (avgEl) avgEl.textContent = `${avg.toFixed(3)} د.ك`;
        if (currentEl) currentEl.textContent = `${current.toFixed(3)} د.ك`;
        
        if (changeEl) {
            const arrow = changePercent >= 0 ? '▲' : '▼';
            const color = changePercent >= 0 ? 'text-green-400' : 'text-red-400';
            changeEl.className = `text-sm ${color}`;
            changeEl.textContent = `${arrow} ${Math.abs(changePercent).toFixed(2)}%`;
        }
        
        if (timeEl) {
            timeEl.textContent = new Date().toLocaleTimeString('ar-KW');
        }
    },
    
    /**
     * إضافة نقطة جديدة للرسم البياني
     */
    addChartDataPoint(price) {
        if (!this.chartInstance) {
            this.initPriceChart();
            if (!this.chartInstance) return;
        }
        
        const now = new Date();
        const timeLabel = now.toLocaleTimeString('ar-KW', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
        
        // إضافة البيانات الجديدة
        this.chartInstance.data.labels.push(timeLabel);
        this.chartInstance.data.datasets[0].data.push(price);
        
        // حفظ في chartData أيضاً
        this.chartData.push({
            time: timeLabel,
            price: price,
            timestamp: now.getTime()
        });
        
        // الاحتفاظ بعدد محدود من النقاط
        const maxPoints = this.getMaxPointsForPeriod();
        while (this.chartInstance.data.labels.length > maxPoints) {
            this.chartInstance.data.labels.shift();
            this.chartInstance.data.datasets[0].data.shift();
            this.chartData.shift();
        }
        
        // تحديث الرسم البياني مع animation
        this.chartInstance.update('active');
        
        // تحديث الإحصائيات
        this.updateChartStats();
    },
    
    /**
     * الحصول على عدد النقاط حسب الفترة
     */
    getMaxPointsForPeriod() {
        const pointsConfig = {
            '1m': 20,
            '5m': 30,
            '10m': 40,
            '30m': 60,
            '1h': 80
        };
        return pointsConfig[this.chartPeriod] || 40;
    },
    
    /**
     * [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
     */
    updateAllKaratPrices(ouncePrice) {
        // تم تعطيل هذه الوظيفة مؤقتاً
    }
};

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
    App.init();
    // تهيئة الرسم البياني بعد تحميل الصفحة
    setTimeout(() => App.initPriceChart(), 500);
});
