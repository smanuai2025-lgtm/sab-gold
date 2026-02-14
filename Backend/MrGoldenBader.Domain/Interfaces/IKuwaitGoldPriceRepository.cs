using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

/// <summary>
/// واجهة مستودع أسعار الذهب في الكويت
/// </summary>
public interface IKuwaitGoldPriceRepository : IRepository<KuwaitGoldPrice>
{
    /// <summary>
    /// جلب آخر أسعار لجميع العيارات
    /// </summary>
    Task<IEnumerable<KuwaitGoldPrice>> GetLatestAllKaratsAsync();
    
    /// <summary>
    /// جلب آخر سعر لعيار محدد
    /// </summary>
    Task<KuwaitGoldPrice?> GetLatestByKaratAsync(int karat);
    
    /// <summary>
    /// جلب أسعار فترة زمنية لعيار محدد
    /// </summary>
    Task<IEnumerable<KuwaitGoldPrice>> GetByDateRangeAndKaratAsync(DateTime from, DateTime to, int karat);
}
