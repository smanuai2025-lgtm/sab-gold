using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة توليد التوصيات الذكية المتقدمة
/// تدمج بين المؤشرات الفنية، الأنماط السعرية، وتأثير الأخبار
/// نظام تحليل محلي احترافي بدون الحاجة لـ AI خارجي
/// </summary>
public class RecommendationEngine : IRecommendationService
{
    private readonly IRecommendationRepository _recommendationRepo;
    private readonly IGoldPriceRepository _priceRepo;
    private readonly NotificationService _notificationService;
    private readonly NewsIntegrationService _newsIntegration;
    private readonly ILogger<RecommendationEngine> _logger;

    public RecommendationEngine(
        IRecommendationRepository recommendationRepo,
        IGoldPriceRepository priceRepo,
        NewsIntegrationService newsIntegration,
        NotificationService notificationService,
        ILogger<RecommendationEngine> logger)
    {
        _recommendationRepo = recommendationRepo;
        _priceRepo = priceRepo;
        _newsIntegration = newsIntegration;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// توليد توصية جديدة بناءً على التحليل الشامل المتقدم
    /// </summary>
    public async Task<RecommendationDto?> GenerateRecommendationAsync()
    {
        try
        {
            _logger.LogInformation("═══════════════════════════════════════");
            _logger.LogInformation("بدء توليد توصية متقدمة (تحليل فني شامل)");
            _logger.LogInformation("═══════════════════════════════════════");

            // 1. جلب السعر الحالي
            var currentPrice = await _priceRepo.GetLatestAsync();
            if (currentPrice == null)
            {
                _logger.LogWarning("لا يوجد سعر حالي");
                return null;
            }

            // 2. جلب الأسعار التاريخية (24 ساعة للمؤشرات القصيرة + 50 نقطة للمتوسطات)
            var historicalPrices = await _priceRepo.GetLastHoursAsync(72);
            var priceList = historicalPrices.OrderBy(p => p.Timestamp).ToList();

            if (priceList.Count < 20)
            {
                _logger.LogWarning("بيانات غير كافية للتحليل المتقدم");
                return await GenerateBasicRecommendationAsync(currentPrice);
            }

            // 3. استخراج البيانات
            var closePrices = priceList.Select(p => p.Close).ToList();
            var highPrices = priceList.Select(p => p.High).ToList();
            var lowPrices = priceList.Select(p => p.Low).ToList();

            // 4. التحليل الفني الشامل
            var analysis = PerformAdvancedAnalysis(closePrices, highPrices, lowPrices);

            // 5. تأثير الأخبار
            var newsImpact = await _newsIntegration.GetNewsImpactSummaryAsync();
            var newsScore = CalculateNewsScore(newsImpact);

            // 6. توليد التوصية
            var recommendation = GenerateAdvancedRecommendation(
                currentPrice, analysis, newsScore, newsImpact);

            // 7. حفظ التوصية
            await _recommendationRepo.AddAsync(recommendation);
            await _recommendationRepo.SaveChangesAsync();

            _logger.LogInformation("✅ تم توليد التوصية: {Type} (ثقة: {Confidence}%)",
                recommendation.Type, recommendation.ConfidenceScore);

            // 8. إرسال تنبيه إذا كانت قوية
            if (recommendation.Type != "hold" && recommendation.ConfidenceScore >= 70)
            {
                await _notificationService.NotifyRecommendationAsync(
                    recommendation.Type, recommendation.TimeHorizon);
            }

            return MapToDto(recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في توليد التوصية");
            return null;
        }
    }

    /// <summary>
    /// التحليل الفني المتقدم الشامل
    /// </summary>
    private AdvancedAnalysisResult PerformAdvancedAnalysis(
        List<decimal> closePrices,
        List<decimal> highPrices,
        List<decimal> lowPrices)
    {
        _logger.LogInformation("📊 بدء التحليل الفني المتقدم...");

        // 1. المؤشرات الفنية
        var rsi = TechnicalIndicators.CalculateRSI(closePrices, 14);
        var macd = TechnicalIndicators.CalculateMACD(closePrices);
        var bollinger = TechnicalIndicators.CalculateBollingerBands(closePrices, 20);
        var movingAvg = TechnicalIndicators.AnalyzeMovingAverages(closePrices);
        var supportResistance = TechnicalIndicators.FindSupportResistance(closePrices, 30);
        var volatility = TechnicalIndicators.CalculateVolatility(closePrices, 20);

        _logger.LogInformation("  RSI: {RSI:F1} ({Signal})", rsi.Value, rsi.Signal);
        _logger.LogInformation("  MACD: {Signal}", macd.Signal);
        _logger.LogInformation("  Bollinger: {Position}", bollinger.Position);
        _logger.LogInformation("  Moving Avg: {Signal}", movingAvg.Signal);

        // 2. الأنماط السعرية
        var patterns = PatternRecognition.AnalyzePatterns(closePrices, highPrices, lowPrices);

        if (patterns.PrimaryPattern != null)
        {
            _logger.LogInformation("  🔍 نمط مكتشف: {Pattern} - {Signal}",
                patterns.PrimaryPattern.PatternName,
                patterns.PrimaryPattern.Signal);
        }

        return new AdvancedAnalysisResult
        {
            RSI = rsi,
            MACD = macd,
            BollingerBands = bollinger,
            MovingAverages = movingAvg,
            SupportResistance = supportResistance,
            Volatility = volatility,
            Patterns = patterns
        };
    }

    /// <summary>
    /// حساب تأثير الأخبار على شكل درجة
    /// </summary>
    private double CalculateNewsScore(NewsImpactSummary newsImpact)
    {
        if (newsImpact.TotalNews == 0) return 0;

        var bullishRatio = (double)newsImpact.BullishCount / newsImpact.TotalNews;
        var bearishRatio = (double)newsImpact.BearishCount / newsImpact.TotalNews;

        var netSentiment = bullishRatio - bearishRatio;
        var score = netSentiment * newsImpact.AverageImpact;

        return Math.Clamp(score, -100, 100);
    }

    /// <summary>
    /// توليد التوصية المتقدمة بناءً على جميع التحليلات
    /// </summary>
    private Recommendation GenerateAdvancedRecommendation(
        GoldPrice currentPrice,
        AdvancedAnalysisResult analysis,
        double newsScore,
        NewsImpactSummary newsImpact)
    {
        _logger.LogInformation("🧠 بدء تجميع الإشارات...");

        // نظام التصويت (Voting System)
        var signals = new List<(string signal, int weight, string source)>();

        // 1. إشارات المؤشرات الفنية
        signals.Add((MapSignalToBuySellHold(analysis.RSI.Signal), analysis.RSI.Confidence, "RSI"));
        signals.Add((MapSignalToBuySellHold(analysis.MACD.Signal), analysis.MACD.Confidence, "MACD"));
        signals.Add((MapSignalToBuySellHold(analysis.BollingerBands.Signal), analysis.BollingerBands.Confidence, "Bollinger"));
        signals.Add((MapSignalToBuySellHold(analysis.MovingAverages.Signal), analysis.MovingAverages.Confidence, "MA"));

        // 2. إشارة الأنماط (وزن أعلى)
        if (analysis.Patterns.PrimaryPattern != null)
        {
            signals.Add((
                MapSignalToBuySellHold(analysis.Patterns.PrimaryPattern.Signal),
                analysis.Patterns.PrimaryPattern.Confidence,
                $"Pattern:{analysis.Patterns.PrimaryPattern.PatternName}"
            ));
        }

        // 3. إشارة الأخبار
        if (Math.Abs(newsScore) > 10)
        {
            var newsSignal = newsScore > 0 ? "buy" : "sell";
            var newsConfidence = (int)Math.Min(80, Math.Abs(newsScore));
            signals.Add((newsSignal, newsConfidence, "News"));
        }

        // 4. حساب الإشارة النهائية
        var buyScore = signals.Where(s => s.signal == "buy").Sum(s => s.weight);
        var sellScore = signals.Where(s => s.signal == "sell").Sum(s => s.weight);
        var holdScore = signals.Where(s => s.signal == "hold").Sum(s => s.weight);

        _logger.LogInformation("  📊 النتائج: Buy={BuyScore} | Sell={SellScore} | Hold={HoldScore}",
            buyScore, sellScore, holdScore);

        // تحديد التوصية النهائية
        string finalType;
        decimal confidence;
        string reason;
        decimal? targetPrice = null;
        decimal? stopLoss = null;
        string timeHorizon;

        var totalScore = buyScore + sellScore + holdScore;
        var dominantScore = Math.Max(buyScore, Math.Max(sellScore, holdScore));

        if (buyScore > sellScore && buyScore > holdScore && buyScore >= 150)
        {
            finalType = "buy";
            confidence = Math.Min(95, (decimal)((buyScore / (double)totalScore) * 100));
            reason = BuildRecommendationReason(signals.Where(s => s.signal == "buy").ToList(), analysis, newsImpact, "شراء");
            targetPrice = Math.Round(currentPrice.Close * 1.03m, 2);
            stopLoss = Math.Round(currentPrice.Close * 0.98m, 2);
            timeHorizon = confidence >= 80 ? "24 ساعة" : "48 ساعة";
        }
        else if (sellScore > buyScore && sellScore > holdScore && sellScore >= 150)
        {
            finalType = "sell";
            confidence = Math.Min(95, (decimal)((sellScore / (double)totalScore) * 100));
            reason = BuildRecommendationReason(signals.Where(s => s.signal == "sell").ToList(), analysis, newsImpact, "بيع");
            targetPrice = Math.Round(currentPrice.Close * 0.97m, 2);
            stopLoss = Math.Round(currentPrice.Close * 1.02m, 2);
            timeHorizon = confidence >= 80 ? "24 ساعة" : "48 ساعة";
        }
        else
        {
            finalType = "hold";
            confidence = 60;
            reason = BuildHoldReason(analysis, newsImpact);
            timeHorizon = "مستمر";
        }

        // تعديل الثقة بناءً على التقلب
        if (analysis.Volatility.Level == "عالي جداً" && confidence > 70)
        {
            confidence = Math.Max(60, confidence - 10);
            reason += " ⚠️ تقلب عالٍ - احذر.";
        }

        _logger.LogInformation("✅ التوصية النهائية: {Type} (ثقة: {Confidence}%)", finalType, confidence);

        return new Recommendation
        {
            Type = finalType,
            Reason = reason,
            ConfidenceScore = confidence,
            PriceAtRecommendation = currentPrice.Close,
            TargetPrice = targetPrice,
            StopLoss = stopLoss,
            TimeHorizon = timeHorizon,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(48)
        };
    }

    /// <summary>
    /// تحويل الإشارة إلى buy/sell/hold
    /// </summary>
    private string MapSignalToBuySellHold(string signal)
    {
        signal = signal.ToLower();

        if (signal.Contains("شراء") || signal.Contains("buy") ||
            signal.Contains("صاعد") || signal.Contains("bullish") ||
            signal.Contains("ذروة بيع"))
            return "buy";

        if (signal.Contains("بيع") || signal.Contains("sell") ||
            signal.Contains("هابط") || signal.Contains("bearish") ||
            signal.Contains("ذروة شراء"))
            return "sell";

        return "hold";
    }

    /// <summary>
    /// بناء سبب التوصية
    /// </summary>
    private string BuildRecommendationReason(
        List<(string signal, int weight, string source)> supportingSignals,
        AdvancedAnalysisResult analysis,
        NewsImpactSummary newsImpact,
        string actionType)
    {
        var reasons = new List<string>();

        // أهم الإشارات
        var topSignals = supportingSignals.OrderByDescending(s => s.weight).Take(3);

        foreach (var sig in topSignals)
        {
            if (sig.source == "RSI")
                reasons.Add($"RSI {analysis.RSI.Signal} ({analysis.RSI.Value:F0})");
            else if (sig.source == "MACD")
                reasons.Add($"MACD {analysis.MACD.Signal}");
            else if (sig.source == "Bollinger")
                reasons.Add($"السعر {analysis.BollingerBands.Position}");
            else if (sig.source == "MA")
                reasons.Add($"المتوسطات {analysis.MovingAverages.Signal}");
            else if (sig.source.StartsWith("Pattern:"))
                reasons.Add($"نمط {sig.source.Replace("Pattern:", "")} مكتشف");
            else if (sig.source == "News" && newsImpact.TotalNews > 0)
                reasons.Add($"{newsImpact.BullishCount + newsImpact.BearishCount} خبر داعم");
        }

        var reasonText = reasons.Any()
            ? $"توصية {actionType}: " + string.Join(" • ", reasons)
            : $"مؤشرات متعددة تدعم {actionType}";

        // إضافة تحذيرات
        if (analysis.SupportResistance.DistanceToResistance < 1 && actionType == "شراء")
        {
            reasonText += $" (قرب المقاومة ${analysis.SupportResistance.Resistance:F2})";
        }
        else if (analysis.SupportResistance.DistanceToSupport < 1 && actionType == "بيع")
        {
            reasonText += $" (قرب الدعم ${analysis.SupportResistance.Support:F2})";
        }

        return reasonText;
    }

    /// <summary>
    /// بناء سبب الانتظار
    /// </summary>
    private string BuildHoldReason(AdvancedAnalysisResult analysis, NewsImpactSummary newsImpact)
    {
        var reasons = new List<string>();

        if (analysis.RSI.Signal.Contains("محايد"))
            reasons.Add("RSI متوازن");

        if (analysis.Patterns.PrimaryPattern?.PatternName == "تماسك")
            reasons.Add("السوق في حالة تماسك");

        if (newsImpact.TotalNews == 0)
            reasons.Add("لا توجد أخبار مؤثرة");

        if (analysis.Volatility.Level == "منخفض")
            reasons.Add("تقلب منخفض");

        return reasons.Any()
            ? "السوق في حالة ترقب: " + string.Join(" • ", reasons) + ". يُنصح بالانتظار."
            : "الإشارات متضاربة. يُنصح بالانتظار حتى يتضح الاتجاه.";
    }

    /// <summary>
    /// توليد توصية أساسية عند عدم توفر بيانات كافية
    /// </summary>
    private async Task<RecommendationDto?> GenerateBasicRecommendationAsync(GoldPrice currentPrice)
    {
        _logger.LogWarning("استخدام التحليل الأساسي - بيانات محدودة");

        var newsImpact = await _newsIntegration.GetNewsImpactSummaryAsync();
        var newsScore = CalculateNewsScore(newsImpact);

        string type;
        string reason;

        if (newsScore > 20)
        {
            type = "buy";
            reason = $"الأخبار إيجابية ({newsImpact.BullishCount} خبر داعم)";
        }
        else if (newsScore < -20)
        {
            type = "sell";
            reason = $"الأخبار سلبية ({newsImpact.BearishCount} خبر ضاغط)";
        }
        else
        {
            type = "hold";
            reason = "بيانات غير كافية للتوصية. يُنصح بالانتظار.";
        }

        var recommendation = new Recommendation
        {
            Type = type,
            Reason = reason,
            ConfidenceScore = 50,
            PriceAtRecommendation = currentPrice.Close,
            TimeHorizon = "مؤقت",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _recommendationRepo.AddAsync(recommendation);
        await _recommendationRepo.SaveChangesAsync();

        return MapToDto(recommendation);
    }

    // ═══════════════════════════════════════════════════════════════════
    // الوظائف المساعدة (IRecommendationService)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<RecommendationDto?> GetActiveRecommendationAsync()
    {
        var recommendation = await _recommendationRepo.GetActiveAsync();
        return recommendation != null ? MapToDto(recommendation) : null;
    }

    public async Task<IEnumerable<RecommendationDto>> GetLatestRecommendationsAsync(int count = 10)
    {
        var recommendations = await _recommendationRepo.GetByDateRangeAsync(
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        return recommendations
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .Select(MapToDto);
    }

    public async Task<double> GetSuccessRateAsync()
    {
        return (double)await _recommendationRepo.GetSuccessRateAsync();
    }

    public async Task UpdateRecommendationResultAsync(Guid id, bool wasSuccessful, decimal actualPrice)
    {
        var recommendation = await _recommendationRepo.GetByIdAsync(id);
        if (recommendation != null)
        {
            recommendation.WasSuccessful = wasSuccessful;
            recommendation.ActualResult = actualPrice;
            recommendation.IsActive = false;
            await _recommendationRepo.UpdateAsync(recommendation);
            await _recommendationRepo.SaveChangesAsync();
        }
    }

    private static RecommendationDto MapToDto(Recommendation r)
    {
        return new RecommendationDto
        {
            Id = r.Id,
            Type = r.Type,
            Reason = r.Reason,
            ConfidenceScore = r.ConfidenceScore,
            PriceAtRecommendation = r.PriceAtRecommendation,
            TargetPrice = r.TargetPrice,
            TimeHorizon = r.TimeHorizon,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            ExpiresAt = r.ExpiresAt
        };
    }
}

// ═══════════════════════════════════════════════════════════════════
// نماذج التحليل
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// نتيجة التحليل المتقدم الشامل
/// </summary>
public class AdvancedAnalysisResult
{
    public TechnicalResult<double> RSI { get; set; } = null!;
    public MacdResult MACD { get; set; } = null!;
    public BollingerBandsResult BollingerBands { get; set; } = null!;
    public MovingAverageResult MovingAverages { get; set; } = null!;
    public SupportResistanceResult SupportResistance { get; set; } = null!;
    public VolatilityResult Volatility { get; set; } = null!;
    public PatternAnalysisResult Patterns { get; set; } = null!;
}
