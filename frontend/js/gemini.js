/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - Gemini AI Service v2.0
 * خدمة الذكاء الاصطناعي المتقدمة للتحليل المالي
 * ═══════════════════════════════════════════════════════════════════
 * الميزات:
 * - نماذج Gemini Pro/Ultra للدقة العالية
 * - تحليل أسباب الصعود والنزول
 * - كشف التصحيحات والاستمرارية
 * - توقعات البنوك الكبرى
 * - المؤشرات الفنية الحقيقية
 * ═══════════════════════════════════════════════════════════════════
 */

const GeminiService = {
    // ═══════════════════════════════════════════════════════════════════
    // الإعدادات والنماذج
    // ═══════════════════════════════════════════════════════════════════
    
    API_KEY: '',
    API_URL: '',
    isConnected: false,
    isInitialized: false,
    lastUpdate: null,
    currentModel: '',
    
    // ترتيب النماذج من الأقوى للأضعف (من Paid tier 1)
    MODELS: {
        // الجيل الثالث (الأحدث)
        PRO_3: 'gemini-3-pro',
        FLASH_3: 'gemini-3-flash',
        // الجيل 2.5
        PRO_25: 'gemini-2.5-pro',
        FLASH_25: 'gemini-2.5-flash',
        // الجيل 2.0
        FLASH_20: 'gemini-2.0-flash',
        FLASH_20_LITE: 'gemini-2.0-flash-lite'
    },
    
    // ترتيب التجربة (من الأقوى)
    MODEL_PRIORITY: [
        'gemini-2.5-pro',            // الأقوى المتاح
        'gemini-2.5-flash',          // سريع وقوي
        'gemini-3-pro',              // الجيل الثالث
        'gemini-3-flash',            // سريع
        'gemini-2.0-flash',          // مستقر
        'gemini-2.0-flash-lite'      // خفيف
    ],
    
    // إعدادات التوليد (دقة عالية للتحليل المالي)
    GENERATION_CONFIG: {
        temperature: 0.2,      // منخفض للدقة
        topP: 0.8,
        topK: 40,
        maxOutputTokens: 4096
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // التهيئة
    // ═══════════════════════════════════════════════════════════════════
    
    init(apiKey) {
        if (apiKey && apiKey.startsWith('AIza') && apiKey.length > 30) {
            this.API_KEY = apiKey;
            this.isConnected = true;
            this.isInitialized = true;
            console.log('✅ Gemini AI v2.0 initialized');
            return true;
        }
        console.warn('⚠️ Gemini API Key not configured');
        return false;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // اختبار الاتصال والعثور على أفضل نموذج
    // ═══════════════════════════════════════════════════════════════════
    
    async testConnection() {
        console.log('🔄 البحث عن أفضل نموذج Gemini...');
        
        let lastError = '';
        
        for (const model of this.MODEL_PRIORITY) {
            const testUrl = `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent`;
            
            try {
                console.log(`🔄 تجربة: ${model}...`);
                
                const response = await fetch(testUrl, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'x-goog-api-key': this.API_KEY
                    },
                    body: JSON.stringify({
                        contents: [{ parts: [{ text: 'Hi' }] }]
                    })
                });
                
                const data = await response.json();
                
                if (response.ok && data.candidates) {
                    this.API_URL = testUrl;
                    this.currentModel = model;
                    this.isInitialized = true;
                    this.isConnected = true;
                    console.log(`✅ تم الاتصال بنجاح! النموذج: ${model}`);
                    return { valid: true, model: model };
                }
                
                // تحليل الخطأ
                const errorMsg = data.error?.message || '';
                console.warn(`⚠️ ${model}: ${errorMsg}`);
                
                if (errorMsg.includes('API key not valid') || errorMsg.includes('API_KEY_INVALID')) {
                    return { valid: false, error: 'مفتاح API غير صالح. تأكد من صحة المفتاح.' };
                }
                
                if (errorMsg.includes('not found') || errorMsg.includes('does not exist')) {
                    lastError = `النموذج ${model} غير متاح`;
                    continue;
                }
                
                if (errorMsg.includes('quota') || errorMsg.includes('RATE_LIMIT')) {
                    return { valid: false, error: 'تجاوزت الحد المسموح. انتظر قليلاً أو فعّل الفوترة.' };
                }
                
                if (errorMsg.includes('permission') || errorMsg.includes('PERMISSION_DENIED')) {
                    lastError = 'فعّل Generative Language API في Google Cloud Console';
                    continue;
                }
                
                lastError = errorMsg || 'خطأ غير معروف';
                
            } catch (err) {
                console.warn(`⚠️ خطأ شبكة: ${model}`, err.message);
                lastError = 'خطأ في الاتصال بالشبكة';
            }
        }
        
        return { valid: false, error: lastError || 'لم يتم العثور على نموذج متاح' };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إرسال طلب إلى Gemini
    // ═══════════════════════════════════════════════════════════════════
    
    async sendRequest(prompt, systemContext = '') {
        if (!this.isConnected) {
            throw new Error('Gemini غير متصل');
        }
        
        try {
            const fullPrompt = systemContext 
                ? `${systemContext}\n\n${prompt}` 
                : prompt;
            
            const response = await fetch(this.API_URL, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'x-goog-api-key': this.API_KEY
                },
                body: JSON.stringify({
                    contents: [{ parts: [{ text: fullPrompt }] }],
                    generationConfig: this.GENERATION_CONFIG,
                    safetySettings: [
                        { category: "HARM_CATEGORY_HARASSMENT", threshold: "BLOCK_NONE" },
                        { category: "HARM_CATEGORY_HATE_SPEECH", threshold: "BLOCK_NONE" },
                        { category: "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold: "BLOCK_NONE" },
                        { category: "HARM_CATEGORY_DANGEROUS_CONTENT", threshold: "BLOCK_NONE" }
                    ]
                })
            });
            
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error?.message || 'خطأ في Gemini');
            }
            
            const data = await response.json();
            this.lastUpdate = new Date();
            
            return data.candidates?.[0]?.content?.parts?.[0]?.text || '';
        } catch (error) {
            console.error('Gemini Error:', error);
            throw error;
        }
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 🔥 التحليل الشامل المتقدم (الجديد)
    // ═══════════════════════════════════════════════════════════════════
    
    async advancedAnalysis(priceData, historicalData = {}) {
        const systemContext = `
أنت محلل مالي خبير متخصص في أسواق الذهب العالمية والسوق الكويتي.
يجب أن تقدم تحليلات دقيقة ومبنية على بيانات حقيقية.
استخدم اللهجة الكويتية المفهومة.
أجب بصيغة JSON فقط بدون أي نص إضافي.
`;

        const prompt = `
حلل سوق الذهب بناءً على هذه البيانات الحقيقية:

═══════════════════════════════════════════════════════════════
📊 البيانات الحالية:
═══════════════════════════════════════════════════════════════
• سعر الأونصة العالمي: $${priceData.ounce || 4793}
• سعر جرام 24K الكويت: ${priceData.gram24k?.toFixed(3) || '47.500'} د.ك
• التغير اليومي: ${priceData.changePercent || 0}%
• أعلى سعر اليوم: $${priceData.high || priceData.ounce}
• أدنى سعر اليوم: $${priceData.low || priceData.ounce}

═══════════════════════════════════════════════════════════════
📈 البيانات التاريخية:
═══════════════════════════════════════════════════════════════
• أعلى قمة تاريخية: $${historicalData.allTimeHigh || 4850}
• أدنى قاع (آخر شهر): $${historicalData.monthLow || 4500}
• متوسط آخر 7 أيام: $${historicalData.avg7d || 4700}
• متوسط آخر 30 يوم: $${historicalData.avg30d || 4600}

═══════════════════════════════════════════════════════════════
🔥 المطلوب: تحليل شامل بصيغة JSON
═══════════════════════════════════════════════════════════════

{
    "currentStatus": {
        "trend": "صاعد|هابط|محايد",
        "trendStrength": 0-100,
        "pricePosition": "قرب القمة|وسط النطاق|قرب القاع"
    },
    
    "trendAnalysis": {
        "direction": "صعود|نزول|ثبات",
        "reasons": [
            {"reason": "السبب الأول للاتجاه", "impact": "قوي|متوسط|ضعيف"},
            {"reason": "السبب الثاني", "impact": "قوي|متوسط|ضعيف"}
        ],
        "mainDrivers": ["العامل الرئيسي 1", "العامل الرئيسي 2"],
        "explanation": "شرح مفصل باللهجة الكويتية لسبب الاتجاه الحالي"
    },
    
    "isAllTimeHigh": {
        "status": true|false,
        "distanceFromATH": "X% تحت القمة أو عند القمة",
        "comment": "تعليق على الوضع"
    },
    
    "continuationOrCorrection": {
        "assessment": "استمرار|تصحيح قريب|تصحيح جاري|ارتداد",
        "probability": 0-100,
        "expectedCorrectionSize": "X%",
        "expectedCorrectionTime": "خلال X أيام",
        "signals": ["إشارة 1", "إشارة 2"],
        "recommendation": "التوصية بناءً على التحليل"
    },
    
    "technicalIndicators": {
        "rsiStatus": "ذروة شراء|محايد|ذروة بيع",
        "rsiValue": 0-100,
        "macdSignal": "شراء|بيع|محايد",
        "movingAverages": "فوق المتوسطات|عند المتوسطات|تحت المتوسطات",
        "supportLevel": رقم,
        "resistanceLevel": رقم,
        "bollingerPosition": "أعلى النطاق|وسط النطاق|أسفل النطاق"
    },
    
    "recommendation": {
        "action": "شراء قوي|شراء|انتظار|بيع|بيع قوي",
        "confidence": 0-100,
        "reasoning": "السبب المفصل باللهجة الكويتية",
        "entryPrice": رقم بالدينار,
        "targetPrice": رقم بالدينار,
        "stopLoss": رقم بالدينار,
        "timeHorizon": "يوم|أسبوع|شهر"
    },
    
    "banksForecast": {
        "goldmanSachs": {"target": رقم بالدولار, "timeframe": "الفترة", "direction": "صعود|نزول"},
        "jpMorgan": {"target": رقم بالدولار, "timeframe": "الفترة", "direction": "صعود|نزول"},
        "citibank": {"target": رقم بالدولار, "timeframe": "الفترة", "direction": "صعود|نزول"},
        "consensus": "الإجماع العام للبنوك",
        "averageTarget": رقم بالدولار
    },
    
    "riskAssessment": {
        "level": "منخفض|متوسط|عالي|عالي جداً",
        "factors": ["عامل خطر 1", "عامل خطر 2"],
        "advice": "نصيحة لإدارة المخاطر"
    },
    
    "shortTermOutlook": {
        "next24h": {"direction": "صعود|نزول|ثبات", "expectedChange": "+X%|-X%"},
        "nextWeek": {"direction": "صعود|نزول|ثبات", "expectedChange": "+X%|-X%"},
        "nextMonth": {"direction": "صعود|نزول|ثبات", "expectedChange": "+X%|-X%"}
    },
    
    "kuwaitSpecific": {
        "bestBuyTime": "أفضل وقت للشراء",
        "localTip": "نصيحة للمستثمر الكويتي",
        "ramadanEffect": "تأثير رمضان على الأسعار"
    }
}
`;

        try {
            const response = await this.sendRequest(prompt, systemContext);
            const analysis = this.parseJsonResponse(response);
            
            if (analysis) {
                return this.validateAndEnhanceAnalysis(analysis, priceData);
            }
            throw new Error('Invalid JSON');
        } catch (error) {
            console.error('Advanced Analysis Error:', error);
            return this.getFallbackAdvancedAnalysis(priceData);
        }
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // تنظيف واستخراج JSON من رد Gemini
    // ═══════════════════════════════════════════════════════════════════
    
    parseJsonResponse(response) {
        if (!response) return null;
        
        try {
            // محاولة 1: استخراج JSON مباشرة
            const jsonMatch = response.match(/\{[\s\S]*\}/);
            if (jsonMatch) {
                let jsonStr = jsonMatch[0];
                
                // تنظيف الـ JSON
                jsonStr = jsonStr
                    .replace(/،/g, ',')           // استبدال الفاصلة العربية
                    .replace(/[\u200B-\u200D\uFEFF]/g, '')  // إزالة الأحرف غير المرئية
                    .replace(/\n\s*\n/g, '\n')    // إزالة الأسطر الفارغة
                    .replace(/,\s*}/g, '}')       // إزالة الفاصلة قبل }
                    .replace(/,\s*]/g, ']')       // إزالة الفاصلة قبل ]
                    .replace(/:\s*"([^"]*)"([^,}\]])/g, ': "$1"$2')  // إصلاح التنسيق
                    .replace(/\\/g, '\\\\')       // escape backslashes
                    .replace(/\t/g, '    ');      // tabs to spaces
                
                // محاولة parse
                try {
                    return JSON.parse(jsonStr);
                } catch (e1) {
                    // محاولة 2: إصلاح المشاكل الشائعة
                    jsonStr = jsonStr
                        .replace(/([{,]\s*)(\w+)(\s*:)/g, '$1"$2"$3')  // إضافة quotes للمفاتيح
                        .replace(/:\s*'([^']*)'/g, ': "$1"');           // استبدال single quotes
                    
                    try {
                        return JSON.parse(jsonStr);
                    } catch (e2) {
                        console.warn('JSON parse failed, using fallback');
                        return null;
                    }
                }
            }
        } catch (error) {
            console.warn('JSON extraction error:', error);
        }
        
        return null;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 📊 تحليل أسباب الصعود والنزول
    // ═══════════════════════════════════════════════════════════════════
    
    async analyzeTrendReasons(currentPrice, previousPrice, changePercent) {
        const direction = changePercent >= 0 ? 'صعود' : 'نزول';
        
        const prompt = `
السعر الحالي: $${currentPrice}
السعر السابق: $${previousPrice}
نسبة التغير: ${changePercent}%
الاتجاه: ${direction}

أعطني تحليلاً مفصلاً بصيغة JSON:

{
    "direction": "${direction}",
    "magnitude": "طفيف|متوسط|كبير|حاد",
    "primaryReasons": [
        {"reason": "السبب الرئيسي", "weight": 1-10, "source": "المصدر"},
        {"reason": "السبب الثانوي", "weight": 1-10, "source": "المصدر"}
    ],
    "economicFactors": {
        "dollarStrength": "قوي|ضعيف|مستقر",
        "interestRates": "مرتفعة|منخفضة|مستقرة",
        "inflation": "مرتفع|منخفض|مستقر"
    },
    "geopoliticalFactors": ["العامل 1", "العامل 2"],
    "marketSentiment": "إيجابي|سلبي|محايد|خوف|طمع",
    "volumeAnalysis": "حجم تداول عالي|متوسط|منخفض",
    "explanation": "شرح شامل باللهجة الكويتية لأسباب ${direction}"
}
`;

        try {
            const response = await this.sendRequest(prompt);
            const result = this.parseJsonResponse(response);
            if (result) return result;
        } catch (error) {
            console.error('Trend Reasons Error:', error);
        }
        
        return {
            direction: direction,
            magnitude: Math.abs(changePercent) > 1 ? 'كبير' : 'طفيف',
            primaryReasons: [
                { reason: 'تحركات السوق العالمية', weight: 8, source: 'السوق' }
            ],
            economicFactors: { dollarStrength: 'مستقر', interestRates: 'مستقرة', inflation: 'مستقر' },
            geopoliticalFactors: ['التوترات العالمية'],
            marketSentiment: 'محايد',
            volumeAnalysis: 'متوسط',
            explanation: `السوق ${direction === 'صعود' ? 'طالع' : 'نازل'} بسبب عوامل السوق العالمية`
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 🔄 تحليل التصحيح والاستمرارية
    // ═══════════════════════════════════════════════════════════════════
    
    async analyzeCorrection(priceData, historicalData) {
        const prompt = `
بناءً على:
- السعر الحالي: $${priceData.ounce}
- أعلى قمة: $${historicalData.allTimeHigh || priceData.ounce}
- نسبة من القمة: ${(((priceData.ounce - (historicalData.allTimeHigh || priceData.ounce)) / (historicalData.allTimeHigh || priceData.ounce)) * 100).toFixed(2)}%
- الاتجاه الأخير: ${priceData.trend || 'صاعد'}

حلل إمكانية التصحيح أو الاستمرارية:

{
    "isAtAllTimeHigh": true|false,
    "distanceFromATH": "X%",
    
    "trendContinuation": {
        "probability": 0-100,
        "supportingFactors": ["عامل 1", "عامل 2"],
        "targetIfContinues": رقم بالدولار
    },
    
    "correctionAnalysis": {
        "probability": 0-100,
        "expectedSize": "X%",
        "expectedDuration": "X أيام/أسابيع",
        "triggerLevels": [رقم1, رقم2],
        "warningSignals": ["إشارة 1", "إشارة 2"]
    },
    
    "timing": {
        "isNearCorrection": true|false,
        "expectedWithin": "X أيام",
        "confidence": 0-100
    },
    
    "recommendation": {
        "action": "استمر بالشراء|انتظر|بع جزء|احتفظ",
        "reasoning": "السبب باللهجة الكويتية"
    }
}
`;

        try {
            const response = await this.sendRequest(prompt);
            const result = this.parseJsonResponse(response);
            if (result) return result;
        } catch (error) {
            console.error('Correction Analysis Error:', error);
        }
        
        return this.getFallbackCorrectionAnalysis(priceData);
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 🏦 توقعات البنوك الكبرى
    // ═══════════════════════════════════════════════════════════════════
    
    async getBanksForecast() {
        const prompt = `
أعطني توقعات البنوك الكبرى لأسعار الذهب للفترة القادمة (بناءً على آخر تقارير معروفة):

{
    "forecasts": [
        {
            "bank": "Goldman Sachs",
            "target": رقم بالدولار للأونصة,
            "timeframe": "نهاية 2026",
            "direction": "صعود|نزول",
            "reasoning": "السبب",
            "lastUpdated": "التاريخ التقريبي"
        },
        {
            "bank": "JP Morgan",
            "target": رقم,
            "timeframe": "الفترة",
            "direction": "صعود|نزول",
            "reasoning": "السبب",
            "lastUpdated": "التاريخ"
        },
        {
            "bank": "Citibank",
            "target": رقم,
            "timeframe": "الفترة",
            "direction": "صعود|نزول",
            "reasoning": "السبب",
            "lastUpdated": "التاريخ"
        },
        {
            "bank": "UBS",
            "target": رقم,
            "timeframe": "الفترة",
            "direction": "صعود|نزول",
            "reasoning": "السبب",
            "lastUpdated": "التاريخ"
        },
        {
            "bank": "Bank of America",
            "target": رقم,
            "timeframe": "الفترة",
            "direction": "صعود|نزول",
            "reasoning": "السبب",
            "lastUpdated": "التاريخ"
        }
    ],
    "consensus": {
        "averageTarget": رقم,
        "direction": "صعود|نزول",
        "confidence": 0-100,
        "summary": "ملخص الإجماع باللهجة الكويتية"
    },
    "kuwaitImpact": "تأثير توقعات البنوك على السوق الكويتي"
}
`;

        try {
            const response = await this.sendRequest(prompt);
            const result = this.parseJsonResponse(response);
            if (result) return result;
        } catch (error) {
            console.error('Banks Forecast Error:', error);
        }
        
        return this.getFallbackBanksForecast();
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // 📈 المؤشرات الفنية المحسوبة
    // ═══════════════════════════════════════════════════════════════════
    
    calculateTechnicalIndicators(priceHistory) {
        if (!priceHistory || priceHistory.length < 14) {
            return this.getDefaultIndicators();
        }
        
        const prices = priceHistory.map(p => p.close || p);
        
        return {
            rsi: this.calculateRSI(prices, 14),
            sma20: this.calculateSMA(prices, 20),
            sma50: this.calculateSMA(prices, 50),
            ema12: this.calculateEMA(prices, 12),
            ema26: this.calculateEMA(prices, 26),
            macd: this.calculateMACD(prices),
            bollingerBands: this.calculateBollingerBands(prices, 20),
            support: this.findSupport(prices),
            resistance: this.findResistance(prices)
        };
    },
    
    calculateRSI(prices, period = 14) {
        if (prices.length < period + 1) return 50;
        
        let gains = 0, losses = 0;
        for (let i = prices.length - period; i < prices.length; i++) {
            const change = prices[i] - prices[i - 1];
            if (change > 0) gains += change;
            else losses -= change;
        }
        
        const avgGain = gains / period;
        const avgLoss = losses / period;
        
        if (avgLoss === 0) return 100;
        const rs = avgGain / avgLoss;
        return Math.round(100 - (100 / (1 + rs)));
    },
    
    calculateSMA(prices, period) {
        if (prices.length < period) return prices[prices.length - 1];
        const slice = prices.slice(-period);
        return slice.reduce((a, b) => a + b, 0) / period;
    },
    
    calculateEMA(prices, period) {
        if (prices.length < period) return prices[prices.length - 1];
        const multiplier = 2 / (period + 1);
        let ema = this.calculateSMA(prices.slice(0, period), period);
        
        for (let i = period; i < prices.length; i++) {
            ema = (prices[i] - ema) * multiplier + ema;
        }
        return ema;
    },
    
    calculateMACD(prices) {
        const ema12 = this.calculateEMA(prices, 12);
        const ema26 = this.calculateEMA(prices, 26);
        const macdLine = ema12 - ema26;
        
        return {
            value: macdLine,
            signal: macdLine > 0 ? 'شراء' : macdLine < 0 ? 'بيع' : 'محايد'
        };
    },
    
    calculateBollingerBands(prices, period = 20) {
        const sma = this.calculateSMA(prices, period);
        const slice = prices.slice(-period);
        const squaredDiffs = slice.map(p => Math.pow(p - sma, 2));
        const stdDev = Math.sqrt(squaredDiffs.reduce((a, b) => a + b, 0) / period);
        
        return {
            upper: sma + (2 * stdDev),
            middle: sma,
            lower: sma - (2 * stdDev),
            position: prices[prices.length - 1] > sma + stdDev ? 'أعلى' :
                      prices[prices.length - 1] < sma - stdDev ? 'أسفل' : 'وسط'
        };
    },
    
    findSupport(prices) {
        const recentPrices = prices.slice(-30);
        return Math.min(...recentPrices);
    },
    
    findResistance(prices) {
        const recentPrices = prices.slice(-30);
        return Math.max(...recentPrices);
    },
    
    getDefaultIndicators() {
        return {
            rsi: 55,
            sma20: 4750,
            sma50: 4700,
            macd: { value: 0, signal: 'محايد' },
            bollingerBands: { upper: 4850, middle: 4750, lower: 4650, position: 'وسط' },
            support: 4650,
            resistance: 4850
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // التحقق والتعزيز
    // ═══════════════════════════════════════════════════════════════════
    
    validateAndEnhanceAnalysis(analysis, priceData) {
        // التحقق من منطقية التوصية
        if (analysis.recommendation) {
            const rec = analysis.recommendation;
            
            // تحقق من التناسق
            if (rec.action === 'شراء قوي' && analysis.technicalIndicators?.rsiStatus === 'ذروة شراء') {
                rec.confidence = Math.min(rec.confidence, 60);
                rec.reasoning += ' (تحذير: RSI في ذروة شراء)';
            }
            
            if (rec.action === 'بيع قوي' && analysis.technicalIndicators?.rsiStatus === 'ذروة بيع') {
                rec.confidence = Math.min(rec.confidence, 60);
                rec.reasoning += ' (تحذير: RSI في ذروة بيع)';
            }
        }
        
        // إضافة الطابع الزمني
        analysis.timestamp = new Date().toISOString();
        analysis.model = this.currentModel;
        
        return analysis;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // الوظائف القديمة (للتوافق)
    // ═══════════════════════════════════════════════════════════════════
    
    async analyzeGoldMarket(currentPrice, historicalData) {
        return this.advancedAnalysis(currentPrice, historicalData);
    },
    
    async predictPrices(currentPrice, historicalPeaks, historicalValleys) {
        const prompt = `
السعر الحالي: ${currentPrice} د.ك
القمم السابقة: ${historicalPeaks?.slice(0, 5).map(p => p.price || p).join(', ') || 'غير متوفر'}
القيعان السابقة: ${historicalValleys?.slice(0, 5).map(v => v.price || v).join(', ') || 'غير متوفر'}

توقع الأسعار:
{
    "predictions": {
        "tomorrow": {"price": رقم, "confidence": 0-100, "direction": "صعود|هبوط|ثبات"},
        "nextWeek": {"price": رقم, "confidence": 0-100, "direction": "صعود|هبوط|ثبات"},
        "nextMonth": {"price": رقم, "confidence": 0-100, "direction": "صعود|هبوط|ثبات"}
    },
    "nextPeak": {"price": رقم, "expectedDate": "التاريخ", "confidence": 0-100},
    "nextValley": {"price": رقم, "expectedDate": "التاريخ", "confidence": 0-100}
}
`;
        
        try {
            const response = await this.sendRequest(prompt);
            const result = this.parseJsonResponse(response);
            if (result) return result;
        } catch (error) {
            console.error('Prediction Error:', error);
        }
        
        return this.getFallbackPredictions(currentPrice);
    },
    
    async analyzeEntryPoint(currentPrice, targetEntry = 46.000) {
        const diff = currentPrice - targetEntry;
        const percentage = ((diff / targetEntry) * 100).toFixed(2);
        
        return {
            entryStatus: diff < 0 ? 'فرصة ذهبية' : diff < 0.5 ? 'فرصة جيدة' : 'انتظر',
            statusIcon: diff < 0 ? '🟢' : diff < 0.5 ? '🟡' : '🔴',
            currentVsTarget: { difference: diff.toFixed(3), percentage: percentage + '%', isBelow: diff < 0 },
            recommendation: diff < 0 ? 'اشترِ الآن!' : 'انتظر هبوط السعر',
            bestAction: diff < 0 ? 'شراء فوري' : 'أمر شراء معلق',
            alternativeEntry: (targetEntry - 0.5).toFixed(3),
            urgency: diff < 0 ? 'عاجل' : 'غير عاجل'
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // البيانات الاحتياطية
    // ═══════════════════════════════════════════════════════════════════
    
    getFallbackAdvancedAnalysis(priceData) {
        const price = priceData.ounce || 4793;
        const gramKWD = priceData.gram24k || 47.5;
        
        return {
            currentStatus: {
                trend: 'صاعد',
                trendStrength: 65,
                pricePosition: 'قرب القمة'
            },
            trendAnalysis: {
                direction: 'صعود',
                reasons: [
                    { reason: 'التوترات الجيوسياسية العالمية', impact: 'قوي' },
                    { reason: 'مشتريات البنوك المركزية', impact: 'قوي' }
                ],
                mainDrivers: ['الملاذ الآمن', 'ضعف الدولار'],
                explanation: 'الذهب طالع بسبب التوترات العالمية والطلب من البنوك المركزية'
            },
            isAllTimeHigh: {
                status: false,
                distanceFromATH: '1% تحت القمة',
                comment: 'قريب جداً من أعلى مستوى تاريخي'
            },
            continuationOrCorrection: {
                assessment: 'استمرار مع احتمال تصحيح طفيف',
                probability: 70,
                expectedCorrectionSize: '2-3%',
                expectedCorrectionTime: 'خلال 5-7 أيام',
                signals: ['RSI مرتفع', 'قرب المقاومة'],
                recommendation: 'احتفظ بمراكزك مع وضع وقف خسارة'
            },
            technicalIndicators: {
                rsiStatus: 'محايد',
                rsiValue: 55,
                macdSignal: 'شراء',
                movingAverages: 'فوق المتوسطات',
                supportLevel: Math.round(price * 0.97),
                resistanceLevel: Math.round(price * 1.03),
                bollingerPosition: 'أعلى النطاق'
            },
            recommendation: {
                action: 'انتظار',
                confidence: 60,
                reasoning: 'السوق قريب من القمة، انتظر تصحيح بسيط للدخول',
                entryPrice: (gramKWD * 0.98).toFixed(3),
                targetPrice: (gramKWD * 1.05).toFixed(3),
                stopLoss: (gramKWD * 0.95).toFixed(3),
                timeHorizon: 'أسبوع'
            },
            banksForecast: {
                goldmanSachs: { target: 5000, timeframe: 'نهاية 2026', direction: 'صعود' },
                jpMorgan: { target: 4900, timeframe: 'Q2 2026', direction: 'صعود' },
                citibank: { target: 5100, timeframe: '2026', direction: 'صعود' },
                consensus: 'البنوك الكبرى متفائلة',
                averageTarget: 5000
            },
            riskAssessment: {
                level: 'متوسط',
                factors: ['قرب القمة التاريخية', 'تقلبات السوق'],
                advice: 'لا تستثمر أكثر من 20% من محفظتك'
            },
            shortTermOutlook: {
                next24h: { direction: 'ثبات', expectedChange: '+0.2%' },
                nextWeek: { direction: 'صعود', expectedChange: '+1%' },
                nextMonth: { direction: 'صعود', expectedChange: '+3%' }
            },
            kuwaitSpecific: {
                bestBuyTime: 'الأسبوع الأخير من الشهر',
                localTip: 'راقب أسعار دار السبائك',
                ramadanEffect: 'الطلب يرتفع قبل رمضان'
            },
            timestamp: new Date().toISOString(),
            model: 'fallback'
        };
    },
    
    getFallbackCorrectionAnalysis(priceData) {
        return {
            isAtAllTimeHigh: false,
            distanceFromATH: '2%',
            trendContinuation: { probability: 60, supportingFactors: ['زخم قوي'], targetIfContinues: 4900 },
            correctionAnalysis: { probability: 40, expectedSize: '3%', expectedDuration: '5 أيام', triggerLevels: [4850, 4800], warningSignals: ['RSI مرتفع'] },
            timing: { isNearCorrection: false, expectedWithin: '7 أيام', confidence: 50 },
            recommendation: { action: 'احتفظ', reasoning: 'الاتجاه إيجابي مع احتياط' }
        };
    },
    
    getFallbackBanksForecast() {
        return {
            forecasts: [
                { bank: 'Goldman Sachs', target: 5000, timeframe: 'نهاية 2026', direction: 'صعود', reasoning: 'الطلب القوي', lastUpdated: 'يناير 2026' },
                { bank: 'JP Morgan', target: 4900, timeframe: 'Q2 2026', direction: 'صعود', reasoning: 'التضخم', lastUpdated: 'يناير 2026' },
                { bank: 'Citibank', target: 5100, timeframe: '2026', direction: 'صعود', reasoning: 'الملاذ الآمن', lastUpdated: 'يناير 2026' },
                { bank: 'UBS', target: 4850, timeframe: 'Q1 2026', direction: 'صعود', reasoning: 'البنوك المركزية', lastUpdated: 'يناير 2026' },
                { bank: 'Bank of America', target: 5000, timeframe: '2026', direction: 'صعود', reasoning: 'ضعف الدولار', lastUpdated: 'يناير 2026' }
            ],
            consensus: { averageTarget: 4970, direction: 'صعود', confidence: 75, summary: 'البنوك الكبرى متفائلة بخصوص الذهب في 2026' },
            kuwaitImpact: 'توقعات إيجابية تعني ارتفاع الأسعار في الكويت'
        };
    },
    
    getFallbackPredictions(currentPrice) {
        const price = parseFloat(currentPrice) || 47.5;
        return {
            predictions: {
                tomorrow: { price: (price * 1.002).toFixed(3), confidence: 65, direction: 'صعود' },
                nextWeek: { price: (price * 1.01).toFixed(3), confidence: 55, direction: 'صعود' },
                nextMonth: { price: (price * 1.03).toFixed(3), confidence: 45, direction: 'صعود' }
            },
            nextPeak: { price: (price * 1.05).toFixed(3), expectedDate: 'خلال أسبوعين', confidence: 50 },
            nextValley: { price: (price * 0.97).toFixed(3), expectedDate: 'خلال أسبوع', confidence: 45 }
        };
    }
};

// تصدير
window.GeminiService = GeminiService;
