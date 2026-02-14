using Hangfire;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.Interfaces;

namespace MrGoldenBader.Infrastructure.Jobs;

/// <summary>
/// مهام Hangfire لجلب الأسعار بشكل دوري
/// </summary>
public class PriceUpdateJobs
{
    private readonly IGoldPriceService _goldPriceService;
    private readonly ILogger<PriceUpdateJobs> _logger;

    public PriceUpdateJobs(
        IGoldPriceService goldPriceService,
        ILogger<PriceUpdateJobs> logger)
    {
        _goldPriceService = goldPriceService;
        _logger = logger;
    }

    /// <summary>
    /// تحديث الأسعار - يعمل كل دقيقة
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task UpdatePricesAsync()
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("بدء مهمة تحديث الأسعار");
        _logger.LogInformation("═══════════════════════════════════════");
        
        try
        {
            await _goldPriceService.RefreshPricesAsync();
            _logger.LogInformation("تم تحديث الأسعار بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في تحديث الأسعار");
            throw; // إعادة الخطأ لـ Hangfire للمحاولة مرة أخرى
        }
    }

    /// <summary>
    /// تسجيل مهام Hangfire المتكررة
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        // تحديث الأسعار كل دقيقة
        RecurringJob.AddOrUpdate<PriceUpdateJobs>(
            "update-gold-prices",
            job => job.UpdatePricesAsync(),
            "*/1 * * * *"); // كل دقيقة
        
        // يمكن إضافة مهام أخرى هنا
        // مثل: تنظيف البيانات القديمة، إرسال التقارير، إلخ
    }
}
