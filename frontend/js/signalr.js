/**
 * ═══════════════════════════════════════════════════════════════════
 * Mr. Golden Bader - SignalR Real-time Connection
 * ═══════════════════════════════════════════════════════════════════
 */

// Include SignalR from CDN (add to HTML before this script)
// <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script>

const SignalRService = {
    connection: null,
    isConnected: false,
    reconnectAttempts: 0,
    maxReconnectAttempts: 3,  // تقليل المحاولات
    isDisabled: false,        // إيقاف كامل

    /**
     * Initialize SignalR connection
     */
    async connect() {
        // إذا تم تعطيل الخدمة
        if (this.isDisabled) {
            return;
        }
        
        // Check if SignalR library is loaded
        if (typeof signalR === 'undefined') {
            this.updateStatus('live');
            this.isDisabled = true;
            return;
        }

        // تجنب إعادة الاتصال المتكرر
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            this.updateStatus('live');
            this.isDisabled = true;
            return;
        }

        // التحقق من وجود Backend أولاً
        try {
            const healthUrl = CONFIG.API_URL + '/health';
            const healthCheck = await fetch(healthUrl, { 
                method: 'GET',
                signal: AbortSignal.timeout(2000)  // 2 ثواني timeout
            });
            if (!healthCheck.ok) throw new Error('Backend not ready');
        } catch (e) {
            // Silent - don't log, just use direct mode
            this.updateStatus('live');
            this.isDisabled = true;
            return;
        }

        try {
            this.updateStatus('connecting');

            // بناء الاتصال
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(CONFIG.SIGNALR_URL)
                .withAutomaticReconnect([2000, 5000, 10000])
                .configureLogging(signalR.LogLevel.Error)
                .build();

            // التأكد من أن الاتصال تم إنشاؤه
            if (!this.connection) {
                throw new Error('Failed to create SignalR connection');
            }

            // Event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();

            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.updateStatus('connected');

            console.log('✅ SignalR Connected');

        } catch (error) {
            this.connection = null;
            this.reconnectAttempts++;
            
            if (this.reconnectAttempts >= this.maxReconnectAttempts) {
                this.updateStatus('live');
                this.isDisabled = true;
            } else {
                const delay = 10000 * this.reconnectAttempts;  // 10, 20, 30 ثانية
                setTimeout(() => this.connect(), delay);
            }
        }
    },

    /**
     * Setup event handlers for SignalR events
     */
    setupEventHandlers() {
        if (!this.connection) {
            console.warn('⚠️ Cannot setup handlers - connection is null');
            return;
        }

        // Price updates
        this.connection.on('ReceivePriceUpdate', (price) => {
            console.log('📊 Price Update:', price);
            this.onPriceUpdate(price);
        });

        // Kuwait prices
        this.connection.on('ReceiveKuwaitPrices', (prices) => {
            console.log('🇰🇼 Kuwait Prices:', prices);
            this.onKuwaitPricesUpdate(prices);
        });

        // Alerts
        this.connection.on('ReceiveAlert', (alert) => {
            console.log('🔔 Alert:', alert);
            this.onAlertReceived(alert);
        });

        // Connection state changes
        this.connection.onreconnecting((error) => {
            console.log('🔄 Reconnecting...', error);
            this.updateStatus('connecting');
        });

        this.connection.onreconnected((connectionId) => {
            console.log('✅ Reconnected:', connectionId);
            this.updateStatus('connected');
        });

        this.connection.onclose((error) => {
            console.log('❌ Connection closed', error);
            this.isConnected = false;
            this.updateStatus('disconnected');
            this.scheduleReconnect();
        });
    },

    /**
     * Handle price update from SignalR
     * 🔴 يتم تجاهل أسعار Backend - نستخدم TradingView فقط
     */
    onPriceUpdate(price) {
        // 🔴 تجاهل أسعار Backend - السعر يأتي من TradingView فقط
        console.log('📊 SignalR Price (ignored - TradingView only):', price?.Close);
    },

    /**
     * Handle Kuwait prices update
     * 🔴 يتم تجاهلها - نحسب الأسعار محلياً من TradingView
     */
    onKuwaitPricesUpdate(prices) {
        // 🔴 تجاهل - يتم حساب الأسعار محلياً من TradingView
        console.log('🇰🇼 SignalR Kuwait Prices (ignored - calculated from TradingView)');
    },

    /**
     * Handle alert received
     */
    onAlertReceived(alert) {
        // Show toast notification
        App.showToast(alert);

        // Update alerts badge
        App.updateAlertsBadge();

        // Play sound
        this.playAlertSound(alert.Type || alert.type);

        // Dispatch custom event
        window.dispatchEvent(new CustomEvent('alertReceived', { detail: alert }));
    },

    /**
     * Play alert sound
     */
    playAlertSound(type) {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            // Different frequencies for different alert types
            const frequencies = {
                'Price': [440, 550],
                'News': [330, 440, 550],
                'Recommendation': [440, 550, 660],
                'Target': [660, 880]
            };

            const freqs = frequencies[type] || [440];

            oscillator.type = 'sine';
            oscillator.frequency.setValueAtTime(freqs[0], audioContext.currentTime);

            gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.5);

        } catch (e) {
            console.log('Sound not available');
        }
    },

    /**
     * Update connection status UI
     */
    updateStatus(status) {
        const dot = document.getElementById('status-dot');
        const text = document.getElementById('status-text');

        if (!dot || !text) return;

        dot.className = 'w-2 h-2 rounded-full';

        switch (status) {
            case 'connected':
                dot.classList.add('bg-green-500', 'animate-pulse');
                text.textContent = 'متصل';
                text.className = 'text-xs text-green-400';
                break;
            case 'connecting':
                dot.classList.add('bg-yellow-500', 'animate-pulse');
                text.textContent = 'جاري الاتصال...';
                text.className = 'text-xs text-yellow-400';
                break;
            case 'disconnected':
                dot.classList.add('bg-red-500');
                text.textContent = 'غير متصل';
                text.className = 'text-xs text-zinc-400';
                break;
            case 'live':
                // حالة خاصة: متصل بـ TradingView مباشرة
                dot.classList.add('bg-green-500', 'animate-pulse');
                text.textContent = 'بث مباشر';
                text.className = 'text-xs text-green-400';
                break;
        }
    },

    /**
     * Schedule reconnection
     */
    scheduleReconnect() {
        if (this.isDisabled || this.reconnectAttempts >= this.maxReconnectAttempts) {
            this.updateStatus('live');
            this.isDisabled = true;
            return;
        }

        this.reconnectAttempts++;
        const delay = 15000;  // 15 ثانية ثابتة

        setTimeout(() => this.connect(), delay);
    },

    /**
     * Disconnect
     */
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            this.updateStatus('disconnected');
        }
    }
};

// Freeze SignalR service
Object.freeze(SignalRService);
