using MrGoldenBader.Application.DTOs;

namespace MrGoldenBader.Application.Interfaces;

/// <summary>
/// واجهة خدمة أسعار الذهب
/// مسؤولة عن جلب وتحديث أسعار الذهب العالمية والمحلية
/// </summary>
public interface IGoldPriceService
{
    /// <summary>
    /// جلب السعر العالمي الحالي للأونصة
    /// </summary>
    Task<GoldPriceDto?> GetCurrentGlobalPriceAsync();
    
    /// <summary>
    /// جلب الأسعار العالمية التاريخية
    /// </summary>
    Task<IEnumerable<GoldPriceDto>> GetGlobalPriceHistoryAsync(int hours = 24);
    
    /// <summary>
    /// جلب أسعار الكويت الحالية لجميع العيارات
    /// </summary>
    Task<IEnumerable<KuwaitGoldPriceDto>> GetCurrentKuwaitPricesAsync();
    
    /// <summary>
    /// جلب سعر الكويت لعيار محدد
    /// </summary>
    Task<KuwaitGoldPriceDto?> GetKuwaitPriceByKaratAsync(int karat);
    
    /// <summary>
    /// تحديث الأسعار من المصادر الخارجية
    /// </summary>
    Task RefreshPricesAsync();
}
