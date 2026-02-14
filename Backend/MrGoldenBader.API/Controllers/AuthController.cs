using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// وحدة تحكم المصادقة
/// تسجيل الدخول وإدارة الرموز
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    /// <summary>
    /// تسجيل الدخول
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (string.IsNullOrEmpty(loginDto.Username) || string.IsNullOrEmpty(loginDto.Password))
            {
                return Error("اسم المستخدم وكلمة المرور مطلوبان");
            }
            
            var result = await _authService.LoginAsync(loginDto);
            
            if (!result.Success)
            {
                return Error(result.Message, 401);
            }
            
            _logger.LogInformation("تسجيل دخول ناجح للمستخدم: {Username}", loginDto.Username);
            return Success(result, "تم تسجيل الدخول بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تسجيل الدخول");
            return Error("حدث خطأ في تسجيل الدخول", 500);
        }
    }
    
    /// <summary>
    /// تجديد الرمز
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken([FromBody] string token)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(token);
            
            if (!result.Success)
            {
                return Error(result.Message, 401);
            }
            
            return Success(result, "تم تجديد الرمز بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تجديد الرمز");
            return Error("حدث خطأ في تجديد الرمز", 500);
        }
    }
}
