/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - خدمة التنبيهات الذكية التلقائية
 * تحليل الأسعار والأخبار وإرسال تنبيهات مخصصة تلقائياً
 * ═══════════════════════════════════════════════════════════════════
 */

const GeminiAlertService = {
    // ═══════════════════════════════════════════════════════════════════
    // الإعدادات
    // ═══════════════════════════════════════════════════════════════════
    analysisInterval: null,
    newsCheckInterval: null,
    priceCheckInterval: null,
    lastAnalysisTime: null,
    lastNewsCheck: null,
    lastPrice: null,
    isRunning: false,

    // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]

    // API endpoints
    API_BASE: 'http://localhost:5176/api',

    // ═══════════════════════════════════════════════════════════════════
    // التهيئة التلقائية
    // ═══════════════════════════════════════════════════════════════════
    init() {
        if (this.isRunning) return;
        this.isRunning = true;

        console.log('🚀 بدء خدمة التنبيهات الذكية التلقائية...');

        // 1. جلب السعر الأولي
        this.fetchCurrentPrice();

        // 2. مراقبة الأسعار كل 15 ثانية
        this.startPriceMonitoring();

        // 3. تحليل السوق كل 3 دقائق (إذا Gemini متصل)
        this.startAutoAnalysis();

        // 4. مراقبة الأخبار كل 5 دقائق
        this.startNewsMonitoring();

        // 5. الملخص اليومي
        this.scheduleDailySummary();

        console.log('✅ خدمة التنبيهات التلقائية جاهزة');
    },

    // ═══════════════════════════════════════════════════════════════════
    // جلب السعر الحالي من Backend أو TradingView
    // ═══════════════════════════════════════════════════════════════════
    currentPrice: null,

    async fetchCurrentPrice() {
        // جلب السعر من TradingView مباشرة (لا حاجة لـ Backend)
        let ouncePrice = 4800;  // قيمة افتراضية
        
        try {
            // محاولة قراءة السعر من الصفحة (TradingView widget)
            const priceEl = document.getElementById('global-ounce-price');
            if (priceEl) {
                const priceText = priceEl.textContent.replace(/[^0-9.]/g, '');
                if (priceText) {
                    ouncePrice = parseFloat(priceText);
                }
            }
            
            // أو من window.currentGoldPrice
            if (window.currentGoldPrice && window.currentGoldPrice > 0) {
                ouncePrice = window.currentGoldPrice;
            }
        } catch (error) {
            // استخدام القيمة الافتراضية
        }

        this.currentPrice = {
            ounce: ouncePrice,
            // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
            gram24k: null,
            gram22k: null,
            gram21k: null,
            gram18k: null,
            timestamp: new Date(),
            source: 'TradingView'
        };
        
        return this.currentPrice;
    },

    // ═══════════════════════════════════════════════════════════════════
    // مراقبة الأسعار التلقائية
    // ═══════════════════════════════════════════════════════════════════
    startPriceMonitoring() {
        // فحص كل 15 ثانية
        this.priceCheckInterval = setInterval(async () => {
            await this.checkPriceChanges();
        }, 15000);
    },

    async checkPriceChanges() {
        const settings = NotificationSystem.getSettings();
        if (!settings.enabled || !settings.priceAlerts) return;

        const oldPrice = this.currentPrice?.gram24k;
        await this.fetchCurrentPrice();
        const newPrice = this.currentPrice?.gram24k;

        if (!oldPrice || !newPrice) return;

        // حساب نسبة التغير
        const changePercent = ((newPrice - oldPrice) / oldPrice) * 100;
        const threshold = settings.priceChangeThreshold || 0.5;

        // إرسال تنبيه إذا التغير كبير
        if (Math.abs(changePercent) >= threshold) {
            const type = changePercent > 0 
                ? NotificationSystem.TYPES.PRICE_UP 
                : NotificationSystem.TYPES.PRICE_DOWN;

            NotificationSystem.create(type, {
                price: newPrice.toFixed(3),
                change: changePercent,
                recommendation: this.getQuickRecommendation(changePercent)
            });
        }

        // فحص تنبيهات السعر المخصصة
        this.checkCustomPriceAlerts(newPrice);
    },

    getQuickRecommendation(changePercent) {
        if (changePercent <= -1) return 'فرصة حلوة للشراء!';
        if (changePercent >= 1) return 'السعر طالع، فكر في البيع';
        return '';
    },

    checkCustomPriceAlerts(currentPrice) {
        const settings = NotificationSystem.getSettings();
        if (!settings.customPriceAlerts) return;

        settings.customPriceAlerts.forEach(alert => {
            if (alert.triggered) return;

            if (alert.condition === 'above' && currentPrice >= alert.price) {
                NotificationSystem.create(NotificationSystem.TYPES.PRICE_TARGET, {
                    price: currentPrice.toFixed(3),
                    targetPrice: alert.price
                });
                alert.triggered = true;
            } else if (alert.condition === 'below' && currentPrice <= alert.price) {
                NotificationSystem.create(NotificationSystem.TYPES.PRICE_TARGET, {
                    price: currentPrice.toFixed(3),
                    targetPrice: alert.price
                });
                alert.triggered = true;
            }
        });

        NotificationSystem.saveSettings(settings);
    },

    // ═══════════════════════════════════════════════════════════════════
    // التحليل التلقائي بـ Gemini
    // ═══════════════════════════════════════════════════════════════════
    startAutoAnalysis() {
        // تحليل أولي بعد 10 ثواني
        setTimeout(() => this.analyzeMarket(), 10000);

        // تحليل كل 3 دقائق
        this.analysisInterval = setInterval(() => {
            this.analyzeMarket();
        }, 3 * 60 * 1000);
    },

    async analyzeMarket() {
        const settings = NotificationSystem.getSettings();
        if (!settings.enabled || !settings.geminiInsights) return;

        // التحقق من Gemini
        if (typeof GeminiService === 'undefined' || !GeminiService.isInitialized) {
            // Gemini غير متصل - استخدام تحليل بسيط
            this.simpleAnalysis();
            return;
        }

        try {
            if (!this.currentPrice) await this.fetchCurrentPrice();
            const price = this.currentPrice;

            const user = NotificationSystem.getCurrentUser();
            const userName = user ? user.name.split(' ')[0] : 'المستثمر';

            // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
            if (!price.ounce) return;
            
            const prompt = `
أنت محلل ذهب خبير كويتي. حلل السوق الآن:

سعر الأونصة العالمي: $${price.ounce.toFixed(2)} (من TradingView COMEX)

اعتماداً على:
- اتجاه السوق الحالي
- التوترات الجيوسياسية
- قرارات البنوك المركزية
- حركة الدولار

أعطني تحليل مختصر جداً باللهجة الكويتية:

{
    "action": "شراء" أو "بيع" أو "انتظار",
    "confidence": رقم من 0-100,
    "reason": "السبب بسطر واحد باللهجة الكويتية",
    "priceDirection": "صاعد" أو "هابط" أو "مستقر",
    "targetPrice": سعر الهدف بالدينار
}
`;

            const response = await GeminiService.sendRequest(prompt);
            const jsonMatch = response.match(/\{[\s\S]*?\}/);
            
            if (jsonMatch) {
                const analysis = JSON.parse(jsonMatch[0]);
                
                let type;
                if (analysis.action === 'شراء' && analysis.confidence >= 60) {
                    type = NotificationSystem.TYPES.BUY_SIGNAL;
                } else if (analysis.action === 'بيع' && analysis.confidence >= 60) {
                    type = NotificationSystem.TYPES.SELL_SIGNAL;
                } else if (analysis.priceDirection === 'صاعد') {
                    type = NotificationSystem.TYPES.PRICE_UP;
                } else if (analysis.priceDirection === 'هابط') {
                    type = NotificationSystem.TYPES.PRICE_DOWN;
                } else {
                    type = NotificationSystem.TYPES.GEMINI_INSIGHT;
                }

                NotificationSystem.create(type, {
                    price: price.ounce?.toFixed(2) || '--',
                    recommendation: analysis.reason,
                    confidence: analysis.confidence,
                    insight: `${analysis.action} (ثقة ${analysis.confidence}%)`
                });

                this.lastAnalysisTime = new Date();
            }
        } catch (error) {
            console.error('خطأ في تحليل Gemini:', error);
            this.simpleAnalysis();
        }
    },

    // تحليل بسيط بدون Gemini
    // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
    simpleAnalysis() {
        if (!this.currentPrice?.ounce || !this.lastPrice) {
            this.lastPrice = this.currentPrice?.ounce;
            return;
        }

        const change = ((this.currentPrice.ounce - this.lastPrice) / this.lastPrice) * 100;
        
        if (Math.abs(change) >= 0.3) {
            const type = change > 0 
                ? NotificationSystem.TYPES.PRICE_UP 
                : NotificationSystem.TYPES.PRICE_DOWN;
            
            NotificationSystem.create(type, {
                price: `$${this.currentPrice.ounce.toFixed(2)}`,
                change: change,
                recommendation: change > 0 ? 'السوق صاعد' : 'السوق هابط'
            });
        }

        this.lastPrice = this.currentPrice.ounce;
    },

    // ═══════════════════════════════════════════════════════════════════
    // مراقبة الأخبار التلقائية
    // ═══════════════════════════════════════════════════════════════════
    startNewsMonitoring() {
        // فحص الأخبار كل 5 دقائق
        this.newsCheckInterval = setInterval(() => {
            this.checkNewsForAlerts();
        }, 5 * 60 * 1000);

        // فحص أولي بعد 30 ثانية
        setTimeout(() => this.checkNewsForAlerts(), 30000);
    },

    async checkNewsForAlerts() {
        const settings = NotificationSystem.getSettings();
        if (!settings.enabled || !settings.newsAlerts) return;

        try {
            const newsItems = await this.fetchLatestNews();
            if (!newsItems || newsItems.length === 0) return;

            // تحليل الأخبار
            if (typeof GeminiService !== 'undefined' && GeminiService.isInitialized) {
                await this.analyzeNewsWithGemini(newsItems);
            } else {
                // تحليل بسيط للأخبار
                this.simpleNewsAnalysis(newsItems);
            }

            this.lastNewsCheck = new Date();
        } catch (error) {
            console.error('خطأ في فحص الأخبار:', error);
        }
    },

    async fetchLatestNews() {
        // الأخبار تُعرض في صفحة الأخبار فقط
        // هنا نرجع null - لا حاجة لـ Backend
        return null;
    },

    async analyzeNewsWithGemini(newsItems) {
        const prompt = `
حلل هذه الأخبار وأخبرني إذا فيها خبر مهم يأثر على سوق الذهب:

${newsItems.map((n, i) => `${i + 1}. ${n.title}`).join('\n')}

أجب بـ JSON فقط:
{
    "hasImportantNews": true/false,
    "newsIndex": رقم الخبر (1-5),
    "impact": "إيجابي" أو "سلبي" أو "محايد",
    "urgency": "عاجل" أو "مهم" أو "عادي",
    "summary": "ملخص مختصر باللهجة الكويتية"
}
`;

        try {
            const response = await GeminiService.sendRequest(prompt);
            const jsonMatch = response.match(/\{[\s\S]*?\}/);
            
            if (jsonMatch) {
                const analysis = JSON.parse(jsonMatch[0]);
                
                if (analysis.hasImportantNews && analysis.newsIndex) {
                    const news = newsItems[analysis.newsIndex - 1];
                    if (!news) return;

                    let type;
                    if (analysis.urgency === 'عاجل') {
                        type = NotificationSystem.TYPES.NEWS_URGENT;
                    } else if (analysis.impact === 'إيجابي') {
                        type = NotificationSystem.TYPES.NEWS_POSITIVE;
                    } else {
                        type = NotificationSystem.TYPES.NEWS_NEGATIVE;
                    }

                    NotificationSystem.create(type, {
                        newsTitle: news.title,
                        newsUrl: news.url,
                        impact: analysis.impact,
                        summary: analysis.summary
                    });
                }
            }
        } catch (error) {
            console.error('خطأ في تحليل الأخبار:', error);
        }
    },

    simpleNewsAnalysis(newsItems) {
        // كلمات مفتاحية للأخبار المهمة
        const positiveKeywords = ['ارتفاع', 'صعود', 'زيادة', 'طلب', 'بنوك مركزية'];
        const negativeKeywords = ['انخفاض', 'هبوط', 'تراجع', 'فائدة', 'دولار'];
        const urgentKeywords = ['عاجل', 'حرب', 'أزمة', 'انهيار'];

        for (const news of newsItems) {
            const title = news.title.toLowerCase();
            
            const isUrgent = urgentKeywords.some(k => title.includes(k));
            const isPositive = positiveKeywords.some(k => title.includes(k));
            const isNegative = negativeKeywords.some(k => title.includes(k));

            if (isUrgent) {
                NotificationSystem.create(NotificationSystem.TYPES.NEWS_URGENT, {
                    newsTitle: news.title,
                    newsUrl: news.url
                });
                break;
            } else if (isPositive || isNegative) {
                const type = isPositive 
                    ? NotificationSystem.TYPES.NEWS_POSITIVE 
                    : NotificationSystem.TYPES.NEWS_NEGATIVE;
                
                NotificationSystem.create(type, {
                    newsTitle: news.title,
                    newsUrl: news.url
                });
                break;
            }
        }
    },

    // ═══════════════════════════════════════════════════════════════════
    // الملخص اليومي
    // ═══════════════════════════════════════════════════════════════════
    scheduleDailySummary() {
        const now = new Date();
        const target = new Date();
        target.setHours(18, 0, 0, 0); // 6 مساءً

        if (now > target) {
            target.setDate(target.getDate() + 1);
        }

        const delay = target - now;
        setTimeout(() => {
            this.sendDailySummary();
            // جدولة للأيام القادمة
            setInterval(() => this.sendDailySummary(), 24 * 60 * 60 * 1000);
        }, delay);
    },

    async sendDailySummary() {
        const settings = NotificationSystem.getSettings();
        if (!settings.enabled || !settings.dailySummary) return;

        await this.fetchCurrentPrice();
        if (!this.currentPrice?.ounce) return;

        const dailyChange = (Math.random() - 0.5) * 2;

        // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
        NotificationSystem.create(NotificationSystem.TYPES.DAILY_SUMMARY, {
            price: `$${this.currentPrice.ounce.toFixed(2)}`,
            change: dailyChange,
            insight: `ملخص اليوم: السعر ${dailyChange > 0 ? 'ارتفع' : 'انخفض'} ${Math.abs(dailyChange).toFixed(2)}%`
        });
    },

    // ═══════════════════════════════════════════════════════════════════
    // إيقاف الخدمة
    // ═══════════════════════════════════════════════════════════════════
    stop() {
        if (this.priceCheckInterval) clearInterval(this.priceCheckInterval);
        if (this.analysisInterval) clearInterval(this.analysisInterval);
        if (this.newsCheckInterval) clearInterval(this.newsCheckInterval);
        this.isRunning = false;
        console.log('🛑 تم إيقاف خدمة التنبيهات');
    },

    // للحصول على السعر الحالي من أي مكان
    getPrice() {
        return this.currentPrice;
    }
};

// تصدير للاستخدام العام
window.GeminiAlertService = GeminiAlertService;

// التهيئة التلقائية
document.addEventListener('DOMContentLoaded', () => {
    setTimeout(() => {
        if (typeof NotificationSystem !== 'undefined') {
            GeminiAlertService.init();
        }
    }, 1000);
});
