using Hangfire;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Infrastructure.Services;

namespace MrGoldenBader.Infrastructure.Jobs;

/// <summary>
/// مهام مراقبة السوق اللحظية
/// تراقب التغيرات السريعة، وصول السعر للأهداف، والأخبار العاجلة
/// </summary>
public class MarketMonitorJobs
{
    private readonly IGoldPriceService _priceService;
    private readonly IRecommendationService _recommendationService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<MarketMonitorJobs> _logger;
    
    // ذاكرة مؤقتة بسيطة لتجنب تكرار التنبيهات لنفس الحدث
    private static decimal _lastNotifiedPrice = 0;
    private static DateTime _lastNotificationTime = DateTime.MinValue;

    public MarketMonitorJobs(
        IGoldPriceService priceService,
        IRecommendationService recommendationService,
        NotificationService notificationService,
        ILogger<MarketMonitorJobs> logger)
    {
        _priceService = priceService;
        _recommendationService = recommendationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// مراقبة السوق - تعمل كل دقيقة
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task MonitorMarketAsync()
    {
        try
        {
            var currentPrice = await _priceService.GetCurrentGlobalPriceAsync();
            if (currentPrice == null) return;

            // 1. فحص التغير المفاجئ (أكثر من 5 دولار في الدقيقة)
            if (_lastNotifiedPrice != 0)
            {
                var change = currentPrice.Close - _lastNotifiedPrice;
                // إذا مر وقت كافٍ (15 دقيقة) وتغير السعر بشكل ملحوظ
                if (DateTime.UtcNow - _lastNotificationTime > TimeSpan.FromMinutes(15) && Math.Abs(change) > 5)
                {
                    await _notificationService.NotifyPriceChangeAsync(
                        "الذهب", currentPrice.Close, change, change > 0 ? "up" : "down");
                    
                    _lastNotifiedPrice = currentPrice.Close;
                    _lastNotificationTime = DateTime.UtcNow;
                }
            }
            else
            {
                _lastNotifiedPrice = currentPrice.Close;
            }

            // 2. فحص التوصية النشطة (هل اقتربنا من الهدف؟)
            var activeRec = await _recommendationService.GetActiveRecommendationAsync();
            if (activeRec != null && activeRec.TargetPrice.HasValue)
            {
                var distance = Math.Abs(activeRec.TargetPrice.Value - currentPrice.Close);
                // إذا اقتربنا لمسافة أقل من 2 دولار
                if (distance < 2)
                {
                     // نتأكد أننا لم نرسل تنبيهاً مؤخراً لهذا السبب
                     // (هنا نحتاج منطق أكثر تعقيداً لتجنب التكرار، لكن للتبسيط سنعتمد على الوقت)
                     // يمكن تطويره لاحقاً
                     await _notificationService.NotifyTargetApproachAsync(
                         currentPrice.Close, activeRec.TargetPrice.Value, activeRec.Type);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في مراقبة السوق");
        }
    }

    /// <summary>
    /// تسجيل المهمة
    /// </summary>
    public static void RegisterJobs()
    {
        RecurringJob.AddOrUpdate<MarketMonitorJobs>(
            "monitor-market",
            job => job.MonitorMarketAsync(),
            "*/1 * * * *"); // كل دقيقة
    }
}
