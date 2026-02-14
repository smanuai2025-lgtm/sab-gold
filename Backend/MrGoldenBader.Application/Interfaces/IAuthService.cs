using MrGoldenBader.Application.DTOs;

namespace MrGoldenBader.Application.Interfaces;

/// <summary>
/// واجهة خدمة المصادقة
/// مسؤولة عن تسجيل الدخول وإدارة الرموز
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// تسجيل الدخول
    /// </summary>
    Task<AuthResultDto> LoginAsync(LoginDto loginDto);
    
    /// <summary>
    /// التحقق من صلاحية الرمز
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// تجديد الرمز
    /// </summary>
    Task<AuthResultDto> RefreshTokenAsync(string token);
    
    /// <summary>
    /// جلب بيانات المستخدم الحالي
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync(Guid userId);
    
    /// <summary>
    /// تحديث إعدادات المستخدم
    /// </summary>
    Task<bool> UpdateUserSettingsAsync(Guid userId, int alertSensitivity, string preferredCurrency);
}
