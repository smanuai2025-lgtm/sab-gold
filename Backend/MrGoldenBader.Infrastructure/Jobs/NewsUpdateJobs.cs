using Hangfire;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Infrastructure.ExternalApis;

namespace MrGoldenBader.Infrastructure.Jobs;

/// <summary>
/// مهام Hangfire لجلب وتحليل الأخبار
/// يستخدم MultiSourceNewsService لجلب الأخبار من مصادر متعددة
/// </summary>
public class NewsUpdateJobs
{
    private readonly MultiSourceNewsService _newsService;
    private readonly ILogger<NewsUpdateJobs> _logger;

    public NewsUpdateJobs(
        MultiSourceNewsService newsService,
        ILogger<NewsUpdateJobs> logger)
    {
        _newsService = newsService;
        _logger = logger;
    }

    /// <summary>
    /// جلب الأخبار الجديدة - يعمل كل 30 دقيقة
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task FetchNewsAsync()
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("بدء مهمة جلب الأخبار من المصادر المتعددة");
        _logger.LogInformation("═══════════════════════════════════════");

        try
        {
            // جلب الأخبار مع إجبار التحديث
            var news = await _newsService.GetNewsAsync(20, forceRefresh: true);
            _logger.LogInformation("تم جلب {Count} خبر جديد", news.Count);
            
            // عرض المصادر
            var sourceGroups = news.GroupBy(n => n.ApiSource);
            foreach (var group in sourceGroups)
            {
                _logger.LogInformation("  - {Source}: {Count} خبر", group.Key, group.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في جلب الأخبار");
            throw;
        }
    }

    /// <summary>
    /// تسجيل مهمة جلب الأخبار المتكررة
    /// </summary>
    public static void RegisterNewsJobs()
    {
        // جلب الأخبار كل 30 دقيقة
        RecurringJob.AddOrUpdate<NewsUpdateJobs>(
            "fetch-news",
            job => job.FetchNewsAsync(),
            "*/30 * * * *"); // كل 30 دقيقة
    }
}
