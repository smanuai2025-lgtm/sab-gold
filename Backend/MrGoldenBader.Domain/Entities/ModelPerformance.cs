namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان أداء النماذج - لتتبع دقة التنبؤات
/// </summary>
public class ModelPerformance : BaseEntity
{
    /// <summary>
    /// اسم النموذج
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ التقييم
    /// </summary>
    public DateTime EvaluationDate { get; set; }
    
    /// <summary>
    /// عدد التنبؤات الكلي
    /// </summary>
    public int TotalPredictions { get; set; }
    
    /// <summary>
    /// عدد التنبؤات الصحيحة
    /// </summary>
    public int CorrectPredictions { get; set; }
    
    /// <summary>
    /// نسبة الدقة
    /// </summary>
    public decimal AccuracyRate { get; set; }
    
    /// <summary>
    /// متوسط الخطأ المطلق
    /// </summary>
    public decimal? MeanAbsoluteError { get; set; }
    
    /// <summary>
    /// ملاحظات إضافية
    /// </summary>
    public string? Notes { get; set; }
}
