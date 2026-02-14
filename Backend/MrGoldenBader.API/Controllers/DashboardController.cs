using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.Services;
using MrGoldenBader.Infrastructure.ExternalApis;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// تحكم لوحة المعلومات - واجهة موحدة لجميع البيانات
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : BaseApiController
{
    private readonly IGoldPriceService _priceService;
    private readonly NewsIntegrationService _newsIntegration;
    private readonly MultiSourceNewsService _newsService;
    private readonly IRecommendationRepository _recommendationRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IGoldPriceService priceService,
        NewsIntegrationService newsIntegration,
        MultiSourceNewsService newsService,
        IRecommendationRepository recommendationRepo,
        IAlertRepository alertRepo,
        ILogger<DashboardController> logger)
    {
        _priceService = priceService;
        _newsIntegration = newsIntegration;
        _newsService = newsService;
        _recommendationRepo = recommendationRepo;
        _alertRepo = alertRepo;
        _logger = logger;
    }

    /// <summary>
    /// جلب لوحة المعلومات الكاملة
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        try
        {
            _logger.LogInformation("جلب بيانات لوحة المعلومات");

            // جلب البيانات بالتتابع (لتجنب مشكلة DbContext threading)
            var globalPrice = await _priceService.GetCurrentGlobalPriceAsync();
            var kuwaitPrices = await _priceService.GetCurrentKuwaitPricesAsync();
            var unreadAlerts = await _alertRepo.GetUnreadCountAsync();
            var activeRec = await _recommendationRepo.GetActiveAsync();
            
            // جلب ملخص الأخبار (قد يفشل إذا لم تكن خدمة AI تعمل)
            NewsImpactSummary? newsImpact = null;
            try
            {
                newsImpact = await _newsIntegration.GetNewsImpactSummaryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "فشل جلب ملخص الأخبار");
            }
            
            var dashboard = new DashboardDto
            {
                GlobalPrice = globalPrice,
                KuwaitPrices = kuwaitPrices.ToList(),
                NewsImpact = newsImpact != null ? new NewsImpactSummaryDto
                {
                    TotalNews = newsImpact.TotalNews,
                    BullishCount = newsImpact.BullishCount,
                    BearishCount = newsImpact.BearishCount,
                    NeutralCount = newsImpact.NeutralCount,
                    AverageImpact = newsImpact.AverageImpact,
                    OverallScore = newsImpact.OverallScore,
                    OverallDirection = newsImpact.OverallDirection,
                    CurrentPrice = newsImpact.CurrentPrice,
                    PriceChange = newsImpact.PriceChange,
                    PriceChangePercent = newsImpact.PriceChangePercent,
                    HighImpactNews = newsImpact.HighImpactNewsCached.Select(n => new AnalyzedNewsDto
                    {
                        Id = n.Id.ToString(),
                        Title = n.Title,
                        SmartSummary = n.Summary,
                        ImpactType = n.GoldImpactDirection,
                        ImpactScore = (double)n.GoldImpactScore,
                        ImpactExplanation = n.ImpactExplanation ?? "",
                        Sentiment = n.Sentiment,
                        GoldImpactDirection = n.GoldImpactDirection,
                        PriceChangePercent = 0
                    }).ToList(),
                    AnalyzedAt = newsImpact.AnalyzedAt
                } : null,
                Recommendation = activeRec != null ? new RecommendationDto
                {
                    Id = activeRec.Id,
                    Type = activeRec.Type.ToString(),
                    TargetPrice = activeRec.TargetPrice,
                    StopLoss = activeRec.StopLoss,
                    ConfidenceScore = activeRec.ConfidenceScore,
                    Reason = activeRec.Reason,
                    CreatedAt = activeRec.CreatedAt,
                    ExpiresAt = activeRec.ExpiresAt
                } : null,
                UnreadAlerts = unreadAlerts,
                MarketStatus = IsMarketOpen() ? "مفتوح" : "مغلق",
                LastUpdate = DateTime.UtcNow
            };

            return Success(dashboard, "تم جلب لوحة المعلومات");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب لوحة المعلومات");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }

    /// <summary>
    /// جلب ملخص تأثير الأخبار
    /// </summary>
    [HttpGet("news-impact")]
    public async Task<ActionResult<NewsImpactSummaryDto>> GetNewsImpact()
    {
        try
        {
            var impact = await _newsIntegration.GetNewsImpactSummaryAsync();
            
            var dto = new NewsImpactSummaryDto
            {
                TotalNews = impact.TotalNews,
                BullishCount = impact.BullishCount,
                BearishCount = impact.BearishCount,
                NeutralCount = impact.NeutralCount,
                AverageImpact = impact.AverageImpact,
                OverallDirection = impact.OverallDirection,
                CurrentPrice = impact.CurrentPrice,
                HighImpactNews = impact.HighImpactNewsCached.Select(n => new AnalyzedNewsDto
                {
                    Id = n.Id.ToString(),
                    Title = n.Title,
                    SmartSummary = n.Summary,
                    ImpactType = n.GoldImpactDirection,
                    ImpactScore = (double)n.GoldImpactScore,
                    ImpactExplanation = n.ImpactExplanation ?? "",
                    Sentiment = n.Sentiment
                }).ToList(),
                AnalyzedAt = impact.AnalyzedAt
            };

            return Success(dto, "ملخص تأثير الأخبار");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب ملخص التأثير");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// تحديث الأسعار وتحليل الأخبار
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> RefreshData()
    {
        try
        {
            _logger.LogInformation("تحديث البيانات...");

            // تحديث الأسعار
            await _priceService.RefreshPricesAsync();

            // جلب أخبار جديدة
            await _newsService.GetNewsAsync(20, forceRefresh: true);

            return Success(new { Message = "تم التحديث بنجاح", Timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في التحديث");
            return Error("حدث خطأ في التحديث", 500);
        }
    }

    /// <summary>
    /// حالة السوق
    /// </summary>
    [HttpGet("market-status")]
    public ActionResult<object> GetMarketStatus()
    {
        var isOpen = IsMarketOpen();
        var now = DateTime.UtcNow;

        return Success(new
        {
            IsOpen = isOpen,
            Status = isOpen ? "مفتوح" : "مغلق",
            CurrentTimeUtc = now,
            LocalTime = now.AddHours(3), // توقيت الكويت
            NextOpen = isOpen ? (DateTime?)null : GetNextMarketOpen(),
            Session = GetCurrentSession()
        });
    }

    /// <summary>
    /// فحص إذا كان السوق مفتوحاً
    /// الذهب يتداول 24/5 (من الأحد للجمعة)
    /// </summary>
    private bool IsMarketOpen()
    {
        var now = DateTime.UtcNow;
        var day = now.DayOfWeek;
        
        // السوق مغلق السبت وجزء من الأحد/الجمعة
        if (day == DayOfWeek.Saturday)
            return false;
        if (day == DayOfWeek.Sunday && now.Hour < 22) // يفتح الأحد 22:00 UTC
            return false;
        if (day == DayOfWeek.Friday && now.Hour >= 22) // يغلق الجمعة 22:00 UTC
            return false;
            
        return true;
    }

    /// <summary>
    /// تحديد موعد الافتتاح القادم
    /// </summary>
    private DateTime GetNextMarketOpen()
    {
        var now = DateTime.UtcNow;
        var nextSunday = now.AddDays((7 - (int)now.DayOfWeek) % 7);
        return new DateTime(nextSunday.Year, nextSunday.Month, nextSunday.Day, 22, 0, 0);
    }

    /// <summary>
    /// تحديد الجلسة الحالية
    /// </summary>
    private string GetCurrentSession()
    {
        var hour = DateTime.UtcNow.Hour;
        
        if (hour >= 0 && hour < 8)
            return "جلسة آسيا";
        if (hour >= 8 && hour < 13)
            return "جلسة أوروبا";
        if (hour >= 13 && hour < 21)
            return "جلسة أمريكا";
            
        return "جلسة متداخلة";
    }
}
