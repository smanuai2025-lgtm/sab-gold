namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة التعرف على الأنماط السعرية (Chart Patterns)
/// تكتشف الأنماط الكلاسيكية وتعطي توقعات بناءً عليها
/// </summary>
public class PatternRecognition
{
    /// <summary>
    /// تحليل شامل لجميع الأنماط
    /// </summary>
    public static PatternAnalysisResult AnalyzePatterns(List<decimal> prices, List<decimal> highs, List<decimal> lows)
    {
        if (prices == null || prices.Count < 10)
        {
            return new PatternAnalysisResult
            {
                DetectedPatterns = new List<PatternResult>(),
                PrimaryPattern = null,
                OverallSignal = "محايد",
                Confidence = 0
            };
        }

        var detectedPatterns = new List<PatternResult>();

        // فحص الأنماط المختلفة
        var doubleTop = DetectDoubleTop(prices, highs);
        if (doubleTop.IsDetected) detectedPatterns.Add(doubleTop);

        var doubleBottom = DetectDoubleBottom(prices, lows);
        if (doubleBottom.IsDetected) detectedPatterns.Add(doubleBottom);

        var headAndShoulders = DetectHeadAndShoulders(prices, highs);
        if (headAndShoulders.IsDetected) detectedPatterns.Add(headAndShoulders);

        var trianglePattern = DetectTriangle(prices, highs, lows);
        if (trianglePattern.IsDetected) detectedPatterns.Add(trianglePattern);

        var trendPattern = DetectTrend(prices);
        if (trendPattern.IsDetected) detectedPatterns.Add(trendPattern);

        var consolidation = DetectConsolidation(prices);
        if (consolidation.IsDetected) detectedPatterns.Add(consolidation);

        // تحديد النمط الأساسي (الأقوى)
        var primaryPattern = detectedPatterns
            .OrderByDescending(p => p.Confidence)
            .FirstOrDefault();

        // تحديد الإشارة العامة
        string overallSignal;
        int overallConfidence;

        if (primaryPattern != null)
        {
            overallSignal = primaryPattern.Signal;
            overallConfidence = primaryPattern.Confidence;
        }
        else
        {
            overallSignal = "محايد";
            overallConfidence = 0;
        }

        return new PatternAnalysisResult
        {
            DetectedPatterns = detectedPatterns,
            PrimaryPattern = primaryPattern,
            OverallSignal = overallSignal,
            Confidence = overallConfidence
        };
    }

    /// <summary>
    /// كشف نمط القمة المزدوجة (Double Top) - نمط هبوطي
    /// </summary>
    private static PatternResult DetectDoubleTop(List<decimal> prices, List<decimal> highs)
    {
        if (prices.Count < 20 || highs.Count < 20)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentPrices = prices.TakeLast(20).ToList();
        var recentHighs = highs.TakeLast(20).ToList();

        // البحث عن قمتين متقاربتين
        var peaks = new List<(int index, decimal value)>();
        for (int i = 1; i < recentHighs.Count - 1; i++)
        {
            if (recentHighs[i] > recentHighs[i - 1] && recentHighs[i] > recentHighs[i + 1])
            {
                peaks.Add((i, recentHighs[i]));
            }
        }

        if (peaks.Count < 2) return new PatternResult { IsDetected = false };

        // فحص آخر قمتين
        var lastTwoPeaks = peaks.TakeLast(2).ToList();
        var peak1 = lastTwoPeaks[0].value;
        var peak2 = lastTwoPeaks[1].value;

        // القمتان يجب أن تكونا متقاربتين (ضمن 2%)
        var difference = Math.Abs(peak1 - peak2) / Math.Max(peak1, peak2);

        if (difference <= 0.02m) // 2% tolerance
        {
            var currentPrice = prices.Last();
            var neckline = Math.Min(recentPrices[lastTwoPeaks[0].index], recentPrices[lastTwoPeaks[1].index]);
            var isBelowNeckline = currentPrice < neckline;

            return new PatternResult
            {
                IsDetected = true,
                PatternName = "قمة مزدوجة",
                Signal = "بيع",
                Confidence = isBelowNeckline ? 85 : 70,
                Description = isBelowNeckline
                    ? "نمط قمة مزدوجة مكتمل - كسر خط العنق، إشارة هبوط قوية"
                    : "نمط قمة مزدوجة محتمل - لم يكسر خط العنق بعد",
                TargetPrice = (double)(neckline - (Math.Max(peak1, peak2) - neckline)),
                StopLoss = (double)Math.Max(peak1, peak2)
            };
        }

        return new PatternResult { IsDetected = false };
    }

