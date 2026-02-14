/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - نظام المحفظة الذهبية
 * ═══════════════════════════════════════════════════════════════════
 * إدارة رصيد الذهب وحساب القيمة الحالية
 * ═══════════════════════════════════════════════════════════════════
 */

const PortfolioService = {
    // ═══════════════════════════════════════════════════════════════════
    // الإعدادات
    // ═══════════════════════════════════════════════════════════════════
    
    STORAGE_KEY: 'sabeeka_portfolio',
    
    // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
    
    // ═══════════════════════════════════════════════════════════════════
    // هيكل المحفظة
    // ═══════════════════════════════════════════════════════════════════
    
    portfolio: {
        holdings: [],           // المشتريات
        totalGrams: 0,          // إجمالي الجرامات
        totalInvested: 0,       // إجمالي المبلغ المستثمر
        averageCost: 0,         // متوسط سعر الشراء
        createdAt: null,
        updatedAt: null
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // التهيئة
    // ═══════════════════════════════════════════════════════════════════
    
    init() {
        this.loadPortfolio();
        console.log('💰 نظام المحفظة جاهز');
        return this;
    },
    
    loadPortfolio() {
        try {
            const saved = localStorage.getItem(this.STORAGE_KEY);
            if (saved) {
                this.portfolio = JSON.parse(saved);
            } else {
                this.portfolio.createdAt = new Date().toISOString();
            }
        } catch (error) {
            console.error('خطأ في تحميل المحفظة:', error);
        }
    },
    
    savePortfolio() {
        try {
            this.portfolio.updatedAt = new Date().toISOString();
            this.recalculateTotals();
            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.portfolio));
        } catch (error) {
            console.error('خطأ في حفظ المحفظة:', error);
        }
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إضافة شراء جديد
    // ═══════════════════════════════════════════════════════════════════
    
    addPurchase(data) {
        const purchase = {
            id: 'purchase_' + Date.now(),
            date: data.date || new Date().toISOString(),
            grams: parseFloat(data.grams),
            karat: parseInt(data.karat) || 24,
            pricePerGram: parseFloat(data.pricePerGram),  // سعر الشراء للجرام
            totalCost: parseFloat(data.grams) * parseFloat(data.pricePerGram),
            source: data.source || 'دار السبائك',
            notes: data.notes || '',
            createdAt: new Date().toISOString()
        };
        
        // تحويل لعيار 24 إذا كان عيار مختلف
        if (purchase.karat !== 24) {
            const karatRatios = { 22: 0.916, 21: 0.875, 18: 0.750 };
            purchase.grams24kEquivalent = purchase.grams * (karatRatios[purchase.karat] || 1);
        } else {
            purchase.grams24kEquivalent = purchase.grams;
        }
        
        this.portfolio.holdings.push(purchase);
        this.savePortfolio();
        
        return purchase;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إضافة بيع
    // ═══════════════════════════════════════════════════════════════════
    
    addSale(data) {
        const sale = {
            id: 'sale_' + Date.now(),
            type: 'sale',
            date: data.date || new Date().toISOString(),
            grams: -parseFloat(data.grams),  // سالب للبيع
            karat: parseInt(data.karat) || 24,
            pricePerGram: parseFloat(data.pricePerGram),  // سعر البيع
            totalValue: parseFloat(data.grams) * parseFloat(data.pricePerGram),
            source: data.source || '',
            notes: data.notes || '',
            createdAt: new Date().toISOString()
        };
        
        if (sale.karat !== 24) {
            const karatRatios = { 22: 0.916, 21: 0.875, 18: 0.750 };
            sale.grams24kEquivalent = sale.grams * (karatRatios[sale.karat] || 1);
        } else {
            sale.grams24kEquivalent = sale.grams;
        }
        
        this.portfolio.holdings.push(sale);
        this.savePortfolio();
        
        return sale;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // حذف عملية
    // ═══════════════════════════════════════════════════════════════════
    
    removeTransaction(id) {
        this.portfolio.holdings = this.portfolio.holdings.filter(h => h.id !== id);
        this.savePortfolio();
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إعادة حساب الإجماليات
    // ═══════════════════════════════════════════════════════════════════
    
    recalculateTotals() {
        let totalGrams = 0;
        let totalInvested = 0;
        let totalPurchaseGrams = 0;
        
        for (const holding of this.portfolio.holdings) {
            const grams = holding.grams24kEquivalent || holding.grams;
            totalGrams += grams;
            
            if (grams > 0) {  // شراء
                totalInvested += holding.totalCost || (grams * holding.pricePerGram);
                totalPurchaseGrams += grams;
            }
        }
        
        this.portfolio.totalGrams = Math.max(0, totalGrams);
        this.portfolio.totalInvested = totalInvested;
        this.portfolio.averageCost = totalPurchaseGrams > 0 
            ? totalInvested / totalPurchaseGrams 
            : 0;
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // حساب القيمة الحالية
    // ═══════════════════════════════════════════════════════════════════
    
    calculateCurrentValue(currentPricePerGram) {
        this.recalculateTotals();
        
        const currentValue = this.portfolio.totalGrams * currentPricePerGram;
        const profitLoss = currentValue - this.portfolio.totalInvested;
        const profitLossPercent = this.portfolio.totalInvested > 0 
            ? (profitLoss / this.portfolio.totalInvested) * 100 
            : 0;
        
        return {
            totalGrams: this.portfolio.totalGrams,
            totalInvested: this.portfolio.totalInvested,
            averageCost: this.portfolio.averageCost,
            currentPricePerGram: currentPricePerGram,
            currentValue: currentValue,
            profitLoss: profitLoss,
            profitLossPercent: profitLossPercent,
            isProfit: profitLoss >= 0
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // الحصول على تفاصيل المحفظة
    // ═══════════════════════════════════════════════════════════════════
    
    getPortfolioDetails(currentPricePerGram) {
        const valuation = this.calculateCurrentValue(currentPricePerGram);
        
        return {
            ...valuation,
            holdings: this.portfolio.holdings,
            holdingsCount: this.portfolio.holdings.filter(h => h.grams > 0).length,
            salesCount: this.portfolio.holdings.filter(h => h.grams < 0).length,
            createdAt: this.portfolio.createdAt,
            updatedAt: this.portfolio.updatedAt
        };
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // الحصول على سجل العمليات
    // ═══════════════════════════════════════════════════════════════════
    
    getTransactionHistory() {
        return this.portfolio.holdings.sort((a, b) => 
            new Date(b.date) - new Date(a.date)
        );
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // تحليل الأداء
    // ═══════════════════════════════════════════════════════════════════
    
    analyzePerformance(currentPricePerGram, historicalPrices = []) {
        const valuation = this.calculateCurrentValue(currentPricePerGram);
        
        // حساب أفضل وأسوأ شراء
        let bestPurchase = null;
        let worstPurchase = null;
        
        for (const holding of this.portfolio.holdings) {
            if (holding.grams > 0) {
                const currentHoldingValue = holding.grams * currentPricePerGram;
                const profit = currentHoldingValue - holding.totalCost;
                const profitPercent = (profit / holding.totalCost) * 100;
                
                if (!bestPurchase || profitPercent > bestPurchase.profitPercent) {
                    bestPurchase = { ...holding, profit, profitPercent };
                }
                if (!worstPurchase || profitPercent < worstPurchase.profitPercent) {
                    worstPurchase = { ...holding, profit, profitPercent };
                }
            }
        }
        
        return {
            ...valuation,
            bestPurchase,
            worstPurchase,
            averageHoldingPeriod: this.calculateAverageHoldingPeriod(),
            recommendation: this.getHoldingRecommendation(valuation)
        };
    },
    
    calculateAverageHoldingPeriod() {
        const purchases = this.portfolio.holdings.filter(h => h.grams > 0);
        if (purchases.length === 0) return 0;
        
        const now = new Date();
        let totalDays = 0;
        
        for (const purchase of purchases) {
            const purchaseDate = new Date(purchase.date);
            const days = Math.floor((now - purchaseDate) / (1000 * 60 * 60 * 24));
            totalDays += days;
        }
        
        return Math.round(totalDays / purchases.length);
    },
    
    getHoldingRecommendation(valuation) {
        if (valuation.profitLossPercent >= 10) {
            return {
                action: 'فكر في البيع الجزئي',
                reason: 'ربحك تجاوز 10%، ممكن تأمن جزء من الأرباح',
                icon: '💰'
            };
        } else if (valuation.profitLossPercent >= 5) {
            return {
                action: 'احتفظ',
                reason: 'ربح جيد، استمر بالمراقبة',
                icon: '📈'
            };
        } else if (valuation.profitLossPercent >= 0) {
            return {
                action: 'احتفظ',
                reason: 'لا تتسرع، السوق إيجابي',
                icon: '⏳'
            };
        } else if (valuation.profitLossPercent >= -5) {
            return {
                action: 'صبر',
                reason: 'خسارة بسيطة، السوق يتعافى عادة',
                icon: '🔄'
            };
        } else {
            return {
                action: 'لا تبيع بخسارة',
                reason: 'انتظر تحسن السوق، الذهب ملاذ آمن',
                icon: '⚠️'
            };
        }
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // مسح المحفظة
    // ═══════════════════════════════════════════════════════════════════
    
    clearPortfolio() {
        this.portfolio = {
            holdings: [],
            totalGrams: 0,
            totalInvested: 0,
            averageCost: 0,
            createdAt: new Date().toISOString(),
            updatedAt: null
        };
        this.savePortfolio();
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // تصدير/استيراد
    // ═══════════════════════════════════════════════════════════════════
    
    exportPortfolio() {
        return JSON.stringify(this.portfolio, null, 2);
    },
    
    importPortfolio(jsonString) {
        try {
            const imported = JSON.parse(jsonString);
            if (imported.holdings) {
                this.portfolio = imported;
                this.savePortfolio();
                return { success: true };
            }
            return { success: false, error: 'صيغة غير صحيحة' };
        } catch (error) {
            return { success: false, error: 'خطأ في القراءة' };
        }
    },
    
    // ═══════════════════════════════════════════════════════════════════
    // إحصائيات سريعة
    // ═══════════════════════════════════════════════════════════════════
    
    getQuickStats(currentPricePerGram) {
        const valuation = this.calculateCurrentValue(currentPricePerGram);
        
        return {
            hasHoldings: this.portfolio.totalGrams > 0,
            totalGrams: valuation.totalGrams.toFixed(2),
            currentValue: valuation.currentValue.toFixed(3),
            profitLoss: valuation.profitLoss.toFixed(3),
            profitLossPercent: valuation.profitLossPercent.toFixed(2),
            isProfit: valuation.isProfit,
            averageCost: valuation.averageCost.toFixed(3)
        };
    }
};

// تصدير
window.PortfolioService = PortfolioService;

// تهيئة تلقائية
document.addEventListener('DOMContentLoaded', () => {
    PortfolioService.init();
});
