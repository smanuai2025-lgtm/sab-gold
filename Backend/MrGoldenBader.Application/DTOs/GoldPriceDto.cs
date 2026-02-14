namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات سعر الذهب العالمي
/// </summary>
public class GoldPriceDto
{
    /// <summary>
    /// الطابع الزمني
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// سعر الافتتاح
    /// </summary>
    public decimal Open { get; set; }
    
    /// <summary>
    /// أعلى سعر
    /// </summary>
    public decimal High { get; set; }
    
    /// <summary>
    /// أدنى سعر
    /// </summary>
    public decimal Low { get; set; }
    
    /// <summary>
    /// سعر الإغلاق / الحالي
    /// </summary>
    public decimal Close { get; set; }
    
    /// <summary>
    /// نسبة التغير
    /// </summary>
    public decimal ChangePercent { get; set; }
    
    /// <summary>
    /// قيمة التغير
    /// </summary>
    public decimal ChangeValue { get; set; }
    
    /// <summary>
    /// الفترة الزمنية
    /// </summary>
    public string TimeFrame { get; set; } = string.Empty;
}
