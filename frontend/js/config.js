/**
 * ═══════════════════════════════════════════════════════════════════
 * Mr. Golden Bader - Configuration
 * ═══════════════════════════════════════════════════════════════════
 */

// Auto-detect production environment
const IS_PRODUCTION = window.location.hostname === 'sabeekagold.com' || 
                      window.location.hostname === 'www.sabeekagold.com';

const CONFIG = {
    // API Base URL - auto-detect environment
    API_URL: IS_PRODUCTION ? '/api' : 'http://localhost:5000/api',

    // SignalR Hub URL
    SIGNALR_URL: IS_PRODUCTION ? '/hubs/goldprice' : 'http://localhost:5000/hubs/goldprice',

    // AI Service URL (not used in production - Gemini is called directly)
    AI_SERVICE_URL: IS_PRODUCTION ? '' : 'http://localhost:8000',
    
    // Is Production flag
    IS_PRODUCTION: IS_PRODUCTION,

    // Refresh intervals (in milliseconds)
    // التحديث كل 4 ثوانٍ حسب السعر العالمي (مثل دار السبائك)
    REFRESH_INTERVALS: {
        PRICES: 4000,       // 4 ثوانٍ - تحديث لحظي
        NEWS: 300000,       // 5 دقائق
        ALERTS: 10000,      // 10 ثوانٍ
        DASHBOARD: 4000     // 4 ثوانٍ - تحديث لحظي
    },

    // Toast duration (in milliseconds)
    TOAST_DURATION: 10000,

    // Karat types
    KARAT_TYPES: {
        24: { name: 'ذهب 24 قيراط', color: 'karat-24' },
        22: { name: 'ذهب 22 قيراط', color: 'karat-22' },
        21: { name: 'ذهب 21 قيراط', color: 'karat-21' },
        18: { name: 'ذهب 18 قيراط', color: 'karat-18' }
    },

    // Alert types
    ALERT_TYPES: {
        Price: { icon: '💰', class: 'alert-price', sound: 'price' },
        News: { icon: '📰', class: 'alert-news', sound: 'news' },
        Recommendation: { icon: '💡', class: 'alert-recommendation', sound: 'recommendation' },
        Target: { icon: '🎯', class: 'alert-target', sound: 'target' }
    },

    // Recommendation types
    RECOMMENDATION_TYPES: {
        Buy: {
            icon: '📈',
            text: 'شراء',
            class: 'rec-buy',
            color: 'text-green-400',
            bgColor: 'bg-green-500/10'
        },
        Sell: {
            icon: '📉',
            text: 'بيع',
            class: 'rec-sell',
            color: 'text-red-400',
            bgColor: 'bg-red-500/10'
        },
        Hold: {
            icon: '⏳',
            text: 'انتظار',
            class: 'rec-hold',
            color: 'text-gold-400',
            bgColor: 'bg-gold-500/10'
        }
    }
};

// Freeze config to prevent modifications
Object.freeze(CONFIG);
Object.freeze(CONFIG.REFRESH_INTERVALS);
Object.freeze(CONFIG.KARAT_TYPES);
Object.freeze(CONFIG.ALERT_TYPES);
Object.freeze(CONFIG.RECOMMENDATION_TYPES);
