namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان سعر الذهب في الكويت - لجميع العيارات
/// يشمل أسعار البيع والشراء بالدينار الكويتي
/// </summary>
public class KuwaitGoldPrice : BaseEntity
{
    /// <summary>
    /// الطابع الزمني للسعر
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// العيار: 24, 22, 21, 18
    /// </summary>
    public int Karat { get; set; }
    
    /// <summary>
    /// سعر الشراء بالدينار الكويتي للجرام
    /// </summary>
    public decimal BuyPrice { get; set; }
    
    /// <summary>
    /// سعر البيع بالدينار الكويتي للجرام
    /// </summary>
    public decimal SellPrice { get; set; }
    
    /// <summary>
    /// نسبة التغير عن اليوم السابق
    /// </summary>
    public decimal? ChangePercent { get; set; }
    
    /// <summary>
    /// قيمة التغير بالدينار
    /// </summary>
    public decimal? ChangeValue { get; set; }
    
    /// <summary>
    /// مصدر البيانات
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
