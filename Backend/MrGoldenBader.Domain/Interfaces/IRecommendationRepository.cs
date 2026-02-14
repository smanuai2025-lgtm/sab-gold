using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

/// <summary>
/// واجهة مستودع التوصيات
/// </summary>
public interface IRecommendationRepository : IRepository<Recommendation>
{
    /// <summary>
    /// جلب التوصية النشطة الحالية
    /// </summary>
    Task<Recommendation?> GetActiveAsync();
    
    /// <summary>
    /// جلب توصيات فترة زمنية
    /// </summary>
    Task<IEnumerable<Recommendation>> GetByDateRangeAsync(DateTime from, DateTime to);
    
    /// <summary>
    /// جلب التوصيات الناجحة
    /// </summary>
    Task<IEnumerable<Recommendation>> GetSuccessfulAsync();
    
    /// <summary>
    /// حساب نسبة نجاح التوصيات
    /// </summary>
    Task<decimal> GetSuccessRateAsync();
}
