using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Infrastructure.ExternalApis;
using MrGoldenBader.Infrastructure.Services;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// تحكم الأخبار - يستخدم مصادر متعددة مع Cache في SQL Server
/// ═══════════════════════════════════════════════════════════════════
/// جميع الأخبار تُخزن في CachedNews (SQL Server) - بدون MongoDB
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NewsController : BaseApiController
{
    private readonly MultiSourceNewsService _newsService;
    private readonly NewsIntegrationService _newsIntegration;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        MultiSourceNewsService newsService,
        NewsIntegrationService newsIntegration,
        ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _newsIntegration = newsIntegration;
        _logger = logger;
    }

    /// <summary>
    /// تحديث الأخبار من المصادر المتعددة
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> RefreshNews()
    {
        try
        {
            _logger.LogInformation("جاري تحديث الأخبار من المصادر المتعددة...");
            var news = await _newsService.GetNewsAsync(20, forceRefresh: true);
            _logger.LogInformation("تم جلب {Count} خبر", news.Count);
            
            return Success(new { 
                Count = news.Count, 
                Message = $"تم جلب {news.Count} خبر من المصادر المتعددة",
                Sources = news.GroupBy(n => n.ApiSource).Select(g => new { Source = g.Key, Count = g.Count() }),
                News = news.Take(5).Select(n => new { n.Title, n.SourceName, n.PublishedAt })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تحديث الأخبار");
            return Error("حدث خطأ في تحديث الأخبار", 500);
        }
    }

    /// <summary>
    /// جلب الأخبار من الـ Cache (SQL Server)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnalyzedNewsDto>>> GetNews([FromQuery] int count = 20)
    {
        try
        {
            var news = await _newsService.GetNewsAsync(count);
            IEnumerable<AnalyzedNewsDto> dtos = news.Select(MapCachedToAnalyzedDto).ToList();

            return Success(dtos, "تم جلب الأخبار بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأخبار");
            return Error("حدث خطأ في جلب الأخبار", 500);
        }
    }

    /// <summary>
    /// جلب ملخص تأثير الأخبار
    /// </summary>
    [HttpGet("impact")]
    public async Task<ActionResult<object>> GetNewsImpact()
    {
        try
        {
            var summary = await _newsIntegration.GetNewsImpactSummaryAsync();
            
            // تحويل الأخبار عالية التأثير إلى DTOs
            var highImpactDtos = summary.HighImpactNewsCached
                .Select(MapCachedToAnalyzedDto)
                .ToList();
            
            var result = new
            {
                TotalNews = summary.TotalNews,
                BullishCount = summary.BullishCount,
                BearishCount = summary.BearishCount,
                NeutralCount = summary.NeutralCount,
                AverageImpact = summary.AverageImpact,
                OverallScore = summary.OverallScore,
                OverallDirection = summary.OverallDirection,
                CurrentPrice = summary.CurrentPrice,
                PriceChange = summary.PriceChange,
                PriceChangePercent = summary.PriceChangePercent,
                HighImpactNews = highImpactDtos,
                AnalyzedAt = summary.AnalyzedAt
            };

            return Success(result, "ملخص تأثير الأخبار");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب ملخص التأثير");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// جلب الأخبار المحللة
    /// </summary>
    [HttpGet("analyzed")]
    public async Task<ActionResult<IEnumerable<AnalyzedNewsDto>>> GetAnalyzedNews([FromQuery] int limit = 20)
    {
        try
        {
            var news = await _newsService.GetNewsAsync(limit);
            var dtos = news.Select(MapCachedToAnalyzedDto);

            return Success(dtos, "تم جلب الأخبار المحللة بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأخبار المحللة");
            return Error("حدث خطأ في جلب الأخبار", 500);
        }
    }

    /// <summary>
    /// جلب الأخبار عالية التأثير
    /// </summary>
    [HttpGet("high-impact")]
    public async Task<ActionResult<IEnumerable<AnalyzedNewsDto>>> GetHighImpactNews(
        [FromQuery] double minScore = 70,
        [FromQuery] int limit = 10)
    {
        try
        {
            var allNews = await _newsService.GetNewsAsync(50);
            var highImpact = allNews
                .Where(n => (double)n.GoldImpactScore >= minScore)
                .Take(limit)
                .ToList();
            
            var dtos = highImpact.Select(MapCachedToAnalyzedDto);

            return Success(dtos, "تم جلب الأخبار عالية التأثير");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأخبار عالية التأثير");
            return Error("حدث خطأ في جلب الأخبار", 500);
        }
    }

    /// <summary>
    /// البحث في الأخبار
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AnalyzedNewsDto>>> SearchNews(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Error("يرجى إدخال كلمة البحث", 400);
            }

            var allNews = await _newsService.GetNewsAsync(100);
            var filtered = allNews
                .Where(n => n.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                           (n.Summary?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(limit)
                .ToList();
            
            var dtos = filtered.Select(MapCachedToAnalyzedDto);

            return Success(dtos, $"تم العثور على {filtered.Count} نتيجة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في البحث عن الأخبار");
            return Error("حدث خطأ في البحث", 500);
        }
    }

    /// <summary>
    /// جلب خبر محدد
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnalyzedNewsDto>> GetNewsById(Guid id)
    {
        try
        {
            var allNews = await _newsService.GetNewsAsync(100);
            var item = allNews.FirstOrDefault(n => n.Id == id);

            if (item == null)
            {
                return NotFound("الخبر غير موجود");
            }

            return Success(MapCachedToAnalyzedDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الخبر");
            return Error("حدث خطأ في جلب الخبر", 500);
        }
    }

    /// <summary>
    /// جلب أخبار اليوم
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<AnalyzedNewsDto>>> GetTodayNews()
    {
        try
        {
            var allNews = await _newsService.GetNewsAsync(50);
            var today = DateTime.UtcNow.Date;
            var todayNews = allNews.Where(n => n.PublishedAt.Date == today);
            var dtos = todayNews.Select(MapCachedToAnalyzedDto);

            return Success(dtos, "أخبار اليوم");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب أخبار اليوم");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// تحويل خبر مخزن مؤقتاً (SQL Server) إلى DTO
    /// </summary>
    private static AnalyzedNewsDto MapCachedToAnalyzedDto(CachedNews news)
    {
        return new AnalyzedNewsDto
        {
            Id = news.Id.ToString(),
            Title = news.Title,
            SmartSummary = news.Summary,
            Source = news.SourceName,
            SourceDomain = news.SourceDomain,
            PublishedAt = news.PublishedAt,
            
            BannerImage = news.ImageUrl ?? "",
            Url = news.ArticleUrl ?? "",
            Authors = news.Authors,
            Topics = news.Keywords.Select(k => new TopicDto { Name = k, RelevanceScore = 50 }).ToList(),
            
            Sentiment = news.Sentiment,
            SentimentScore = (double)news.SentimentScore,
            SentimentLabel = news.Sentiment == "bullish" ? "Bullish" : news.Sentiment == "bearish" ? "Bearish" : "Neutral",
            
            GoldImpactDirection = news.GoldImpactDirection,
            ImpactType = news.GoldImpactDirection,
            ImpactScore = (double)news.GoldImpactScore,
            ConfidenceScore = Math.Abs((double)news.SentimentScore) * 100,
            ImpactExplanation = news.ImpactExplanation ?? "",
            GoldRelevanceScore = (double)news.GoldImpactScore,
            
            Category = news.Category,
            Keywords = news.Keywords,
            
            Language = news.Language,
            ModelUsed = news.ApiSource,
            AnalyzedAt = news.FetchedAt
        };
    }
}
