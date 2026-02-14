/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - نظام التنبيهات الذكي
 * تنبيهات مخصصة باللهجة الكويتية مع تحليل Gemini
 * ═══════════════════════════════════════════════════════════════════
 */

const NotificationSystem = {
    // ═══════════════════════════════════════════════════════════════════
    // الإعدادات والثوابت
    // ═══════════════════════════════════════════════════════════════════
    STORAGE_KEYS: {
        NOTIFICATIONS: 'sabeeka_notifications',
        SETTINGS: 'sabeeka_notification_settings',
        UNREAD_COUNT: 'sabeeka_unread_count',
        PRICE_ALERTS: 'sabeeka_price_alerts'
    },

    // أنواع التنبيهات
    TYPES: {
        PRICE_UP: 'price_up',           // السعر يرتفع
        PRICE_DOWN: 'price_down',       // السعر ينخفض
        BUY_SIGNAL: 'buy_signal',       // إشارة شراء
        SELL_SIGNAL: 'sell_signal',     // إشارة بيع
        PRICE_TARGET: 'price_target',   // وصول لسعر محدد
        NEWS_POSITIVE: 'news_positive', // خبر إيجابي
        NEWS_NEGATIVE: 'news_negative', // خبر سلبي
        NEWS_URGENT: 'news_urgent',     // خبر عاجل
        MARKET_OPEN: 'market_open',     // فتح السوق
        DAILY_SUMMARY: 'daily_summary', // ملخص يومي
        GEMINI_INSIGHT: 'gemini_insight' // تحليل Gemini
    },

    // ألوان كل نوع
    COLORS: {
        price_up: { bg: 'rgba(16, 185, 129, 0.15)', border: '#10b981', text: '#34d399', icon: '📈' },
        price_down: { bg: 'rgba(239, 68, 68, 0.15)', border: '#ef4444', text: '#f87171', icon: '📉' },
        buy_signal: { bg: 'rgba(34, 197, 94, 0.2)', border: '#22c55e', text: '#4ade80', icon: '💰' },
        sell_signal: { bg: 'rgba(249, 115, 22, 0.2)', border: '#f97316', text: '#fb923c', icon: '💸' },
        price_target: { bg: 'rgba(168, 85, 247, 0.15)', border: '#a855f7', text: '#c084fc', icon: '🎯' },
        news_positive: { bg: 'rgba(59, 130, 246, 0.15)', border: '#3b82f6', text: '#60a5fa', icon: '📰' },
        news_negative: { bg: 'rgba(239, 68, 68, 0.15)', border: '#ef4444', text: '#f87171', icon: '⚠️' },
        news_urgent: { bg: 'rgba(220, 38, 38, 0.2)', border: '#dc2626', text: '#f87171', icon: '🚨' },
        market_open: { bg: 'rgba(234, 179, 8, 0.15)', border: '#eab308', text: '#fde047', icon: '🔔' },
        daily_summary: { bg: 'rgba(99, 102, 241, 0.15)', border: '#6366f1', text: '#a5b4fc', icon: '📊' },
        gemini_insight: { bg: 'rgba(139, 92, 246, 0.2)', border: '#8b5cf6', text: '#c4b5fd', icon: '🤖' }
    },

    // أصوات التنبيهات
    SOUNDS: {
        price_up: 'sounds/price-up.mp3',
        price_down: 'sounds/price-down.mp3',
        buy_signal: 'sounds/buy-signal.mp3',
        sell_signal: 'sounds/sell-signal.mp3',
        price_target: 'sounds/target-reached.mp3',
        news_positive: 'sounds/news-positive.mp3',
        news_negative: 'sounds/news-alert.mp3',
        news_urgent: 'sounds/urgent-alert.mp3',
        default: 'sounds/notification.mp3'
    },

    // الإعدادات الافتراضية
    DEFAULT_SETTINGS: {
        enabled: true,
        sound: true,
        soundVolume: 0.7,
        priceAlerts: true,
        newsAlerts: true,
        geminiInsights: true,
        dailySummary: true,
        vibration: true,
        desktop: false,
        priceChangeThreshold: 0.5, // نسبة التغير لإرسال تنبيه (%)
        customPriceAlerts: [] // تنبيهات أسعار مخصصة
    },

    // ═══════════════════════════════════════════════════════════════════
    // التهيئة
    // ═══════════════════════════════════════════════════════════════════
    init() {
        this.loadSettings();
        this.loadNotifications();
        this.updateBadge();
        this.requestDesktopPermission();
        this.startPriceMonitoring();
        console.log('🔔 نظام التنبيهات جاهز');
    },

    // ═══════════════════════════════════════════════════════════════════
    // إدارة الإعدادات
    // ═══════════════════════════════════════════════════════════════════
    loadSettings() {
        try {
            const saved = localStorage.getItem(this.STORAGE_KEYS.SETTINGS);
            this.settings = saved ? { ...this.DEFAULT_SETTINGS, ...JSON.parse(saved) } : { ...this.DEFAULT_SETTINGS };
        } catch {
            this.settings = { ...this.DEFAULT_SETTINGS };
        }
    },

    saveSettings(newSettings) {
        this.settings = { ...this.settings, ...newSettings };
        localStorage.setItem(this.STORAGE_KEYS.SETTINGS, JSON.stringify(this.settings));
    },

    getSettings() {
        return this.settings;
    },

    // ═══════════════════════════════════════════════════════════════════
    // إدارة التنبيهات
    // ═══════════════════════════════════════════════════════════════════
    notifications: [],
    unreadCount: 0,

    loadNotifications() {
        try {
            this.notifications = JSON.parse(localStorage.getItem(this.STORAGE_KEYS.NOTIFICATIONS)) || [];
            this.unreadCount = parseInt(localStorage.getItem(this.STORAGE_KEYS.UNREAD_COUNT)) || 0;
        } catch {
            this.notifications = [];
            this.unreadCount = 0;
        }
    },

    saveNotifications() {
        // الاحتفاظ بآخر 100 تنبيه فقط
        if (this.notifications.length > 100) {
            this.notifications = this.notifications.slice(-100);
        }
        localStorage.setItem(this.STORAGE_KEYS.NOTIFICATIONS, JSON.stringify(this.notifications));
        localStorage.setItem(this.STORAGE_KEYS.UNREAD_COUNT, this.unreadCount.toString());
    },

    // ═══════════════════════════════════════════════════════════════════
    // إنشاء تنبيه جديد
    // ═══════════════════════════════════════════════════════════════════
    create(type, data) {
        if (!this.settings.enabled) return null;

        const user = this.getCurrentUser();
        const userName = user ? user.name.split(' ')[0] : 'عزيزي المستثمر';

        const notification = {
            id: 'notif_' + Date.now(),
            type: type,
            title: this.generateTitle(type, userName, data),
            message: this.generateMessage(type, userName, data),
            data: data,
            timestamp: new Date().toISOString(),
            read: false,
            colors: this.COLORS[type] || this.COLORS.gemini_insight
        };

        this.notifications.unshift(notification);
        this.unreadCount++;
        this.saveNotifications();
        this.updateBadge();

        // تشغيل الصوت
        if (this.settings.sound) {
            this.playSound(type);
        }

        // إشعار سطح المكتب
        if (this.settings.desktop && Notification.permission === 'granted') {
            this.showDesktopNotification(notification);
        }

        // إظهار Toast
        this.showToast(notification);

        // إطلاق حدث للتحديث اللحظي
        window.dispatchEvent(new CustomEvent('newNotification', { detail: notification }));

        return notification;
    },

    // ═══════════════════════════════════════════════════════════════════
    // توليد العناوين والرسائل باللهجة الكويتية
    // ═══════════════════════════════════════════════════════════════════
    generateTitle(type, userName, data) {
        const titles = {
            price_up: `${userName}، الذهب طالع! 📈`,
            price_down: `${userName}، الذهب نازل! 📉`,
            buy_signal: `فرصة شراء يا ${userName}! 💰`,
            sell_signal: `${userName}، وقت البيع! 💸`,
            price_target: `🎯 وصل السعر اللي تبيه يا ${userName}!`,
            news_positive: `خبر حلو لك يا ${userName} 📰`,
            news_negative: `⚠️ ${userName}، خبر مهم لازم تعرفه`,
            news_urgent: `🚨 عاجل يا ${userName}!`,
            market_open: `صباح الخير ${userName}، السوق فتح 🔔`,
            daily_summary: `ملخص يومك يا ${userName} 📊`,
            gemini_insight: `تحليل خاص لك يا ${userName} 🤖`
        };
        return titles[type] || `تنبيه لك يا ${userName}`;
    },

    generateMessage(type, userName, data) {
        const price = data?.price || '--';
        const change = data?.change || 0;
        const changeDir = change >= 0 ? 'ارتفع' : 'انخفض';
        const newsTitle = data?.newsTitle || '';
        const recommendation = data?.recommendation || '';

        const messages = {
            price_up: `الذهب ${changeDir} ${Math.abs(change).toFixed(2)}% ووصل ${price} د.ك. ${recommendation}`,
            price_down: `السعر نزل ${Math.abs(change).toFixed(2)}% وصار ${price} د.ك. ${recommendation}`,
            buy_signal: `${userName}، الوقت مناسب <span class="text-emerald-400 font-bold">للشراء</span>! السعر ${price} د.ك ${recommendation}`,
            sell_signal: `${userName}، ننصحك <span class="text-orange-400 font-bold">بالبيع</span> الحين! السعر ${price} د.ك ${recommendation}`,
            price_target: `وصل سعر الذهب للهدف اللي حددته: ${data?.targetPrice || price} د.ك`,
            news_positive: `${newsTitle}. هالخبر ممكن يأثر إيجابي على أسعار الذهب.`,
            news_negative: `${newsTitle}. انتبه، هالخبر ممكن يأثر على السوق.`,
            news_urgent: `خبر عاجل: ${newsTitle}`,
            market_open: `السوق فتح والسعر الحالي ${price} د.ك. يوم موفق إن شاء الله!`,
            daily_summary: `اليوم الذهب ${changeDir} ${Math.abs(change).toFixed(2)}%. السعر الحالي ${price} د.ك.`,
            gemini_insight: data?.insight || 'تحليل جديد متوفر من Gemini AI'
        };
        return messages[type] || data?.message || 'تنبيه جديد';
    },

    // ═══════════════════════════════════════════════════════════════════
    // الأصوات
    // ═══════════════════════════════════════════════════════════════════
    playSound(type) {
        try {
            // استخدام Web Audio API لتوليد أصوات بسيطة
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            // ترددات مختلفة لكل نوع
            const frequencies = {
                buy_signal: [523, 659, 784],      // C5, E5, G5 - صوت إيجابي
                sell_signal: [784, 659, 523],     // G5, E5, C5 - صوت تحذيري
                price_up: [440, 554, 659],        // A4, C#5, E5
                price_down: [659, 554, 440],      // E5, C#5, A4
                price_target: [523, 659, 784, 1047], // نغمة احتفالية
                news_urgent: [880, 440, 880, 440], // تنبيه متكرر
                default: [523, 659]               // نغمة عادية
            };

            const freqs = frequencies[type] || frequencies.default;
            let time = audioContext.currentTime;

            freqs.forEach((freq, i) => {
                const osc = audioContext.createOscillator();
                const gain = audioContext.createGain();
                
                osc.connect(gain);
                gain.connect(audioContext.destination);
                
                osc.frequency.value = freq;
                osc.type = 'sine';
                
                gain.gain.setValueAtTime(this.settings.soundVolume * 0.3, time + i * 0.15);
                gain.gain.exponentialRampToValueAtTime(0.01, time + i * 0.15 + 0.15);
                
                osc.start(time + i * 0.15);
                osc.stop(time + i * 0.15 + 0.2);
            });
        } catch (e) {
            console.log('تعذر تشغيل الصوت:', e);
        }
    },

    // ═══════════════════════════════════════════════════════════════════
    // Toast Notification
    // ═══════════════════════════════════════════════════════════════════
    showToast(notification) {
        // إزالة أي toast موجود
        const existingToast = document.getElementById('notification-toast');
        if (existingToast) existingToast.remove();

        const colors = notification.colors;
        const toast = document.createElement('div');
        toast.id = 'notification-toast';
        toast.className = 'fixed top-4 left-4 z-[9999] max-w-sm animate-slide-in';
        toast.innerHTML = `
            <div class="rounded-2xl p-4 backdrop-blur-xl shadow-2xl border transition-all duration-300 hover:scale-[1.02] cursor-pointer"
                 style="background: ${colors.bg}; border-color: ${colors.border};">
                <div class="flex items-start gap-3">
                    <div class="text-2xl">${colors.icon}</div>
                    <div class="flex-1 min-w-0">
                        <h4 class="font-bold text-sm mb-1" style="color: ${colors.text}">${notification.title}</h4>
                        <p class="text-xs text-gray-300 line-clamp-2">${notification.message}</p>
                        <p class="text-xs text-gray-500 mt-2">${this.formatTime(notification.timestamp)}</p>
                    </div>
                    <button onclick="this.closest('#notification-toast').remove()" class="text-gray-500 hover:text-white">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                        </svg>
                    </button>
                </div>
            </div>
        `;

        // إضافة أنيميشن
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateX(-100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            .animate-slide-in { animation: slideIn 0.3s ease-out; }
        `;
        document.head.appendChild(style);

        document.body.appendChild(toast);

        // النقر للانتقال لصفحة التنبيهات
        toast.querySelector('div').addEventListener('click', (e) => {
            if (e.target.tagName !== 'BUTTON') {
                window.location.href = window.location.pathname.includes('/pages/') 
                    ? 'alerts.html' 
                    : 'pages/alerts.html';
            }
        });

        // إخفاء تلقائي بعد 6 ثواني
        setTimeout(() => {
            if (toast.parentNode) {
                toast.style.animation = 'slideIn 0.3s ease-out reverse';
                setTimeout(() => toast.remove(), 300);
            }
        }, 6000);
    },

    // ═══════════════════════════════════════════════════════════════════
    // إشعارات سطح المكتب
    // ═══════════════════════════════════════════════════════════════════
    async requestDesktopPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            // لن نطلب الإذن تلقائياً، سنتركه للمستخدم
        }
    },

    showDesktopNotification(notification) {
        if (Notification.permission === 'granted') {
            const n = new Notification(notification.title, {
                body: notification.message.replace(/<[^>]*>/g, ''),
                icon: '/images/logo.png',
                badge: '/images/logo.png',
                tag: notification.id,
                requireInteraction: false
            });

            n.onclick = () => {
                window.focus();
                window.location.href = '/pages/alerts.html';
            };

            setTimeout(() => n.close(), 5000);
        }
    },

    // ═══════════════════════════════════════════════════════════════════
    // تحديث Badge العداد
    // ═══════════════════════════════════════════════════════════════════
    updateBadge() {
        const badges = document.querySelectorAll('[data-notification-badge]');
        badges.forEach(badge => {
            if (this.unreadCount > 0) {
                badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        });

        // تحديث عنوان الصفحة
        if (this.unreadCount > 0) {
            document.title = `(${this.unreadCount}) ${document.title.replace(/^\(\d+\)\s*/, '')}`;
        }
    },

    // ═══════════════════════════════════════════════════════════════════
    // وظائف مساعدة
    // ═══════════════════════════════════════════════════════════════════
    getCurrentUser() {
        try {
            return JSON.parse(localStorage.getItem('sabeeka_current_user'));
        } catch {
            return null;
        }
    },

    formatTime(timestamp) {
        const date = new Date(timestamp);
        const now = new Date();
        const diff = now - date;

        if (diff < 60000) return 'الحين';
        if (diff < 3600000) return `قبل ${Math.floor(diff / 60000)} دقيقة`;
        if (diff < 86400000) return `قبل ${Math.floor(diff / 3600000)} ساعة`;
        return date.toLocaleDateString('ar-KW');
    },

    markAsRead(notificationId) {
        const notif = this.notifications.find(n => n.id === notificationId);
        if (notif && !notif.read) {
            notif.read = true;
            this.unreadCount = Math.max(0, this.unreadCount - 1);
            this.saveNotifications();
            this.updateBadge();
        }
    },

    markAllAsRead() {
        this.notifications.forEach(n => n.read = true);
        this.unreadCount = 0;
        this.saveNotifications();
        this.updateBadge();
    },

    deleteNotification(notificationId) {
        const index = this.notifications.findIndex(n => n.id === notificationId);
        if (index > -1) {
            if (!this.notifications[index].read) {
                this.unreadCount = Math.max(0, this.unreadCount - 1);
            }
            this.notifications.splice(index, 1);
            this.saveNotifications();
            this.updateBadge();
        }
    },

    clearAll() {
        this.notifications = [];
        this.unreadCount = 0;
        this.saveNotifications();
        this.updateBadge();
    },

    getAll() {
        return this.notifications;
    },

    getUnread() {
        return this.notifications.filter(n => !n.read);
    },

    // ═══════════════════════════════════════════════════════════════════
    // مراقبة الأسعار
    // ═══════════════════════════════════════════════════════════════════
    lastPrice: null,
    priceMonitorInterval: null,

    startPriceMonitoring() {
        // مراقبة الأسعار كل 30 ثانية
        this.priceMonitorInterval = setInterval(() => {
            this.checkPriceAlerts();
        }, 30000);
    },

    stopPriceMonitoring() {
        if (this.priceMonitorInterval) {
            clearInterval(this.priceMonitorInterval);
        }
    },

    async checkPriceAlerts() {
        if (!this.settings.priceAlerts) return;

        // الحصول على السعر الحالي
        const currentPrice = this.getCurrentGoldPrice();
        if (!currentPrice) return;

        // التحقق من تنبيهات الأسعار المخصصة
        this.settings.customPriceAlerts.forEach(alert => {
            if (!alert.triggered) {
                if (alert.condition === 'above' && currentPrice >= alert.price) {
                    this.create(this.TYPES.PRICE_TARGET, { 
                        price: currentPrice.toFixed(3),
                        targetPrice: alert.price 
                    });
                    alert.triggered = true;
                } else if (alert.condition === 'below' && currentPrice <= alert.price) {
                    this.create(this.TYPES.PRICE_TARGET, { 
                        price: currentPrice.toFixed(3),
                        targetPrice: alert.price 
                    });
                    alert.triggered = true;
                }
            }
        });

        // التحقق من تغير السعر الكبير
        if (this.lastPrice) {
            const changePercent = ((currentPrice - this.lastPrice) / this.lastPrice) * 100;
            
            if (Math.abs(changePercent) >= this.settings.priceChangeThreshold) {
                const type = changePercent > 0 ? this.TYPES.PRICE_UP : this.TYPES.PRICE_DOWN;
                this.create(type, {
                    price: currentPrice.toFixed(3),
                    change: changePercent
                });
            }
        }

        this.lastPrice = currentPrice;
        this.saveSettings(this.settings);
    },

    getCurrentGoldPrice() {
        // [تم إزالة حساب السعر الكويتي - سيتم إضافة آلية جديدة لاحقاً]
        // محاولة الحصول على السعر من الصفحة
        const priceEl = document.querySelector('#hero-price-value, #current-gram, [data-gold-price]');
        if (priceEl) {
            const price = parseFloat(priceEl.textContent.replace(/[^\d.]/g, ''));
            if (!isNaN(price) && price > 0) return price;
        }
        
        // السعر غير متاح
        return null;
    },

    // ═══════════════════════════════════════════════════════════════════
    // تنبيهات أسعار مخصصة
    // ═══════════════════════════════════════════════════════════════════
    addPriceAlert(price, condition = 'above') {
        const alert = {
            id: 'price_alert_' + Date.now(),
            price: parseFloat(price),
            condition: condition, // 'above' أو 'below'
            triggered: false,
            createdAt: new Date().toISOString()
        };

        this.settings.customPriceAlerts.push(alert);
        this.saveSettings(this.settings);
        return alert;
    },

    removePriceAlert(alertId) {
        this.settings.customPriceAlerts = this.settings.customPriceAlerts.filter(a => a.id !== alertId);
        this.saveSettings(this.settings);
    },

    getPriceAlerts() {
        return this.settings.customPriceAlerts || [];
    }
};

// تصدير للاستخدام العام
window.NotificationSystem = NotificationSystem;

// تهيئة عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', () => {
    NotificationSystem.init();
});
