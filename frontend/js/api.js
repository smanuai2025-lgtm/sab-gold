/**
 * ═══════════════════════════════════════════════════════════════════
 * Mr. Golden Bader - API Client
 * ═══════════════════════════════════════════════════════════════════
 */

const API = {
    // Track if backend is available
    _backendAvailable: null,
    _lastCheck: 0,
    _checkInterval: 60000, // Check every 60 seconds
    
    /**
     * Check if backend is available
     */
    async isBackendAvailable() {
        const now = Date.now();
        if (this._backendAvailable !== null && (now - this._lastCheck) < this._checkInterval) {
            return this._backendAvailable;
        }
        
        try {
            const controller = new AbortController();
            const timeout = setTimeout(() => controller.abort(), 3000);
            
            const response = await fetch(`${CONFIG.API_URL}/health`, {
                method: 'GET',
                signal: controller.signal
            });
            
            clearTimeout(timeout);
            this._backendAvailable = response.ok;
            this._lastCheck = now;
            
            if (this._backendAvailable) {
                console.log('✅ Backend متصل');
            }
            return this._backendAvailable;
        } catch {
            this._backendAvailable = false;
            this._lastCheck = now;
            return false;
        }
    },
    
    /**
     * Generic fetch wrapper - silent fail if backend unavailable
     */
    async fetch(endpoint, options = {}) {
        // Skip if backend not available
        if (!(await this.isBackendAvailable())) {
            return null;
        }
        
        try {
            const controller = new AbortController();
            const timeout = setTimeout(() => controller.abort(), 5000);
            
            const response = await fetch(`${CONFIG.API_URL}${endpoint}`, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                signal: controller.signal,
                ...options
            });
            
            clearTimeout(timeout);

            if (!response.ok) {
                return null;
            }

            const result = await response.json();

            // الاستجابة ملفوفة في { Success, Data, Message } أو { success, data, message }
            if (result) {
                // دعم كلا الحالتين (حرف كبير أو صغير)
                if (result.hasOwnProperty('Data')) {
                    return result.Data;
                }
                if (result.hasOwnProperty('data')) {
                    return result.data;
                }
            }

            return result;
        } catch (error) {
            // Silent fail - don't spam console
            return null;
        }
    },

    // ═══════════════════════════════════════════════════════════════════
    // Dashboard
    // ═══════════════════════════════════════════════════════════════════

    async getDashboard() {
        return this.fetch('/dashboard');
    },

    // ═══════════════════════════════════════════════════════════════════
    // Prices
    // ═══════════════════════════════════════════════════════════════════

    async getGlobalPrice() {
        return this.fetch('/prices/global');
    },

    async getKuwaitPrices() {
        return this.fetch('/prices/kuwait');
    },

    async getPriceHistory(hours = 24) {
        return this.fetch(`/prices/history?hours=${hours}`);
    },

    // ═══════════════════════════════════════════════════════════════════
    // News
    // ═══════════════════════════════════════════════════════════════════

    async getNews(count = 10) {
        return this.fetch(`/news?count=${count}`);
    },

    async getNewsImpact() {
        return this.fetch('/news/impact');
    },

    async refreshNews() {
        return this.fetch('/news/refresh', { method: 'POST' });
    },

    // ═══════════════════════════════════════════════════════════════════
    // Predictions
    // ═══════════════════════════════════════════════════════════════════

    async getPrediction(hours = 24) {
        return this.fetch(`/predictions?hours=${hours}`);
    },

    async getShortTermPrediction() {
        return this.fetch('/predictions/short-term');
    },

    // ═══════════════════════════════════════════════════════════════════
    // Recommendations
    // ═══════════════════════════════════════════════════════════════════

    async getActiveRecommendation() {
        return this.fetch('/recommendations/active');
    },

    async getRecommendationHistory(count = 10) {
        return this.fetch(`/recommendations?count=${count}`);
    },

    async generateRecommendation() {
        return this.fetch('/recommendations/generate', { method: 'POST' });
    },

    // ═══════════════════════════════════════════════════════════════════
    // Alerts
    // ═══════════════════════════════════════════════════════════════════

    async getAlerts(count = 20) {
        return this.fetch(`/alerts?count=${count}`);
    },

    async getUnreadAlerts() {
        return this.fetch('/alerts/unread');
    },

    async markAlertAsRead(id) {
        return this.fetch(`/alerts/${id}/read`, { method: 'POST' });
    },

    async markAllAlertsAsRead() {
        return this.fetch('/alerts/read-all', { method: 'POST' });
    },

    // ═══════════════════════════════════════════════════════════════════
    // Reports
    // ═══════════════════════════════════════════════════════════════════

    async getReports(period = null) {
        const url = period ? `/reports?period=${period}` : '/reports';
        return this.fetch(url);
    },

    async generateReport(period) {
        return this.fetch(`/reports/generate?period=${period}`, { method: 'POST' });
    }
};

// Freeze API object
Object.freeze(API);
