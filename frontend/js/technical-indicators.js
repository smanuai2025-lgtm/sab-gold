/**
 * ═══════════════════════════════════════════════════════════════════
 * Mr. Golden Bader - Technical Indicators Service
 * خدمة المؤشرات الفنية المتقدمة - تدمج مع Backend المحلي
 * ═══════════════════════════════════════════════════════════════════
 */

const TechnicalIndicatorsService = {

    cache: {
        comprehensive: null,
        lastUpdate: null,
        cacheDuration: 30000 // 30 ثانية
    },

    /**
     * جلب التحليل الفني الشامل من Backend
     */
    async getComprehensiveAnalysis() {
        try {
            // التحقق من الكاش
            if (this.cache.comprehensive &&
                this.cache.lastUpdate &&
                (Date.now() - this.cache.lastUpdate) < this.cacheDuration) {
                console.log('📦 Using cached technical analysis');
                return this.cache.comprehensive;
            }

            console.log('🔄 Fetching fresh technical analysis...');
            const response = await API.get('/technical-analysis/comprehensive?hours=72');

            if (response.success && response.data) {
                this.cache.comprehensive = response.data;
                this.cache.lastUpdate = Date.now();
                console.log('✅ Technical analysis loaded');
                return response.data;
            }

            throw new Error(response.message || 'Failed to fetch analysis');
        } catch (error) {
            console.error('❌ Error fetching technical analysis:', error);
            throw error;
        }
    },

    /**
     * جلب RSI فقط
     */
    async getRSI(period = 14) {
        try {
            const response = await API.get(`/technical-analysis/rsi?period=${period}&hours=24`);
            return response.success ? response.data : null;
        } catch (error) {
            console.error('Error fetching RSI:', error);
            return null;
        }
    },

    /**
     * جلب MACD فقط
     */
    async getMACD() {
        try {
            const response = await API.get('/technical-analysis/macd?hours=72');
            return response.success ? response.data : null;
        } catch (error) {
            console.error('Error fetching MACD:', error);
            return null;
        }
    },

    /**
     * جلب Bollinger Bands
     */
    async getBollingerBands(period = 20) {
        try {
            const response = await API.get(`/technical-analysis/bollinger?period=${period}&hours=48`);
            return response.success ? response.data : null;
        } catch (error) {
            console.error('Error fetching Bollinger Bands:', error);
            return null;
        }
    },

    /**
     * جلب مستويات الدعم والمقاومة
     */
    async getSupportResistance() {
        try {
            const response = await API.get('/technical-analysis/support-resistance?lookback=30');
            return response.success ? response.data : null;
        } catch (error) {
            console.error('Error fetching Support/Resistance:', error);
            return null;
        }
    },

    /**
     * جلب الأنماط السعرية
     */
    async getPatterns() {
        try {
            const response = await API.get('/technical-analysis/patterns?hours=72');
            return response.success ? response.data : null;
        } catch (error) {
            console.error('Error fetching patterns:', error);
            return null;
        }
    },

    /**
     * مقارنة مع تحليل Gemini
     */
    async compareWithGemini(geminiAnalysis) {
        try {
            console.log('🔄 Comparing local analysis with Gemini...');
            const response = await API.post('/technical-analysis/compare-with-gemini', geminiAnalysis);

            if (response.success && response.data) {
                console.log('✅ Comparison completed');
                return response.data;
            }

            throw new Error(response.message || 'Comparison failed');
        } catch (error) {
            console.error('❌ Error comparing with Gemini:', error);
            throw error;
        }
    },

    /**
     * رسم RSI Gauge
     */
    renderRSIGauge(containerId, rsiData) {
        const container = document.getElementById(containerId);
        if (!container || !rsiData) return;

        const value = rsiData.Value || rsiData.value || 50;
        const signal = rsiData.Signal || rsiData.signal || 'محايد';
        const confidence = rsiData.Confidence || rsiData.confidence || 0;

        // تحديد اللون بناءً على القيمة
        let color, bgColor, label;
        if (value >= 70) {
            color = '#ef4444';
            bgColor = '#fee2e2';
            label = 'ذروة شراء';
        } else if (value >= 60) {
            color = '#f59e0b';
            bgColor = '#fef3c7';
            label = 'قوي صاعد';
        } else if (value <= 30) {
            color = '#10b981';
            bgColor = '#d1fae5';
            label = 'ذروة بيع';
        } else if (value <= 40) {
            color = '#f59e0b';
            bgColor = '#fef3c7';
            label = 'قوي هابط';
        } else {
            color = '#6b7280';
            bgColor = '#f3f4f6';
            label = 'محايد';
        }

        container.innerHTML = `
            <div class="bg-gradient-to-br from-gray-800 to-gray-900 rounded-2xl p-6 border border-gray-700">
                <div class="flex items-center justify-between mb-4">
                    <h3 class="text-lg font-bold text-white flex items-center gap-2">
                        📊 RSI (14)
                    </h3>
                    <span class="px-3 py-1 rounded-full text-xs font-bold"
                          style="background: ${bgColor}; color: ${color}">
                        ${label}
                    </span>
                </div>

                <!-- Gauge -->
                <div class="relative h-40 mb-4">
                    <svg viewBox="0 0 200 120" class="w-full h-full">
                        <!-- Background arc -->
                        <path d="M 20 100 A 80 80 0 0 1 180 100"
                              fill="none"
                              stroke="#374151"
                              stroke-width="12"
                              stroke-linecap="round"/>

                        <!-- Colored segments -->
                        <!-- Oversold (0-30) - Green -->
                        <path d="M 20 100 A 80 80 0 0 1 68 33"
                              fill="none"
                              stroke="#10b981"
                              stroke-width="12"
                              stroke-linecap="round"/>

                        <!-- Neutral (30-70) - Gray -->
                        <path d="M 68 33 A 80 80 0 0 1 132 33"
                              fill="none"
                              stroke="#6b7280"
                              stroke-width="12"
                              stroke-linecap="round"/>

                        <!-- Overbought (70-100) - Red -->
                        <path d="M 132 33 A 80 80 0 0 1 180 100"
                              fill="none"
                              stroke="#ef4444"
                              stroke-width="12"
                              stroke-linecap="round"/>

                        <!-- Needle -->
                        <line x1="100" y1="100"
                              x2="${100 + 70 * Math.cos((180 - value * 1.8) * Math.PI / 180)}"
                              y2="${100 - 70 * Math.sin((180 - value * 1.8) * Math.PI / 180)}"
                              stroke="${color}"
                              stroke-width="3"
                              stroke-linecap="round"/>

                        <!-- Center dot -->
                        <circle cx="100" cy="100" r="6" fill="${color}"/>

                        <!-- Value text -->
                        <text x="100" y="95"
                              text-anchor="middle"
                              font-size="24"
                              font-weight="bold"
                              fill="white">
                            ${value.toFixed(0)}
                        </text>

                        <!-- Labels -->
                        <text x="20" y="115" font-size="10" fill="#9ca3af">0</text>
                        <text x="95" y="25" font-size="10" fill="#9ca3af">50</text>
                        <text x="175" y="115" font-size="10" fill="#9ca3af">100</text>
                    </svg>
                </div>

                <!-- Description -->
                <p class="text-gray-300 text-sm text-center">
                    ${rsiData.Description || signal}
                </p>

                <!-- Confidence -->
                <div class="mt-3 flex items-center justify-center gap-2">
                    <span class="text-xs text-gray-400">الثقة:</span>
                    <div class="w-24 h-2 bg-gray-700 rounded-full overflow-hidden">
                        <div class="h-full rounded-full transition-all"
                             style="width: ${confidence}%; background: ${color}"></div>
                    </div>
                    <span class="text-xs text-gray-400">${confidence}%</span>
                </div>
            </div>
        `;
    },

    /**
     * رسم MACD
     */
    renderMACD(containerId, macdData) {
        const container = document.getElementById(containerId);
        if (!container || !macdData) return;

        const signal = macdData.Signal || macdData.signal || 'محايد';
        const confidence = macdData.Confidence || macdData.confidence || 0;
        const macdLine = macdData.MacdLine || 0;
        const signalLine = macdData.SignalLine || 0;
        const histogram = macdData.Histogram || 0;

        let color, icon;
        if (signal.includes('شراء') || signal.includes('buy')) {
            color = '#10b981';
            icon = '📈';
        } else if (signal.includes('بيع') || signal.includes('sell')) {
            color = '#ef4444';
            icon = '📉';
        } else {
            color = '#6b7280';
            icon = '➡️';
        }

        container.innerHTML = `
            <div class="bg-gradient-to-br from-gray-800 to-gray-900 rounded-2xl p-6 border border-gray-700">
                <div class="flex items-center justify-between mb-4">
                    <h3 class="text-lg font-bold text-white">
                        ${icon} MACD
                    </h3>
                    <span class="px-3 py-1 rounded-full text-xs font-bold text-white"
                          style="background: ${color}">
                        ${signal}
                    </span>
                </div>

                <div class="space-y-3">
                    <!-- MACD Line -->
                    <div class="flex items-center justify-between">
                        <span class="text-sm text-gray-400">MACD Line:</span>
                        <span class="text-white font-mono">${macdLine.toFixed(2)}</span>
                    </div>

                    <!-- Signal Line -->
                    <div class="flex items-center justify-between">
                        <span class="text-sm text-gray-400">Signal Line:</span>
                        <span class="text-white font-mono">${signalLine.toFixed(2)}</span>
                    </div>

                    <!-- Histogram -->
                    <div class="flex items-center justify-between">
                        <span class="text-sm text-gray-400">Histogram:</span>
                        <span class="text-white font-mono font-bold"
                              style="color: ${histogram > 0 ? '#10b981' : '#ef4444'}">
                            ${histogram > 0 ? '+' : ''}${histogram.toFixed(2)}
                        </span>
                    </div>

                    <!-- Visual Bar -->
                    <div class="relative h-3 bg-gray-700 rounded-full overflow-hidden">
                        <div class="absolute inset-y-0 left-1/2 w-0.5 bg-gray-500"></div>
                        <div class="h-full rounded-full transition-all"
                             style="width: ${Math.abs(histogram) * 50}%;
                                    background: ${histogram > 0 ? '#10b981' : '#ef4444'};
                                    margin-left: ${histogram > 0 ? '50%' : 'auto'};
                                    margin-right: ${histogram < 0 ? '50%' : 'auto'}">
                        </div>
                    </div>

                    <!-- Description -->
                    <p class="text-gray-300 text-sm pt-2">
                        ${macdData.Description || signal}
                    </p>

                    <!-- Confidence -->
                    <div class="flex items-center gap-2 pt-2">
                        <span class="text-xs text-gray-400">الثقة:</span>
                        <div class="flex-1 h-2 bg-gray-700 rounded-full overflow-hidden">
                            <div class="h-full rounded-full transition-all"
                                 style="width: ${confidence}%; background: ${color}"></div>
                        </div>
                        <span class="text-xs text-gray-400">${confidence}%</span>
                    </div>
                </div>
            </div>
        `;
    },

    /**
     * رسم Bollinger Bands
     */
    renderBollingerBands(containerId, bollingerData) {
        const container = document.getElementById(containerId);
        if (!container || !bollingerData) return;

        const upper = bollingerData.Upper || 0;
        const middle = bollingerData.Middle || 0;
        const lower = bollingerData.Lower || 0;
        const position = bollingerData.Position || 'وسط النطاق';
        const signal = bollingerData.Signal || 'محايد';
        const bandWidth = bollingerData.BandWidth || 0;

        let color, icon;
        if (position.includes('فوق') || position.includes('أعلى')) {
            color = '#ef4444';
            icon = '⬆️';
        } else if (position.includes('تحت') || position.includes('أسفل')) {
            color = '#10b981';
            icon = '⬇️';
        } else {
            color = '#f59e0b';
            icon = '➡️';
        }

        container.innerHTML = `
            <div class="bg-gradient-to-br from-gray-800 to-gray-900 rounded-2xl p-6 border border-gray-700">
                <div class="flex items-center justify-between mb-4">
                    <h3 class="text-lg font-bold text-white">
                        ${icon} Bollinger Bands
                    </h3>
                    <span class="px-3 py-1 rounded-full text-xs font-bold text-white"
                          style="background: ${color}">
                        ${signal}
                    </span>
                </div>

                <div class="space-y-4">
                    <!-- Visual Representation -->
                    <div class="relative h-32 bg-gray-900 rounded-lg p-4">
                        <!-- Upper Band -->
                        <div class="absolute top-4 left-4 right-4 h-0.5 bg-red-500 opacity-50"></div>
                        <div class="absolute top-4 right-4 text-xs text-red-400">${upper.toFixed(2)}</div>

                        <!-- Middle Band -->
                        <div class="absolute top-1/2 left-4 right-4 h-0.5 bg-yellow-500"></div>
                        <div class="absolute top-1/2 right-4 text-xs text-yellow-400 -mt-2">${middle.toFixed(2)}</div>

                        <!-- Lower Band -->
                        <div class="absolute bottom-4 left-4 right-4 h-0.5 bg-green-500 opacity-50"></div>
                        <div class="absolute bottom-4 right-4 text-xs text-green-400">${lower.toFixed(2)}</div>

                        <!-- Current Position Indicator -->
                        <div class="absolute left-8 w-3 h-3 rounded-full animate-pulse"
                             style="background: ${color};
                                    top: ${position.includes('فوق') ? '1rem' :
                                           position.includes('تحت') ? 'calc(100% - 1.5rem)' : '50%'}">
                        </div>
                    </div>

                    <!-- Stats -->
                    <div class="grid grid-cols-2 gap-3">
                        <div class="bg-gray-900 rounded-lg p-3">
                            <div class="text-xs text-gray-400 mb-1">الموقع</div>
                            <div class="text-sm text-white font-bold">${position}</div>
                        </div>
                        <div class="bg-gray-900 rounded-lg p-3">
                            <div class="text-xs text-gray-400 mb-1">عرض النطاق</div>
                            <div class="text-sm text-white font-bold">${bandWidth.toFixed(2)}%</div>
                        </div>
                    </div>

                    <!-- Description -->
                    <p class="text-gray-300 text-sm">
                        ${bollingerData.Description || position}
                    </p>
                </div>
            </div>
        `;
    },

    /**
     * رسم الدعم والمقاومة
     */
    renderSupportResistance(containerId, srData, currentPrice) {
        const container = document.getElementById(containerId);
        if (!container || !srData) return;

        const support = srData.Support || 0;
        const resistance = srData.Resistance || 0;
        const price = currentPrice || ((support + resistance) / 2);

        const range = resistance - support;
        const pricePosition = ((price - support) / range) * 100;

        container.innerHTML = `
            <div class="bg-gradient-to-br from-gray-800 to-gray-900 rounded-2xl p-6 border border-gray-700">
                <h3 class="text-lg font-bold text-white mb-4">
                    🎯 الدعم والمقاومة
                </h3>

                <div class="space-y-4">
                    <!-- Visual Chart -->
                    <div class="relative h-48 bg-gray-900 rounded-lg p-4">
                        <!-- Resistance Line -->
                        <div class="absolute top-4 left-4 right-4 border-t-2 border-dashed border-red-500"></div>
                        <div class="absolute top-2 right-4 text-xs text-red-400">
                            مقاومة: $${resistance.toFixed(2)}
                        </div>

                        <!-- Support Line -->
                        <div class="absolute bottom-4 left-4 right-4 border-t-2 border-dashed border-green-500"></div>
                        <div class="absolute bottom-2 right-4 text-xs text-green-400">
                            دعم: $${support.toFixed(2)}
                        </div>

                        <!-- Current Price -->
                        <div class="absolute left-4 right-4 flex items-center"
                             style="top: ${100 - pricePosition}%">
                            <div class="flex-1 h-0.5 bg-gold-500"></div>
                            <div class="px-3 py-1 bg-gold-500 rounded-full text-xs font-bold text-gray-900">
                                السعر: $${price.toFixed(2)}
                            </div>
                        </div>
                    </div>

                    <!-- Stats -->
                    <div class="grid grid-cols-2 gap-3">
                        <div class="bg-gray-900 rounded-lg p-3">
                            <div class="text-xs text-gray-400 mb-1">المسافة للمقاومة</div>
                            <div class="text-sm text-red-400 font-bold">
                                ${srData.DistanceToResistance?.toFixed(2) || '0.00'}%
                            </div>
                        </div>
                        <div class="bg-gray-900 rounded-lg p-3">
                            <div class="text-xs text-gray-400 mb-1">المسافة للدعم</div>
                            <div class="text-sm text-green-400 font-bold">
                                ${srData.DistanceToSupport?.toFixed(2) || '0.00'}%
                            </div>
                        </div>
                    </div>

                    <!-- Description -->
                    <p class="text-gray-300 text-sm">
                        ${srData.Description || 'مستويات الدعم والمقاومة الحالية'}
                    </p>
                </div>
            </div>
        `;
    },

    /**
     * رسم الأنماط المكتشفة
     */
    renderPatterns(containerId, patternsData) {
        const container = document.getElementById(containerId);
        if (!container || !patternsData) return;

        const patterns = patternsData.DetectedPatterns || [];
        const primaryPattern = patternsData.PrimaryPattern;

        container.innerHTML = `
            <div class="bg-gradient-to-br from-gray-800 to-gray-900 rounded-2xl p-6 border border-gray-700">
                <div class="flex items-center justify-between mb-4">
                    <h3 class="text-lg font-bold text-white">
                        🔍 الأنماط المكتشفة
                    </h3>
                    <span class="px-3 py-1 bg-gray-700 rounded-full text-xs text-gray-300">
                        ${patterns.length} نمط
                    </span>
                </div>

                ${primaryPattern ? `
                    <div class="mb-4 p-4 bg-gradient-to-r from-gold-500/10 to-gold-600/10 border border-gold-500/30 rounded-xl">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-gold-400 font-bold">🌟 النمط الرئيسي</span>
                            <span class="px-2 py-1 bg-gold-500/20 rounded text-xs text-gold-300">
                                ثقة: ${primaryPattern.Confidence || primaryPattern.confidence}%
                            </span>
                        </div>
                        <div class="text-white font-bold mb-1">
                            ${primaryPattern.PatternName || primaryPattern.patternName}
                        </div>
                        <div class="text-sm text-gray-300">
                            ${primaryPattern.Description || primaryPattern.description}
                        </div>
                        ${primaryPattern.TargetPrice ? `
                            <div class="mt-2 flex gap-4 text-xs">
                                <span class="text-green-400">هدف: $${primaryPattern.TargetPrice.toFixed(2)}</span>
                                ${primaryPattern.StopLoss ? `
                                    <span class="text-red-400">وقف: $${primaryPattern.StopLoss.toFixed(2)}</span>
                                ` : ''}
                            </div>
                        ` : ''}
                    </div>
                ` : ''}

                ${patterns.length > 0 ? `
                    <div class="space-y-2">
                        ${patterns.map(pattern => `
                            <div class="p-3 bg-gray-900 rounded-lg border border-gray-700">
                                <div class="flex items-center justify-between mb-1">
                                    <span class="text-white font-semibold text-sm">
                                        ${pattern.PatternName || pattern.patternName}
                                    </span>
                                    <span class="text-xs ${
                                        (pattern.Signal || pattern.signal).includes('شراء') ? 'text-green-400' :
                                        (pattern.Signal || pattern.signal).includes('بيع') ? 'text-red-400' :
                                        'text-gray-400'
                                    }">
                                        ${pattern.Signal || pattern.signal}
                                    </span>
                                </div>
                                <div class="text-xs text-gray-400">
                                    ${pattern.Description || pattern.description}
                                </div>
                            </div>
                        `).join('')}
                    </div>
                ` : `
                    <div class="text-center py-8 text-gray-400">
                        <div class="text-4xl mb-2">📊</div>
                        <div>لم يتم اكتشاف أنماط واضحة</div>
                    </div>
                `}
            </div>
        `;
    },

    /**
     * تنظيف الكاش
     */
    clearCache() {
        this.cache.comprehensive = null;
        this.cache.lastUpdate = null;
    }
};

// Export
window.TechnicalIndicatorsService = TechnicalIndicatorsService;