    /// <summary>
    /// كشف نمط القاع المزدوج (Double Bottom) - نمط صاعد
    /// </summary>
    private static PatternResult DetectDoubleBottom(List<decimal> prices, List<decimal> lows)
    {
        if (prices.Count < 20 || lows.Count < 20)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentPrices = prices.TakeLast(20).ToList();
        var recentLows = lows.TakeLast(20).ToList();

        // البحث عن قاعين متقاربين
        var valleys = new List<(int index, decimal value)>();
        for (int i = 1; i < recentLows.Count - 1; i++)
        {
            if (recentLows[i] < recentLows[i - 1] && recentLows[i] < recentLows[i + 1])
            {
                valleys.Add((i, recentLows[i]));
            }
        }

        if (valleys.Count < 2) return new PatternResult { IsDetected = false };

        var lastTwoValleys = valleys.TakeLast(2).ToList();
        var valley1 = lastTwoValleys[0].value;
        var valley2 = lastTwoValleys[1].value;

        var difference = Math.Abs(valley1 - valley2) / Math.Max(valley1, valley2);

        if (difference <= 0.02m)
        {
            var currentPrice = prices.Last();
            var neckline = Math.Max(recentPrices[lastTwoValleys[0].index], recentPrices[lastTwoValleys[1].index]);
            var isAboveNeckline = currentPrice > neckline;

            return new PatternResult
            {
                IsDetected = true,
                PatternName = "قاع مزدوج",
                Signal = "شراء",
                Confidence = isAboveNeckline ? 85 : 70,
                Description = isAboveNeckline
                    ? "نمط قاع مزدوج مكتمل - كسر خط العنق صعوداً، إشارة صعود قوية"
                    : "نمط قاع مزدوج محتمل - لم يكسر خط العنق بعد",
                TargetPrice = (double)(neckline + (neckline - Math.Min(valley1, valley2))),
                StopLoss = (double)Math.Min(valley1, valley2)
            };
        }

        return new PatternResult { IsDetected = false };
    }

    /// <summary>
    /// كشف نمط الرأس والكتفين (Head and Shoulders) - نمط انعكاسي هبوطي
    /// </summary>
    private static PatternResult DetectHeadAndShoulders(List<decimal> prices, List<decimal> highs)
    {
        if (prices.Count < 30 || highs.Count < 30)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentHighs = highs.TakeLast(30).ToList();

        // البحث عن 3 قمم
        var peaks = new List<(int index, decimal value)>();
        for (int i = 2; i < recentHighs.Count - 2; i++)
        {
            if (recentHighs[i] > recentHighs[i - 1] &&
                recentHighs[i] > recentHighs[i - 2] &&
                recentHighs[i] > recentHighs[i + 1] &&
                recentHighs[i] > recentHighs[i + 2])
            {
                peaks.Add((i, recentHighs[i]));
            }
        }

        if (peaks.Count < 3) return new PatternResult { IsDetected = false };

        var lastThreePeaks = peaks.TakeLast(3).ToList();
        var leftShoulder = lastThreePeaks[0].value;
        var head = lastThreePeaks[1].value;
        var rightShoulder = lastThreePeaks[2].value;

        // الرأس يجب أن يكون أعلى من الكتفين
        // والكتفان متقاربان في الارتفاع
        if (head > leftShoulder && head > rightShoulder)
        {
            var shoulderDifference = Math.Abs(leftShoulder - rightShoulder) / Math.Max(leftShoulder, rightShoulder);

            if (shoulderDifference <= 0.03m) // 3% tolerance
            {
                var currentPrice = prices.Last();
                var neckline = (leftShoulder + rightShoulder) / 2;
                var isBelowNeckline = currentPrice < neckline;

                return new PatternResult
                {
                    IsDetected = true,
                    PatternName = "رأس وكتفين",
                    Signal = "بيع",
                    Confidence = isBelowNeckline ? 90 : 75,
                    Description = isBelowNeckline
                        ? "نمط رأس وكتفين مكتمل - انعكاس هبوطي قوي"
                        : "نمط رأس وكتفين محتمل - انتظر كسر خط العنق",
                    TargetPrice = (double)(neckline - (head - neckline)),
                    StopLoss = (double)head
                };
            }
        }

        return new PatternResult { IsDetected = false };
    }

