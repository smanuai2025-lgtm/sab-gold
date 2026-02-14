namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان المستخدم - مخصص لمالك واحد للنظام
/// يحتوي على بيانات المصادقة والإعدادات الشخصية
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// اسم المستخدم للدخول
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// البريد الإلكتروني
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// كلمة المرور المشفرة
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// الاسم الكامل
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// هل الحساب نشط
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// تاريخ آخر دخول
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// حساسية التنبيهات: محافظ = 1، متوازن = 2، عدواني = 3
    /// </summary>
    public int AlertSensitivity { get; set; } = 2;
    
    /// <summary>
    /// العملة المفضلة للعرض: KWD أو USD
    /// </summary>
    public string PreferredCurrency { get; set; } = "KWD";
}
