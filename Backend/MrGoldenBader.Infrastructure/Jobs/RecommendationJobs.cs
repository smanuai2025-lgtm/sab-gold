using Hangfire;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.Interfaces;

namespace MrGoldenBader.Infrastructure.Jobs;

/// <summary>
/// مهام Hangfire لتوليد التوصيات
/// </summary>
public class RecommendationJobs
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationJobs> _logger;

    public RecommendationJobs(
        IRecommendationService recommendationService,
        ILogger<RecommendationJobs> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// توليد توصية جديدة - يعمل كل 4 ساعات
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task GenerateRecommendationAsync()
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("بدء مهمة توليد التوصيات");
        _logger.LogInformation("═══════════════════════════════════════");

        try
        {
            var recommendation = await _recommendationService.GenerateRecommendationAsync();
            
            if (recommendation != null)
            {
                _logger.LogInformation("تم توليد توصية: {Type} - الثقة: {Confidence}%", 
                    recommendation.Type, recommendation.ConfidenceScore);
            }
            else
            {
                _logger.LogWarning("لم يتم توليد توصية");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في توليد التوصية");
            throw;
        }
    }

    /// <summary>
    /// تسجيل مهمة توليد التوصيات المتكررة
    /// </summary>
    public static void RegisterRecommendationJobs()
    {
        // توليد توصية كل 4 ساعات
        RecurringJob.AddOrUpdate<RecommendationJobs>(
            "generate-recommendation",
            job => job.GenerateRecommendationAsync(),
            "0 */4 * * *"); // كل 4 ساعات
    }
}
