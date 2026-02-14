namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان التنبيه - إشعارات الأسعار والأخبار والتوصيات
/// </summary>
public class Alert : BaseEntity
{
    /// <summary>
    /// نوع التنبيه: Price, News, Recommendation
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// العنوان
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// المحتوى
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// مستوى الأهمية: Low, Medium, High, Critical
    /// </summary>
    public string Severity { get; set; } = "Medium";
    
    /// <summary>
    /// هل تم قراءة التنبيه
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// هل تم إرسال الإشعار
    /// </summary>
    public bool IsSent { get; set; } = false;
    
    /// <summary>
    /// تاريخ الإرسال
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// معرف الكيان المرتبط (سعر، خبر، توصية)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    
    /// <summary>
    /// تاريخ القراءة
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
