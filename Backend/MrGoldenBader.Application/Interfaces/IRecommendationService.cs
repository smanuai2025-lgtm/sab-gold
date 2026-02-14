using MrGoldenBader.Application.DTOs;

namespace MrGoldenBader.Application.Interfaces;

public interface IRecommendationService
{
    /// <summary>
    /// توليد توصية جديدة بناءً على التحليل الحالي
    /// </summary>
    Task<RecommendationDto?> GenerateRecommendationAsync();
    
    /// <summary>
    /// الحصول على التوصية النشطة حالياً
    /// </summary>
    Task<RecommendationDto?> GetActiveRecommendationAsync();
    
    /// <summary>
    /// الحصول على آخر التوصيات
    /// </summary>
    Task<IEnumerable<RecommendationDto>> GetLatestRecommendationsAsync(int count = 10);
    
    /// <summary>
    /// الحصول على نسبة نجاح التوصيات (0-100)
    /// </summary>
    Task<double> GetSuccessRateAsync();
    
    /// <summary>
    /// تحديث نتيجة التوصية (نجاح/فشل) بعد انتهائها
    /// </summary>
    Task UpdateRecommendationResultAsync(Guid id, bool wasSuccessful, decimal actualPrice);
}
