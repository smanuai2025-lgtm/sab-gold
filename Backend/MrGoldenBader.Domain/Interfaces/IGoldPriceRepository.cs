using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

/// <summary>
/// واجهة مستودع أسعار الذهب العالمية
/// </summary>
public interface IGoldPriceRepository : IRepository<GoldPrice>
{
    /// <summary>
    /// جلب آخر سعر
    /// </summary>
    Task<GoldPrice?> GetLatestAsync();
    
    /// <summary>
    /// جلب أسعار فترة زمنية محددة
    /// </summary>
    Task<IEnumerable<GoldPrice>> GetByDateRangeAsync(DateTime from, DateTime to, string timeFrame = "Hour");
    
    /// <summary>
    /// جلب أسعار آخر X ساعات
    /// </summary>
    Task<IEnumerable<GoldPrice>> GetLastHoursAsync(int hours);
    
    /// <summary>
    /// جلب أسعار آخر X أيام
    /// </summary>
    Task<IEnumerable<GoldPrice>> GetLastDaysAsync(int days);
}
