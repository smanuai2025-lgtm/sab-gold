using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.Services;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// وحدة تحكم التحليل الفني المتقدم
/// توفر نقاط نهاية للوصول للمؤشرات الفنية والأنماط
/// </summary>
[AllowAnonymous]
public class TechnicalAnalysisController : BaseApiController
{
    private readonly IGoldPriceRepository _priceRepo;
    private readonly ILogger<TechnicalAnalysisController> _logger;

    public TechnicalAnalysisController(
        IGoldPriceRepository priceRepo,
        ILogger<TechnicalAnalysisController> logger)
    {
        _priceRepo = priceRepo;
        _logger = logger;
    }

    /// <summary>
    /// التحليل الفني الشامل
    /// </summary>
    [HttpGet("comprehensive")]
    public async Task<IActionResult> GetComprehensiveAnalysis([FromQuery] int hours = 72)
    {
        try
        {
            var historicalPrices = await _priceRepo.GetLastHoursAsync(hours);
            var priceList = historicalPrices.OrderBy(p => p.Timestamp).ToList();

            if (priceList.Count < 20)
            {
                return Error("بيانات غير كافية للتحليل", 400);
            }

            var closePrices = priceList.Select(p => p.Close).ToList();
            var highPrices = priceList.Select(p => p.High).ToList();
            var lowPrices = priceList.Select(p => p.Low).ToList();
            var currentPrice = priceList.Last();

            // حساب جميع المؤشرات
            var rsi = TechnicalIndicators.CalculateRSI(closePrices, 14);
            var macd = TechnicalIndicators.CalculateMACD(closePrices);
            var bollinger = TechnicalIndicators.CalculateBollingerBands(closePrices, 20);
            var movingAvg = TechnicalIndicators.AnalyzeMovingAverages(closePrices);
            var supportResistance = TechnicalIndicators.FindSupportResistance(closePrices, 30);
            var volatility = TechnicalIndicators.CalculateVolatility(closePrices, 20);
            var patterns = PatternRecognition.AnalyzePatterns(closePrices, highPrices, lowPrices);

            var result = new
            {
                CurrentPrice = new
                {
                    Close = currentPrice.Close,
                    High = currentPrice.High,
                    Low = currentPrice.Low,
                    Timestamp = currentPrice.Timestamp
                },
                Indicators = new
                {
                    RSI = new
                    {
                        Value = rsi.Value,
                        Signal = rsi.Signal,
                        Confidence = rsi.Confidence,
                        Description = rsi.Description
                    },
                    MACD = new
                    {
                        MacdLine = macd.MacdLine,
                        SignalLine = macd.SignalLine,
                        Histogram = macd.Histogram,
                        Signal = macd.Signal,
                        Confidence = macd.Confidence,
                        Description = macd.Description
                    },
                    BollingerBands = new
                    {
                        Upper = bollinger.Upper,
                        Middle = bollinger.Middle,
                        Lower = bollinger.Lower,
                        Position = bollinger.Position,
                        Signal = bollinger.Signal,
                        Confidence = bollinger.Confidence,
                        Description = bollinger.Description,
                        BandWidth = bollinger.BandWidth
                    },
                    MovingAverages = new
                    {
                        SMA20 = movingAvg.SMA20,
                        SMA50 = movingAvg.SMA50,
                        EMA12 = movingAvg.EMA12,
                        EMA26 = movingAvg.EMA26,
                        Signal = movingAvg.Signal,
                        Confidence = movingAvg.Confidence,
                        Description = movingAvg.Description
                    },
                    SupportResistance = new
                    {
                        Support = supportResistance.Support,
                        Resistance = supportResistance.Resistance,
                        DistanceToSupport = supportResistance.DistanceToSupport,
                        DistanceToResistance = supportResistance.DistanceToResistance,
                        Description = supportResistance.Description
                    },
                    Volatility = new
                    {
                        Value = volatility.Value,
                        Level = volatility.Level,
                        Description = volatility.Description
                    }
                },
                Patterns = new
                {
                    DetectedPatterns = patterns.DetectedPatterns.Select(p => new
                    {
                        p.PatternName,
                        p.Signal,
                        p.Confidence,
                        p.Description,
                        p.TargetPrice,
                        p.StopLoss
                    }),
                    PrimaryPattern = patterns.PrimaryPattern != null ? new
                    {
                        patterns.PrimaryPattern.PatternName,
                        patterns.PrimaryPattern.Signal,
                        patterns.PrimaryPattern.Confidence,
                        patterns.PrimaryPattern.Description,
                        patterns.PrimaryPattern.TargetPrice,
                        patterns.PrimaryPattern.StopLoss
                    } : null,
                    OverallSignal = patterns.OverallSignal,
                    Confidence = patterns.Confidence
                },
                Summary = GenerateSummary(rsi, macd, bollinger, movingAvg, patterns)
            };

            return SuccessResult(result, "تم جلب التحليل الفني الشامل");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في التحليل الفني الشامل");
            return Error("حدث خطأ في جلب التحليل", 500);
        }
    }

