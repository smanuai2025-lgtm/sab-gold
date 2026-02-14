namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان التوصية - قرارات الشراء والبيع والانتظار
/// يحتوي على السبب ودرجة الثقة والأفق الزمني
/// </summary>
public class Recommendation : BaseEntity
{
    /// <summary>
    /// نوع التوصية: Buy, Hold, Sell
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// السبب بالعربية
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// درجة الثقة من 0 إلى 100
    /// </summary>
    public decimal ConfidenceScore { get; set; }
    
    /// <summary>
    /// السعر عند إصدار التوصية
    /// </summary>
    public decimal PriceAtRecommendation { get; set; }
    
    /// <summary>
    /// السعر المستهدف (إن وجد)
    /// </summary>
    public decimal? TargetPrice { get; set; }

    /// <summary>
    /// وقف الخسارة (إن وجد)
    /// </summary>
    public decimal? StopLoss { get; set; }
    
    /// <summary>
    /// الأفق الزمني: ساعات، أيام، أسبوع
    /// </summary>
    public string TimeHorizon { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ انتهاء صلاحية التوصية
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// هل التوصية نشطة
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// السعر الفعلي عند انتهاء التوصية (للتقييم)
    /// </summary>
    public decimal? ActualResult { get; set; }
    
    /// <summary>
    /// هل كانت التوصية ناجحة
    /// </summary>
    public bool? WasSuccessful { get; set; }
}
