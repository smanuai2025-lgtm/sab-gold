namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان سعر الذهب العالمي - بيانات الأونصة (OHLC)
/// يُستخدم لتخزين الأسعار التاريخية والتحليل الفني
/// </summary>
public class GoldPrice : BaseEntity
{
    /// <summary>
    /// الطابع الزمني للسعر
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// سعر الافتتاح بالدولار
    /// </summary>
    public decimal Open { get; set; }
    
    /// <summary>
    /// أعلى سعر بالدولار
    /// </summary>
    public decimal High { get; set; }
    
    /// <summary>
    /// أدنى سعر بالدولار
    /// </summary>
    public decimal Low { get; set; }
    
    /// <summary>
    /// سعر الإغلاق بالدولار
    /// </summary>
    public decimal Close { get; set; }
    
    /// <summary>
    /// حجم التداول
    /// </summary>
    public decimal? Volume { get; set; }
    
    /// <summary>
    /// الفترة الزمنية: Minute, Hour, Day
    /// </summary>
    public string TimeFrame { get; set; } = "Hour";
    
    /// <summary>
    /// مصدر البيانات
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