    /// <summary>
    /// جلب RSI فقط
    /// </summary>
    [HttpGet("rsi")]
    public async Task<IActionResult> GetRSI([FromQuery] int period = 14, [FromQuery] int hours = 24)
    {
        try
        {
            var prices = await GetClosePrices(hours);
            if (prices == null) return Error("بيانات غير كافية", 400);

            var rsi = TechnicalIndicators.CalculateRSI(prices, period);

            return SuccessResult(new
            {
                Value = rsi.Value,
                Signal = rsi.Signal,
                Confidence = rsi.Confidence,
                Description = rsi.Description,
                Period = period
            }, "تم حساب RSI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب RSI");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// جلب MACD فقط
    /// </summary>
    [HttpGet("macd")]
    public async Task<IActionResult> GetMACD([FromQuery] int hours = 72)
    {
        try
        {
            var prices = await GetClosePrices(hours);
            if (prices == null) return Error("بيانات غير كافية", 400);

            var macd = TechnicalIndicators.CalculateMACD(prices);

            return SuccessResult(new
            {
                MacdLine = macd.MacdLine,
                SignalLine = macd.SignalLine,
                Histogram = macd.Histogram,
                Signal = macd.Signal,
                Confidence = macd.Confidence,
                Description = macd.Description
            }, "تم حساب MACD");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب MACD");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// جلب Bollinger Bands
    /// </summary>
    [HttpGet("bollinger")]
    public async Task<IActionResult> GetBollingerBands([FromQuery] int period = 20, [FromQuery] int hours = 48)
    {
        try
        {
            var prices = await GetClosePrices(hours);
            if (prices == null) return Error("بيانات غير كافية", 400);

            var bollinger = TechnicalIndicators.CalculateBollingerBands(prices, period);

            return SuccessResult(new
            {
                Upper = bollinger.Upper,
                Middle = bollinger.Middle,
                Lower = bollinger.Lower,
                Position = bollinger.Position,
                Signal = bollinger.Signal,
                Confidence = bollinger.Confidence,
                Description = bollinger.Description,
                BandWidth = bollinger.BandWidth,
                Period = period
            }, "تم حساب Bollinger Bands");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب Bollinger Bands");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// جلب مستويات الدعم والمقاومة
    /// </summary>
    [HttpGet("support-resistance")]
    public async Task<IActionResult> GetSupportResistance([FromQuery] int lookback = 30)
    {
        try
        {
            var prices = await GetClosePrices(lookback);
            if (prices == null) return Error("بيانات غير كافية", 400);

            var sr = TechnicalIndicators.FindSupportResistance(prices, lookback);

            return SuccessResult(new
            {
                Support = sr.Support,
                Resistance = sr.Resistance,
                DistanceToSupport = sr.DistanceToSupport,
                DistanceToResistance = sr.DistanceToResistance,
                Description = sr.Description
            }, "تم حساب الدعم والمقاومة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب الدعم والمقاومة");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// كشف الأنماط السعرية
    /// </summary>
    [HttpGet("patterns")]
    public async Task<IActionResult> GetPatterns([FromQuery] int hours = 72)
    {
        try
        {
            var historicalPrices = await _priceRepo.GetLastHoursAsync(hours);
            var priceList = historicalPrices.OrderBy(p => p.Timestamp).ToList();

            if (priceList.Count < 15)
            {
                return Error("بيانات غير كافية لكشف الأنماط", 400);
            }

            var closePrices = priceList.Select(p => p.Close).ToList();
            var highPrices = priceList.Select(p => p.High).ToList();
            var lowPrices = priceList.Select(p => p.Low).ToList();

            var patterns = PatternRecognition.AnalyzePatterns(closePrices, highPrices, lowPrices);

            return SuccessResult(new
            {
                DetectedPatterns = patterns.DetectedPatterns.Select(p => new
                {
                    p.PatternName,
                    p.Signal,
                    p.Confidence,
                    p.Description,
                    p.TargetPrice,
                    p.StopLoss
                }),
                PrimaryPattern = patterns.PrimaryPattern != null ? new
                {
                    patterns.PrimaryPattern.PatternName,
                    patterns.PrimaryPattern.Signal,
                    patterns.PrimaryPattern.Confidence,
                    patterns.PrimaryPattern.Description,
                    patterns.PrimaryPattern.TargetPrice,
                    patterns.PrimaryPattern.StopLoss
                } : null,
                OverallSignal = patterns.OverallSignal,
                Confidence = patterns.Confidence,
                TotalPatternsDetected = patterns.DetectedPatterns.Count
            }, "تم كشف الأنماط");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في كشف الأنماط");
            return Error("حدث خطأ", 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // المساعدات
    // ═══════════════════════════════════════════════════════════════════

    private async Task<List<decimal>?> GetClosePrices(int hours)
    {
        var historicalPrices = await _priceRepo.GetLastHoursAsync(hours);
        var priceList = historicalPrices.OrderBy(p => p.Timestamp).ToList();

        if (priceList.Count < 10) return null;

        return priceList.Select(p => p.Close).ToList();
    }

    private object GenerateSummary(
        TechnicalResult<double> rsi,
        MacdResult macd,
        BollingerBandsResult bollinger,
        MovingAverageResult movingAvg,
        PatternAnalysisResult patterns)
    {
        // حساب الإشارات
        var bullishSignals = 0;
        var bearishSignals = 0;

        if (rsi.Signal.Contains("شراء") || rsi.Signal.Contains("ذروة بيع")) bullishSignals++;
        else if (rsi.Signal.Contains("بيع") || rsi.Signal.Contains("ذروة شراء")) bearishSignals++;

        if (macd.Signal.Contains("شراء")) bullishSignals++;
        else if (macd.Signal.Contains("بيع")) bearishSignals++;

        if (bollinger.Signal.Contains("ذروة بيع")) bullishSignals++;
        else if (bollinger.Signal.Contains("ذروة شراء")) bearishSignals++;

        if (movingAvg.Signal.Contains("شراء")) bullishSignals++;
        else if (movingAvg.Signal.Contains("بيع")) bearishSignals++;

        if (patterns.OverallSignal.Contains("شراء")) bullishSignals++;
        else if (patterns.OverallSignal.Contains("بيع")) bearishSignals++;

        string overallTrend;
        int confidence;

        if (bullishSignals > bearishSignals)
        {
            overallTrend = "صاعد";
            confidence = (bullishSignals * 100) / (bullishSignals + bearishSignals);
        }
        else if (bearishSignals > bullishSignals)
        {
            overallTrend = "هابط";
            confidence = (bearishSignals * 100) / (bullishSignals + bearishSignals);
        }
        else
        {
            overallTrend = "محايد";
            confidence = 50;
        }

        return new
        {
            OverallTrend = overallTrend,
            Confidence = confidence,
            BullishSignals = bullishSignals,
            BearishSignals = bearishSignals,
            Recommendation = bullishSignals > bearishSignals + 1 ? "شراء" :
                           bearishSignals > bullishSignals + 1 ? "بيع" : "انتظار"
        };
    }

    /// <summary>
    /// مقارنة التحليل المحلي مع تحليل Gemini
    /// </summary>
    [HttpPost("compare-with-gemini")]
    public async Task<IActionResult> CompareWithGemini([FromBody] GeminiAnalysisRequest geminiAnalysis)
    {
        try
        {
            // 1. جلب التحليل المحلي
            var historicalPrices = await _priceRepo.GetLastHoursAsync(72);
            var priceList = historicalPrices.OrderBy(p => p.Timestamp).ToList();

            if (priceList.Count < 20)
            {
                return Error("بيانات غير كافية للمقارنة", 400);
            }

            var closePrices = priceList.Select(p => p.Close).ToList();
            var highPrices = priceList.Select(p => p.High).ToList();
            var lowPrices = priceList.Select(p => p.Low).ToList();
            var currentPrice = priceList.Last();

            // حساب المؤشرات المحلية
            var localRsi = TechnicalIndicators.CalculateRSI(closePrices, 14);
            var localMacd = TechnicalIndicators.CalculateMACD(closePrices);
            var localBollinger = TechnicalIndicators.CalculateBollingerBands(closePrices, 20);
            var localMovingAvg = TechnicalIndicators.AnalyzeMovingAverages(closePrices);
            var localSR = TechnicalIndicators.FindSupportResistance(closePrices, 30);
            var localVolatility = TechnicalIndicators.CalculateVolatility(closePrices, 20);
            var localPatterns = PatternRecognition.AnalyzePatterns(closePrices, highPrices, lowPrices);

            // 2. بناء نتيجة المقارنة
            var comparison = new
            {
                Timestamp = DateTime.UtcNow,
                CurrentPrice = new
                {
                    Close = currentPrice.Close,
                    Timestamp = currentPrice.Timestamp
                },

                // المقارنة التفصيلية
                Comparison = new
                {
                    RSI = CompareRSI(localRsi, geminiAnalysis),
                    MACD = CompareMACD(localMacd, geminiAnalysis),
                    BollingerBands = CompareBollinger(localBollinger, geminiAnalysis),
                    MovingAverages = CompareMovingAverages(localMovingAvg, geminiAnalysis),
                    SupportResistance = CompareSupportResistance(localSR, geminiAnalysis),
                    Volatility = CompareVolatility(localVolatility, geminiAnalysis),
                    OverallRecommendation = CompareRecommendations(
                        localRsi, localMacd, localBollinger, localMovingAvg, geminiAnalysis)
                },

                // التحليل المحلي الكامل
                LocalAnalysis = new
                {
                    RSI = new { localRsi.Value, localRsi.Signal, localRsi.Confidence },
                    MACD = new { localMacd.Signal, localMacd.Confidence, localMacd.MacdLine },
                    Bollinger = new { localBollinger.Position, localBollinger.Signal, localBollinger.Confidence },
                    MovingAvg = new { localMovingAvg.Signal, localMovingAvg.Confidence },
                    SupportResistance = new { localSR.Support, localSR.Resistance },
                    Volatility = new { localVolatility.Level, localVolatility.Value },
                    Patterns = new
                    {
                        PrimaryPattern = localPatterns.PrimaryPattern?.PatternName,
                        Signal = localPatterns.PrimaryPattern?.Signal,
                        AllPatterns = localPatterns.DetectedPatterns.Count
                    }
                },

                // تحليل Gemini
                GeminiAnalysis = geminiAnalysis,

                // الملخص النهائي
                Summary = GenerateComparisonSummary(
                    localRsi, localMacd, localBollinger, localMovingAvg, localPatterns, geminiAnalysis)
            };

            return SuccessResult(comparison, "تمت المقارنة بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في المقارنة مع Gemini");
            return Error("حدث خطأ في المقارنة", 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // دوال المقارنة المساعدة
    // ═══════════════════════════════════════════════════════════════════

    private object CompareRSI(TechnicalResult<double> local, GeminiAnalysisRequest gemini)
    {
        var geminiRsi = gemini.TechnicalIndicators?.RsiValue ?? 50;
        var difference = Math.Abs(local.Value - geminiRsi);
        var agreement = difference < 10 ? "متطابق" : difference < 20 ? "متقارب" : "مختلف";

        return new
        {
            LocalValue = local.Value,
            GeminiValue = geminiRsi,
            Difference = difference,
            Agreement = agreement,
            LocalSignal = local.Signal,
            GeminiSignal = gemini.TechnicalIndicators?.RsiStatus ?? "غير محدد",
            Winner = DetermineWinner(local.Confidence, gemini.Recommendation?.Confidence ?? 50)
        };
    }

    private object CompareMACD(MacdResult local, GeminiAnalysisRequest gemini)
    {
        var geminiSignal = gemini.TechnicalIndicators?.MacdSignal ?? "محايد";
        var signalsMatch = NormalizeSignal(local.Signal) == NormalizeSignal(geminiSignal);

        return new
        {
            LocalSignal = local.Signal,
            GeminiSignal = geminiSignal,
            Agreement = signalsMatch ? "متطابق" : "مختلف",
            LocalConfidence = local.Confidence,
            Winner = DetermineWinner(local.Confidence, gemini.Recommendation?.Confidence ?? 50)
        };
    }

    private object CompareBollinger(BollingerBandsResult local, GeminiAnalysisRequest gemini)
    {
        var geminiPosition = gemini.TechnicalIndicators?.BollingerPosition ?? "غير محدد";
        var positionsMatch = local.Position.Contains(geminiPosition) || geminiPosition.Contains(local.Position);

        return new
        {
            LocalPosition = local.Position,
            GeminiPosition = geminiPosition,
            Agreement = positionsMatch ? "متطابق" : "مختلف",
            LocalBandWidth = local.BandWidth,
            LocalSignal = local.Signal
        };
    }

    private object CompareMovingAverages(MovingAverageResult local, GeminiAnalysisRequest gemini)
    {
        var geminiSignal = gemini.TechnicalIndicators?.MovingAverages ?? "محايد";
        var signalsMatch = NormalizeSignal(local.Signal) == NormalizeSignal(geminiSignal);

        return new
        {
            LocalSignal = local.Signal,
            GeminiSignal = geminiSignal,
            Agreement = signalsMatch ? "متطابق" : "مختلف",
            LocalSMA20 = local.SMA20,
            LocalSMA50 = local.SMA50
        };
    }

    private object CompareSupportResistance(SupportResistanceResult local, GeminiAnalysisRequest gemini)
    {
        var geminiSupport = gemini.TechnicalIndicators?.SupportLevel ?? 0;
        var geminiResistance = gemini.TechnicalIndicators?.ResistanceLevel ?? 0;

        var supportDiff = geminiSupport > 0 ? Math.Abs(local.Support - geminiSupport) : 0;
        var resistanceDiff = geminiResistance > 0 ? Math.Abs(local.Resistance - geminiResistance) : 0;

        return new
        {
            Local = new { Support = local.Support, Resistance = local.Resistance },
            Gemini = new { Support = geminiSupport, Resistance = geminiResistance },
            Difference = new { Support = supportDiff, Resistance = resistanceDiff },
            Agreement = (supportDiff < 50 && resistanceDiff < 50) ? "متقارب" : "مختلف"
        };
    }

    private object CompareVolatility(VolatilityResult local, GeminiAnalysisRequest gemini)
    {
        return new
        {
            LocalLevel = local.Level,
            LocalValue = local.Value,
            Description = local.Description
        };
    }

    private object CompareRecommendations(
        TechnicalResult<double> rsi,
        MacdResult macd,
        BollingerBandsResult bollinger,
        MovingAverageResult movingAvg,
        GeminiAnalysisRequest gemini)
    {
        // التوصية المحلية
        var localSignals = new List<string>
        {
            MapSignalToBuySellHold(rsi.Signal),
            MapSignalToBuySellHold(macd.Signal),
            MapSignalToBuySellHold(bollinger.Signal),
            MapSignalToBuySellHold(movingAvg.Signal)
        };

        var buyCount = localSignals.Count(s => s == "buy");
        var sellCount = localSignals.Count(s => s == "sell");

        string localRecommendation = buyCount > sellCount ? "شراء" :
                                     sellCount > buyCount ? "بيع" : "انتظار";

        // التوصية من Gemini
        var geminiRecommendation = gemini.Recommendation?.Action ?? "غير محدد";

        var agree = NormalizeSignal(localRecommendation) == NormalizeSignal(geminiRecommendation);

        return new
        {
            LocalRecommendation = localRecommendation,
            LocalConfidence = (buyCount + sellCount) * 20,
            GeminiRecommendation = geminiRecommendation,
            GeminiConfidence = gemini.Recommendation?.Confidence ?? 0,
            Agreement = agree ? "✅ متطابق" : "⚠️ مختلف",
            Explanation = agree
                ? "كلا التحليلين يعطيان نفس التوصية"
                : $"التحليل المحلي يوصي بـ {localRecommendation} بينما Gemini يوصي بـ {geminiRecommendation}"
        };
    }

    private object GenerateComparisonSummary(
        TechnicalResult<double> rsi,
        MacdResult macd,
        BollingerBandsResult bollinger,
        MovingAverageResult movingAvg,
        PatternAnalysisResult patterns,
        GeminiAnalysisRequest gemini)
    {
        var agreements = 0;
        var total = 0;

        // حساب التطابقات
        if (gemini.TechnicalIndicators?.RsiValue != null)
        {
            total++;
            if (Math.Abs(rsi.Value - gemini.TechnicalIndicators.RsiValue.Value) < 15)
                agreements++;
        }

        if (gemini.TechnicalIndicators?.MacdSignal != null)
        {
            total++;
            if (NormalizeSignal(macd.Signal) == NormalizeSignal(gemini.TechnicalIndicators.MacdSignal))
                agreements++;
        }

        if (gemini.TechnicalIndicators?.MovingAverages != null)
        {
            total++;
            if (NormalizeSignal(movingAvg.Signal) == NormalizeSignal(gemini.TechnicalIndicators.MovingAverages))
                agreements++;
        }

        var agreementPercentage = total > 0 ? (agreements * 100) / total : 0;

        return new
        {
            AgreementPercentage = agreementPercentage,
            TotalIndicatorsCompared = total,
            MatchingIndicators = agreements,
            OverallAssessment = agreementPercentage >= 75 ? "تطابق عالي ✅" :
                               agreementPercentage >= 50 ? "تطابق متوسط ⚠️" : "تباين ملحوظ ❌",
            Recommendation = agreementPercentage >= 75
                ? "كلا التحليلين متوافقان، يمكن الثقة بالتوصية"
                : agreementPercentage >= 50
                    ? "توافق معتدل، يُنصح بالحذر والمتابعة"
                    : "تباين كبير، يُفضّل انتظار إشارات أوضح",
            LocalStrength = new
            {
                RSI = rsi.Confidence,
                MACD = macd.Confidence,
                Bollinger = bollinger.Confidence,
                MovingAvg = movingAvg.Confidence,
                Patterns = patterns.Confidence
            },
            GeminiStrength = new
            {
                Overall = gemini.Recommendation?.Confidence ?? 0
            }
        };
    }

    private string MapSignalToBuySellHold(string signal)
    {
        signal = signal.ToLower();
        if (signal.Contains("شراء") || signal.Contains("buy") || signal.Contains("صاعد") || signal.Contains("ذروة بيع"))
            return "buy";
        if (signal.Contains("بيع") || signal.Contains("sell") || signal.Contains("هابط") || signal.Contains("ذروة شراء"))
            return "sell";
        return "hold";
    }

    private string NormalizeSignal(string signal)
    {
        signal = signal.ToLower();
        if (signal.Contains("شراء") || signal.Contains("buy") || signal.Contains("صاعد"))
            return "buy";
        if (signal.Contains("بيع") || signal.Contains("sell") || signal.Contains("هابط"))
            return "sell";
        return "hold";
    }

    private string DetermineWinner(int localConfidence, int geminiConfidence)
    {
        if (Math.Abs(localConfidence - geminiConfidence) < 10)
            return "متساوي";
        return localConfidence > geminiConfidence ? "المحلي" : "Gemini";
    }
}

// ═══════════════════════════════════════════════════════════════════
// نماذج الطلبات
// ═══════════════════════════════════════════════════════════════════

public class GeminiAnalysisRequest
{
    public TechnicalIndicatorsDto? TechnicalIndicators { get; set; }
    public GeminiRecommendationDto? Recommendation { get; set; }
    public string? TrendAnalysis { get; set; }
}

public class TechnicalIndicatorsDto
{
    public string? RsiStatus { get; set; }
    public double? RsiValue { get; set; }
    public string? MacdSignal { get; set; }
    public string? MovingAverages { get; set; }
    public double? SupportLevel { get; set; }
    public double? ResistanceLevel { get; set; }
    public string? BollingerPosition { get; set; }
}

public class GeminiRecommendationDto
{
    public string? Action { get; set; }
    public int Confidence { get; set; }
    public string? Reasoning { get; set; }
}
