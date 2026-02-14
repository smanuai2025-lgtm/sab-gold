using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.Data;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة دمج الأخبار مع الأسعار
/// ═══════════════════════════════════════════════════════════════════
/// المنطق الموحد:
/// - ربط كل خبر بحركة السعر الزمنية (قبل وبعد النشر)
/// - تصنيف التأثير بناءً على اتجاه السعر الفعلي وليس رأياً بشرياً
/// - استخدام SQL Server (CachedNews) فقط - بدون MongoDB
/// - التحليل محلي بدون AI خارجي
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
public class NewsIntegrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGoldPriceRepository _priceRepository;
    private readonly ILogger<NewsIntegrationService> _logger;

    public NewsIntegrationService(
        ApplicationDbContext dbContext,
        IGoldPriceRepository priceRepository,
        ILogger<NewsIntegrationService> logger)
    {
        _dbContext = dbContext;
        _priceRepository = priceRepository;
        _logger = logger;
    }

    /// <summary>
    /// تحليل خبر جديد وربطه بحركة السعر الفعلية
    /// ═══════════════════════════════════════════════════════════════════
    /// المنطق الموحد:
    /// 1. جلب سعر الذهب وقت نشر الخبر
    /// 2. جلب السعر الحالي
    /// 3. حساب التغير الفعلي
    /// 4. تصنيف الخبر بناءً على اتجاه السعر
    /// ═══════════════════════════════════════════════════════════════════
    /// </summary>
    public async Task<CachedNews?> AnalyzeNewsAsync(CachedNews news)
    {
        try
        {
            _logger.LogInformation("تحليل خبر: {Title}", news.Title.Substring(0, Math.Min(50, news.Title.Length)));

            // ═══════════════════════════════════════════════════════════════════
            // الخطوة 1: جلب حركة السعر المرتبطة بوقت الخبر
            // ═══════════════════════════════════════════════════════════════════
            var priceMovement = await GetPriceMovementForNewsAsync(news.PublishedAt);
            
            // ═══════════════════════════════════════════════════════════════════
            // الخطوة 2: تصنيف الخبر بناءً على حركة السعر الفعلية
            // ═══════════════════════════════════════════════════════════════════
            var impactClassification = ClassifyNewsByPriceMovement(news, priceMovement);

            // تحديث الخبر بالتحليل
            news.Sentiment = impactClassification.Sentiment;
            news.SentimentScore = (decimal)impactClassification.SentimentScore;
            news.GoldImpactDirection = impactClassification.Direction;
            news.GoldImpactScore = (decimal)impactClassification.ImpactScore;
            news.ImpactExplanation = impactClassification.Explanation;

            _logger.LogInformation("تم تحليل الخبر: التأثير {Direction} ({Score}%) - تغير السعر: {Change}%", 
                news.GoldImpactDirection, 
                news.GoldImpactScore,
                priceMovement.ChangePercent);

            return news;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تحليل الخبر");
            return null;
        }
    }

    /// <summary>
    /// جلب حركة السعر المرتبطة بوقت الخبر
    /// </summary>
    private async Task<PriceMovement> GetPriceMovementForNewsAsync(DateTime newsTime)
    {
        try
        {
            // جلب السعر الحالي
            var currentPrice = await _priceRepository.GetLatestAsync();
            
            // جلب آخر ساعتين من الأسعار
            var recentPrices = await _priceRepository.GetLastHoursAsync(2);
            
            if (currentPrice == null)
            {
                return new PriceMovement { PriceAtTime = 0, ChangeValue = 0, ChangePercent = 0 };
            }

            // البحث عن أقرب سعر لوقت نشر الخبر
            var priceAtNewsTime = recentPrices
                .OrderBy(p => Math.Abs((p.Timestamp - newsTime).TotalMinutes))
                .FirstOrDefault();

            var priceAtTime = priceAtNewsTime?.Close ?? currentPrice.Close;
            var changeValue = currentPrice.Close - priceAtTime;
            var changePercent = priceAtTime > 0 ? (changeValue / priceAtTime) * 100 : 0;

            return new PriceMovement
            {
                PriceAtTime = priceAtTime,
                CurrentPrice = currentPrice.Close,
                ChangeValue = changeValue,
                ChangePercent = Math.Round(changePercent, 2)
            };
        }
        catch
        {
            return new PriceMovement { PriceAtTime = 0, ChangeValue = 0, ChangePercent = 0 };
        }
    }

    /// <summary>
    /// تصنيف الخبر بناءً على حركة السعر الفعلية
    /// ═══════════════════════════════════════════════════════════════════
    /// المنطق الموحد (بدون تكرار):
    /// - إذا ارتفع السعر بعد الخبر → الخبر إيجابي (bullish)
    /// - إذا انخفض السعر بعد الخبر → الخبر سلبي (bearish)
    /// - إذا لم يتغير → محايد (neutral)
    /// ═══════════════════════════════════════════════════════════════════
    /// </summary>
    private ImpactClassification ClassifyNewsByPriceMovement(CachedNews news, PriceMovement priceMovement)
    {
        string direction;
        double impactScore;
        string explanation;
        string sentiment;
        double sentimentScore;

        // تصنيف بناءً على حركة السعر الفعلية
        if (priceMovement.ChangePercent > 0.1m) // ارتفاع أكثر من 0.1%
        {
            direction = "bullish";
            sentiment = "positive";
            sentimentScore = (double)priceMovement.ChangePercent / 5; // تحويل النسبة لدرجة
            impactScore = Math.Min(100, 50 + (double)Math.Abs(priceMovement.ChangePercent) * 20);
            explanation = $"السعر ارتفع {priceMovement.ChangePercent:F2}% بعد هذا الخبر";
        }
        else if (priceMovement.ChangePercent < -0.1m) // انخفاض أكثر من 0.1%
        {
            direction = "bearish";
            sentiment = "negative";
            sentimentScore = (double)priceMovement.ChangePercent / 5;
            impactScore = Math.Min(100, 50 + (double)Math.Abs(priceMovement.ChangePercent) * 20);
            explanation = $"السعر انخفض {Math.Abs(priceMovement.ChangePercent):F2}% بعد هذا الخبر";
        }
        else
        {
            direction = "neutral";
            sentiment = "neutral";
            sentimentScore = 0;
            impactScore = 30;
            explanation = "السعر مستقر - لا تأثير واضح";
        }

        // تعزيز التصنيف بكلمات مفتاحية (اختياري)
        var text = $"{news.Title} {news.Content}".ToLower();
        if (text.Contains("gold") || text.Contains("الذهب") || text.Contains("bullion"))
        {
            impactScore = Math.Min(100, impactScore + 10);
        }

        return new ImpactClassification
        {
            Direction = direction,
            Sentiment = sentiment,
            SentimentScore = sentimentScore,
            ImpactScore = impactScore,
            Explanation = explanation
        };
    }

    /// <summary>
    /// حساب التأثير الإجمالي للأخبار على الذهب
    /// ═══════════════════════════════════════════════════════════════════
    /// المنطق الموحد: بناءً على حركة السعر الفعلية وليس تصنيفات يدوية
    /// ═══════════════════════════════════════════════════════════════════
    /// </summary>
    public async Task<NewsImpactSummary> GetNewsImpactSummaryAsync()
    {
        try
        {
            // جلب الأخبار من CachedNews (SQL Server)
            var recentNews = await _dbContext.CachedNews
                .Where(n => n.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(n => n.PublishedAt)
                .Take(20)
                .ToListAsync();
                
            var currentPrice = await _priceRepository.GetLatestAsync();
            
            // جلب حركة السعر في آخر ساعة
            var priceHistory = await _priceRepository.GetLastHoursAsync(1);
            var priceMovement = CalculatePriceMovement(priceHistory, currentPrice?.Close ?? 0);

            if (!recentNews.Any())
            {
                return new NewsImpactSummary
                {
                    TotalNews = 0,
                    AverageImpact = 0,
                    OverallDirection = "neutral",
                    CurrentPrice = currentPrice?.Close ?? 0,
                    PriceChange = priceMovement.ChangeValue,
                    PriceChangePercent = priceMovement.ChangePercent,
                    AnalyzedAt = DateTime.UtcNow
                };
            }

            var bullishNews = recentNews.Count(n => n.GoldImpactDirection == "bullish");
            var bearishNews = recentNews.Count(n => n.GoldImpactDirection == "bearish");
            var avgImpact = recentNews.Average(n => (double)n.GoldImpactScore);

            // ═══════════════════════════════════════════════════════════════════
            // التصنيف الموحد: بناءً على حركة السعر الفعلية
            // ═══════════════════════════════════════════════════════════════════
            string overallDirection;
            if (priceMovement.ChangePercent > 0.1m)
                overallDirection = "bullish";
            else if (priceMovement.ChangePercent < -0.1m)
                overallDirection = "bearish";
            else
                overallDirection = "neutral";

            // تحويل CachedNews إلى AnalyzedNewsDto للتوافق
            var highImpactNews = recentNews
                .Where(n => n.GoldImpactScore >= 70)
                .Take(3)
                .ToList();

            return new NewsImpactSummary
            {
                TotalNews = recentNews.Count,
                BullishCount = bullishNews,
                BearishCount = bearishNews,
                NeutralCount = recentNews.Count - bullishNews - bearishNews,
                AverageImpact = Math.Round(avgImpact, 1),
                OverallScore = Math.Round(avgImpact, 0),
                OverallDirection = overallDirection,
                CurrentPrice = currentPrice?.Close ?? 0,
                PriceChange = priceMovement.ChangeValue,
                PriceChangePercent = priceMovement.ChangePercent,
                HighImpactNewsCached = highImpactNews,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب تأثير الأخبار");
            return new NewsImpactSummary { AnalyzedAt = DateTime.UtcNow };
        }
    }

    /// <summary>
    /// حساب حركة السعر من التاريخ
    /// </summary>
    private PriceMovement CalculatePriceMovement(IEnumerable<GoldPrice> priceHistory, decimal currentPrice)
    {
        var prices = priceHistory.ToList();
        if (!prices.Any() || currentPrice == 0)
        {
            return new PriceMovement { PriceAtTime = currentPrice, CurrentPrice = currentPrice };
        }

        var oldestPrice = prices.OrderBy(p => p.Timestamp).First();
        var changeValue = currentPrice - oldestPrice.Close;
        var changePercent = oldestPrice.Close > 0 ? (changeValue / oldestPrice.Close) * 100 : 0;

        return new PriceMovement
        {
            PriceAtTime = oldestPrice.Close,
            CurrentPrice = currentPrice,
            ChangeValue = changeValue,
            ChangePercent = Math.Round(changePercent, 2)
        };
    }
}

/// <summary>
/// حركة السعر المرتبطة بالخبر
/// </summary>
public class PriceMovement
{
    public decimal PriceAtTime { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ChangeValue { get; set; }
    public decimal ChangePercent { get; set; }
}

/// <summary>
/// تصنيف تأثير الخبر
/// </summary>
public class ImpactClassification
{
    public string Direction { get; set; } = "neutral";
    public string Sentiment { get; set; } = "neutral";
    public double SentimentScore { get; set; }
    public double ImpactScore { get; set; }
    public string Explanation { get; set; } = "";
}

/// <summary>
/// ملخص تأثير الأخبار
/// </summary>
public class NewsImpactSummary
{
    public int TotalNews { get; set; }
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public int NeutralCount { get; set; }
    public double AverageImpact { get; set; }
    public double OverallScore { get; set; }
    public string OverallDirection { get; set; } = "neutral";
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public List<CachedNews> HighImpactNewsCached { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}
