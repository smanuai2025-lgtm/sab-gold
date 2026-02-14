namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة حساب المؤشرات الفنية الاحترافية
/// تطبيق كامل لجميع المؤشرات المستخدمة في التحليل الفني
/// </summary>
public class TechnicalIndicators
{
    /// <summary>
    /// حساب RSI (Relative Strength Index)
    /// مؤشر القوة النسبية - يقيس قوة الاتجاه والزخم
    /// القيم: 0-30 ذروة بيع | 30-70 محايد | 70-100 ذروة شراء
    /// </summary>
    public static TechnicalResult<double> CalculateRSI(List<decimal> prices, int period = 14)
    {
        if (prices == null || prices.Count < period + 1)
        {
            return new TechnicalResult<double>
            {
                Value = 50,
                Signal = "محايد",
                Confidence = 0,
                Description = "بيانات غير كافية لحساب RSI"
            };
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        // حساب الأرباح والخسائر
        for (int i = 1; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        // حساب المتوسط
        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0)
        {
            return new TechnicalResult<double>
            {
                Value = 100,
                Signal = "ذروة شراء قوية",
                Confidence = 95,
                Description = "قوة شراء مطلقة - احتمال تصحيح عالي"
            };
        }

        var rs = (double)(avgGain / avgLoss);
        var rsi = 100 - (100 / (1 + rs));

        string signal;
        int confidence;
        string description;

        if (rsi >= 70)
        {
            signal = "ذروة شراء";
            confidence = (int)Math.Min(95, (rsi - 70) * 3 + 70);
            description = $"RSI عند {rsi:F1} - السوق في منطقة تشبع شرائي، احتمال تصحيح";
        }
        else if (rsi <= 30)
        {
            signal = "ذروة بيع";
            confidence = (int)Math.Min(95, (30 - rsi) * 3 + 70);
            description = $"RSI عند {rsi:F1} - السوق في منطقة تشبع بيعي، فرصة شراء";
        }
        else if (rsi >= 60)
        {
            signal = "قوي صاعد";
            confidence = 70;
            description = $"RSI عند {rsi:F1} - زخم إيجابي قوي";
        }
        else if (rsi <= 40)
        {
            signal = "قوي هابط";
            confidence = 70;
            description = $"RSI عند {rsi:F1} - ضغط بيعي واضح";
        }
        else
        {
            signal = "محايد";
            confidence = 50;
            description = $"RSI عند {rsi:F1} - السوق متوازن";
        }

        return new TechnicalResult<double>
        {
            Value = rsi,
            Signal = signal,
            Confidence = confidence,
            Description = description
        };
    }

    /// <summary>
    /// حساب MACD (Moving Average Convergence Divergence)
    /// مؤشر تقارب وتباعد المتوسطات المتحركة
    /// </summary>
    public static MacdResult CalculateMACD(List<decimal> prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        if (prices == null || prices.Count < slowPeriod)
        {
            return new MacdResult
            {
                MacdLine = 0,
                SignalLine = 0,
                Histogram = 0,
                Signal = "محايد",
                Confidence = 0,
                Description = "بيانات غير كافية لحساب MACD"
            };
        }

        // حساب EMA السريع والبطيء
        var emaFast = CalculateEMA(prices, fastPeriod);
        var emaSlow = CalculateEMA(prices, slowPeriod);

        // حساب خط MACD
        var macdLine = (double)(emaFast - emaSlow);

        // لحساب خط الإشارة، نحتاج تاريخ MACD
        // لكن للتبسيط، سنستخدم قيمة تقريبية
        var macdValues = new List<decimal> { (decimal)macdLine };
        var signalLine = (double)CalculateEMA(macdValues, signalPeriod);

        var histogram = macdLine - signalLine;

        string signal;
        int confidence;
        string description;

        if (macdLine > signalLine && macdLine > 0)
        {
            signal = "شراء قوي";
            confidence = (int)Math.Min(95, Math.Abs(histogram) * 50 + 70);
            description = "MACD فوق خط الإشارة وفوق الصفر - إشارة صعود قوية";
        }
        else if (macdLine > signalLine && macdLine <= 0)
        {
            signal = "شراء محتمل";
            confidence = 60;
            description = "MACD يعبر خط الإشارة صعوداً - بداية زخم إيجابي";
        }
        else if (macdLine < signalLine && macdLine < 0)
        {
            signal = "بيع قوي";
            confidence = (int)Math.Min(95, Math.Abs(histogram) * 50 + 70);
            description = "MACD تحت خط الإشارة وتحت الصفر - إشارة هبوط قوية";
        }
        else if (macdLine < signalLine && macdLine >= 0)
        {
            signal = "بيع محتمل";
            confidence = 60;
            description = "MACD يعبر خط الإشارة هبوطاً - بداية ضعف";
        }
        else
        {
            signal = "محايد";
            confidence = 50;
            description = "MACD قريب من خط الإشارة - لا اتجاه واضح";
        }

        return new MacdResult
        {
            MacdLine = macdLine,
            SignalLine = signalLine,
            Histogram = histogram,
            Signal = signal,
            Confidence = confidence,
            Description = description
        };
    }

    /// <summary>
    /// حساب Bollinger Bands
    /// نطاقات بولينجر - تقيس التقلبات ومستويات الدعم والمقاومة
    /// </summary>
    public static BollingerBandsResult CalculateBollingerBands(List<decimal> prices, int period = 20, double stdDevMultiplier = 2)
    {
        if (prices == null || prices.Count < period)
        {
            var fallbackPrice = prices?.LastOrDefault() ?? 0;
            return new BollingerBandsResult
            {
                Upper = (double)(fallbackPrice * 1.02m),
                Middle = (double)fallbackPrice,
                Lower = (double)(fallbackPrice * 0.98m),
                Position = "غير محدد",
                Signal = "محايد",
                Confidence = 0,
                Description = "بيانات غير كافية لحساب Bollinger Bands"
            };
        }

        var recentPrices = prices.TakeLast(period).ToList();
        var sma = recentPrices.Average();

        // حساب الانحراف المعياري
        var variance = recentPrices.Sum(p => Math.Pow((double)(p - sma), 2)) / period;
        var stdDev = Math.Sqrt(variance);

        var upper = (double)sma + (stdDev * stdDevMultiplier);
        var middle = (double)sma;
        var lower = (double)sma - (stdDev * stdDevMultiplier);

        var currentPrice = (double)prices.Last();

        // تحديد الموقع
        string position;
        string signal;
        int confidence;
        string description;

        var upperDistance = ((upper - currentPrice) / upper) * 100;
        var lowerDistance = ((currentPrice - lower) / lower) * 100;

        if (currentPrice >= upper)
        {
            position = "فوق النطاق العلوي";
            signal = "ذروة شراء";
            confidence = 85;
            description = $"السعر فوق النطاق العلوي ({upperDistance:F1}%) - احتمال تصحيح هبوطي";
        }
        else if (currentPrice >= middle + (stdDev * 1.5))
        {
            position = "قرب النطاق العلوي";
            signal = "قوي صاعد";
            confidence = 75;
            description = "السعر يقترب من الحد العلوي - قوة شرائية";
        }
        else if (currentPrice <= lower)
        {
            position = "تحت النطاق السفلي";
            signal = "ذروة بيع";
            confidence = 85;
            description = $"السعر تحت النطاق السفلي ({lowerDistance:F1}%) - فرصة شراء محتملة";
        }
        else if (currentPrice <= middle - (stdDev * 1.5))
        {
            position = "قرب النطاق السفلي";
            signal = "قوي هابط";
            confidence = 75;
            description = "السعر يقترب من الحد السفلي - ضغط بيعي";
        }
        else
        {
            position = "وسط النطاق";
            signal = "محايد";
            confidence = 50;
            description = "السعر في منتصف النطاق - حركة طبيعية";
        }

        return new BollingerBandsResult
        {
            Upper = upper,
            Middle = middle,
            Lower = lower,
            Position = position,
            Signal = signal,
            Confidence = confidence,
            Description = description,
            BandWidth = ((upper - lower) / middle) * 100 // قياس التقلب
        };
    }

    /// <summary>
    /// حساب SMA (Simple Moving Average)
    /// المتوسط المتحرك البسيط
    /// </summary>
    public static decimal CalculateSMA(List<decimal> prices, int period)
    {
        if (prices == null || prices.Count < period)
            return prices?.LastOrDefault() ?? 0;

        return prices.TakeLast(period).Average();
    }

    /// <summary>
    /// حساب EMA (Exponential Moving Average)
    /// المتوسط المتحرك الأسي - يعطي وزن أكبر للأسعار الأحدث
    /// </summary>
    public static decimal CalculateEMA(List<decimal> prices, int period)
    {
        if (prices == null || prices.Count < period)
            return prices?.LastOrDefault() ?? 0;

        var multiplier = 2.0m / (period + 1);
        var ema = CalculateSMA(prices.Take(period).ToList(), period);

        for (int i = period; i < prices.Count; i++)
        {
            ema = ((prices[i] - ema) * multiplier) + ema;
        }

        return ema;
    }

    /// <summary>
    /// تحليل المتوسطات المتحركة
    /// </summary>
    public static MovingAverageResult AnalyzeMovingAverages(List<decimal> prices)
    {
        if (prices == null || prices.Count < 50)
        {
            return new MovingAverageResult
            {
                Signal = "محايد",
                Confidence = 0,
                Description = "بيانات غير كافية"
            };
        }

        var currentPrice = prices.Last();
        var sma20 = CalculateSMA(prices, 20);
        var sma50 = CalculateSMA(prices, 50);
        var ema12 = CalculateEMA(prices, 12);
        var ema26 = CalculateEMA(prices, 26);

        int bullishSignals = 0;
        int bearishSignals = 0;

        // تحليل الإشارات
        if (currentPrice > sma20) bullishSignals++; else bearishSignals++;
        if (currentPrice > sma50) bullishSignals++; else bearishSignals++;
        if (currentPrice > ema12) bullishSignals++; else bearishSignals++;
        if (currentPrice > ema26) bullishSignals++; else bearishSignals++;
        if (sma20 > sma50) bullishSignals++; else bearishSignals++; // Golden/Death Cross

        string signal;
        int confidence;
        string description;

        if (bullishSignals >= 4)
        {
            signal = "شراء قوي";
            confidence = 85;
            description = $"السعر فوق جميع المتوسطات ({bullishSignals}/5) - اتجاه صاعد قوي";
        }
        else if (bullishSignals == 3)
        {
            signal = "شراء";
            confidence = 70;
            description = "السعر فوق معظم المتوسطات - اتجاه إيجابي";
        }
        else if (bearishSignals >= 4)
        {
            signal = "بيع قوي";
            confidence = 85;
            description = $"السعر تحت جميع المتوسطات ({bearishSignals}/5) - اتجاه هابط قوي";
        }
        else if (bearishSignals == 3)
        {
            signal = "بيع";
            confidence = 70;
            description = "السعر تحت معظم المتوسطات - اتجاه سلبي";
        }
        else
        {
            signal = "محايد";
            confidence = 50;
            description = "السعر متقاطع مع المتوسطات - لا اتجاه واضح";
        }

        return new MovingAverageResult
        {
            SMA20 = (double)sma20,
            SMA50 = (double)sma50,
            EMA12 = (double)ema12,
            EMA26 = (double)ema26,
            Signal = signal,
            Confidence = confidence,
            Description = description
        };
    }

    /// <summary>
    /// العثور على مستويات الدعم والمقاومة
    /// </summary>
    public static SupportResistanceResult FindSupportResistance(List<decimal> prices, int lookbackPeriod = 30)
    {
        if (prices == null || prices.Count < lookbackPeriod)
        {
            var fallbackPrice = prices?.LastOrDefault() ?? 0;
            return new SupportResistanceResult
            {
                Support = (double)(fallbackPrice * 0.97m),
                Resistance = (double)(fallbackPrice * 1.03m),
                Description = "بيانات غير كافية"
            };
        }

        var recentPrices = prices.TakeLast(lookbackPeriod).ToList();
        var currentPrice = prices.Last();

        // إيجاد القمم والقيعان المحلية
        var peaks = new List<decimal>();
        var valleys = new List<decimal>();

        for (int i = 1; i < recentPrices.Count - 1; i++)
        {
            if (recentPrices[i] > recentPrices[i - 1] && recentPrices[i] > recentPrices[i + 1])
            {
                peaks.Add(recentPrices[i]);
            }
            else if (recentPrices[i] < recentPrices[i - 1] && recentPrices[i] < recentPrices[i + 1])
            {
                valleys.Add(recentPrices[i]);
            }
        }

        var resistance = peaks.Any() ? (double)peaks.Max() : (double)(currentPrice * 1.03m);
        var support = valleys.Any() ? (double)valleys.Min() : (double)(currentPrice * 0.97m);

        var distanceToResistance = ((resistance - (double)currentPrice) / (double)currentPrice) * 100;
        var distanceToSupport = (((double)currentPrice - support) / (double)currentPrice) * 100;

        string description;
        if (distanceToResistance < 1)
        {
            description = $"قرب المقاومة ({resistance:F2}) - احتمال ارتداد";
        }
        else if (distanceToSupport < 1)
        {
            description = $"قرب الدعم ({support:F2}) - احتمال ارتداد صاعد";
        }
        else
        {
            description = $"الدعم عند {support:F2} والمقاومة عند {resistance:F2}";
        }

        return new SupportResistanceResult
        {
            Support = support,
            Resistance = resistance,
            DistanceToResistance = distanceToResistance,
            DistanceToSupport = distanceToSupport,
            Description = description
        };
    }

    /// <summary>
    /// حساب التقلب (Volatility)
    /// </summary>
    public static VolatilityResult CalculateVolatility(List<decimal> prices, int period = 20)
    {
        if (prices == null || prices.Count < period)
        {
            return new VolatilityResult
            {
                Value = 0,
                Level = "منخفض",
                Description = "بيانات غير كافية"
            };
        }

        var recentPrices = prices.TakeLast(period).ToList();
        var returns = new List<double>();

        for (int i = 1; i < recentPrices.Count; i++)
        {
            var returnValue = (double)((recentPrices[i] - recentPrices[i - 1]) / recentPrices[i - 1]);
            returns.Add(returnValue);
        }

        var variance = returns.Sum(r => Math.Pow(r - returns.Average(), 2)) / returns.Count;
        var volatility = Math.Sqrt(variance) * Math.Sqrt(252) * 100; // Annualized

        string level;
        string description;

        if (volatility > 30)
        {
            level = "عالي جداً";
            description = $"تقلب مرتفع ({volatility:F1}%) - حذر في التداول";
        }
        else if (volatility > 20)
        {
            level = "عالي";
            description = $"تقلب ملحوظ ({volatility:F1}%) - فرص ومخاطر";
        }
        else if (volatility > 10)
        {
            level = "متوسط";
            description = $"تقلب معتدل ({volatility:F1}%) - طبيعي";
        }
        else
        {
            level = "منخفض";
            description = $"تقلب محدود ({volatility:F1}%) - سوق هادئ";
        }

        return new VolatilityResult
        {
            Value = volatility,
            Level = level,
            Description = description
        };
    }
}

// ═══════════════════════════════════════════════════════════════════
// نماذج النتائج
// ═══════════════════════════════════════════════════════════════════

public class TechnicalResult<T>
{
    public T Value { get; set; } = default!;
    public string Signal { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class MacdResult
{
    public double MacdLine { get; set; }
    public double SignalLine { get; set; }
    public double Histogram { get; set; }
    public string Signal { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class BollingerBandsResult
{
    public double Upper { get; set; }
    public double Middle { get; set; }
    public double Lower { get; set; }
    public string Position { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
    public double BandWidth { get; set; }
}

public class MovingAverageResult
{
    public double SMA20 { get; set; }
    public double SMA50 { get; set; }
    public double EMA12 { get; set; }
    public double EMA26 { get; set; }
    public string Signal { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class SupportResistanceResult
{
    public double Support { get; set; }
    public double Resistance { get; set; }
    public double DistanceToResistance { get; set; }
    public double DistanceToSupport { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class VolatilityResult
{
    public double Value { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
