namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// الكيان الأساسي - جميع الكيانات ترث منه
/// يحتوي على المعرف وتاريخ الإنشاء والتعديل
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// المعرف الفريد
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// تاريخ الإنشاء
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// تاريخ آخر تعديل
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
