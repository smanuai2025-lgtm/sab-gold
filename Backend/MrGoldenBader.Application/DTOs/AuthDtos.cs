namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج تسجيل الدخول
/// </summary>
public class LoginDto
{
    /// <summary>
    /// اسم المستخدم
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// كلمة المرور
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// نموذج نتيجة المصادقة
/// </summary>
public class AuthResultDto
{
    /// <summary>
    /// هل نجحت المصادقة
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// الرسالة
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// رمز الوصول JWT
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// تاريخ انتهاء الرمز
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// بيانات المستخدم
    /// </summary>
    public UserDto? User { get; set; }
}

/// <summary>
/// نموذج بيانات المستخدم
/// </summary>
public class UserDto
{
    /// <summary>
    /// المعرف
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// اسم المستخدم
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// الاسم الكامل
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// البريد الإلكتروني
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// العملة المفضلة
    /// </summary>
    public string PreferredCurrency { get; set; } = string.Empty;
    
    /// <summary>
    /// حساسية التنبيهات
    /// </summary>
    public int AlertSensitivity { get; set; }
}
