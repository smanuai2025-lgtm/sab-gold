namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات التنبيه
/// </summary>
public class AlertDto
{
    /// <summary>
    /// المعرف
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// النوع
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// العنوان
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// الرسالة
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// مستوى الأهمية
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// هل مقروء
    /// </summary>
    public bool IsRead { get; set; }
    
    /// <summary>
    /// تاريخ الإنشاء
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