    /// <summary>
    /// كشف نمط المثلث (Triangle Pattern)
    /// </summary>
    private static PatternResult DetectTriangle(List<decimal> prices, List<decimal> highs, List<decimal> lows)
    {
        if (prices.Count < 15)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentPrices = prices.TakeLast(15).ToList();
        var recentHighs = highs.TakeLast(15).ToList();
        var recentLows = lows.TakeLast(15).ToList();

        // حساب نطاق التداول
        var firstHalf = recentPrices.Take(7).ToList();
        var secondHalf = recentPrices.Skip(8).ToList();

        var firstHalfRange = firstHalf.Max() - firstHalf.Min();
        var secondHalfRange = secondHalf.Max() - secondHalf.Min();

        // المثلث = تقلص النطاق
        if (secondHalfRange < firstHalfRange * 0.7m)
        {
            // تحديد نوع المثلث
            var isAscending = recentLows.Last() > recentLows.First();
            var isDescending = recentHighs.Last() < recentHighs.First();

            string triangleType;
            string signal;
            int confidence;
            string description;

            if (isAscending && !isDescending)
            {
                triangleType = "مثلث صاعد";
                signal = "شراء";
                confidence = 75;
                description = "نمط مثلث صاعد - قيعان مرتفعة، احتمال كسر صعودي";
            }
            else if (isDescending && !isAscending)
            {
                triangleType = "مثلث هابط";
                signal = "بيع";
                confidence = 75;
                description = "نمط مثلث هابط - قمم منخفضة، احتمال كسر هبوطي";
            }
            else
            {
                triangleType = "مثلث متماثل";
                signal = "انتظار الكسر";
                confidence = 60;
                description = "نمط مثلث متماثل - انتظر كسر النطاق لتحديد الاتجاه";
            }

            return new PatternResult
            {
                IsDetected = true,
                PatternName = triangleType,
                Signal = signal,
                Confidence = confidence,
                Description = description
            };
        }

        return new PatternResult { IsDetected = false };
    }

    /// <summary>
    /// كشف الاتجاه (Trend)
    /// </summary>
    private static PatternResult DetectTrend(List<decimal> prices)
    {
        if (prices.Count < 10)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentPrices = prices.TakeLast(10).ToList();
        var oldest = recentPrices.First();
        var newest = recentPrices.Last();
        var change = ((newest - oldest) / oldest) * 100;

        // حساب عدد الارتفاعات مقابل الانخفاضات
        int ups = 0, downs = 0;
        for (int i = 1; i < recentPrices.Count; i++)
        {
            if (recentPrices[i] > recentPrices[i - 1]) ups++;
            else if (recentPrices[i] < recentPrices[i - 1]) downs++;
        }

        if (change > 0.5m && ups > downs)
        {
            var strength = Math.Min(95, 60 + (int)(Math.Abs((double)change) * 10));
            return new PatternResult
            {
                IsDetected = true,
                PatternName = "اتجاه صاعد",
                Signal = "شراء",
                Confidence = strength,
                Description = $"اتجاه صاعد قوي - ارتفاع {change:F2}% خلال آخر 10 نقاط"
            };
        }
        else if (change < -0.5m && downs > ups)
        {
            var strength = Math.Min(95, 60 + (int)(Math.Abs((double)change) * 10));
            return new PatternResult
            {
                IsDetected = true,
                PatternName = "اتجاه هابط",
                Signal = "بيع",
                Confidence = strength,
                Description = $"اتجاه هابط قوي - انخفاض {change:F2}% خلال آخر 10 نقاط"
            };
        }

        return new PatternResult { IsDetected = false };
    }

    /// <summary>
    /// كشف حالة التماسك/التذبذب (Consolidation)
    /// </summary>
    private static PatternResult DetectConsolidation(List<decimal> prices)
    {
        if (prices.Count < 10)
        {
            return new PatternResult { IsDetected = false };
        }

        var recentPrices = prices.TakeLast(10).ToList();
        var max = recentPrices.Max();
        var min = recentPrices.Min();
        var range = ((max - min) / min) * 100;

        // إذا كان النطاق ضيق جداً
        if (range < 0.5m)
        {
            return new PatternResult
            {
                IsDetected = true,
                PatternName = "تماسك",
                Signal = "انتظار",
                Confidence = 70,
                Description = $"السوق في حالة تماسك - نطاق ضيق ({range:F2}%)، انتظر كسر النطاق"
            };
        }

        return new PatternResult { IsDetected = false };
    }
}

// ═══════════════════════════════════════════════════════════════════
// نماذج النتائج
// ═══════════════════════════════════════════════════════════════════

public class PatternAnalysisResult
{
    public List<PatternResult> DetectedPatterns { get; set; } = new();
    public PatternResult? PrimaryPattern { get; set; }
    public string OverallSignal { get; set; } = string.Empty;
    public int Confidence { get; set; }
}

public class PatternResult
{
    public bool IsDetected { get; set; }
    public string PatternName { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
    public double? TargetPrice { get; set; }
    public double? StopLoss { get; set; }
}
