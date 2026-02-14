using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// تطبيق خدمة المصادقة
/// مسؤولة عن تسجيل الدخول وإدارة الرموز
/// </summary>
public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;
    
    public AuthService(IRepository<User> userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }
    
    /// <summary>
    /// تسجيل الدخول
    /// </summary>
    public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            // البحث عن المستخدم
            var user = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);
                
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "اسم المستخدم أو كلمة المرور غير صحيحة"
                };
            }
            
            // التحقق من كلمة المرور
            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "اسم المستخدم أو كلمة المرور غير صحيحة"
                };
            }
            
            // تحديث آخر وقت دخول
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            
            // إنشاء الرمز
            var token = GenerateJwtToken(user);
            var expirationHours = _configuration.GetValue<int>("Jwt:ExpirationInHours", 24);
            
            return new AuthResultDto
            {
                Success = true,
                Message = "تم تسجيل الدخول بنجاح",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = $"حدث خطأ أثناء تسجيل الدخول: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// التحقق من صلاحية الرمز
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            
            return validatedToken != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// تجديد الرمز
    /// </summary>
    public async Task<AuthResultDto> RefreshTokenAsync(string token)
    {
        try
        {
            var isValid = await ValidateTokenAsync(token);
            if (!isValid)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "الرمز غير صالح أو منتهي الصلاحية"
                };
            }
            
            // استخراج معرف المستخدم من الرمز
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "الرمز غير صالح"
                };
            }
            
            // جلب المستخدم
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "المستخدم غير موجود أو غير نشط"
                };
            }
            
            // إنشاء رمز جديد
            var newToken = GenerateJwtToken(user);
            var expirationHours = _configuration.GetValue<int>("Jwt:ExpirationInHours", 24);
            
            return new AuthResultDto
            {
                Success = true,
                Message = "تم تجديد الرمز بنجاح",
                Token = newToken,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = $"حدث خطأ أثناء تجديد الرمز: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// جلب بيانات المستخدم الحالي
    /// </summary>
    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }
    
    /// <summary>
    /// تحديث إعدادات المستخدم
    /// </summary>
    public async Task<bool> UpdateUserSettingsAsync(Guid userId, int alertSensitivity, string preferredCurrency)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;
            
            user.AlertSensitivity = alertSensitivity;
            user.PreferredCurrency = preferredCurrency;
            
            await _userRepository.UpdateAsync(user);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    #region Private Methods
    
    /// <summary>
    /// إنشاء رمز JWT
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName)
            }),
            Expires = DateTime.UtcNow.AddHours(_configuration.GetValue<int>("Jwt:ExpirationInHours", 24)),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    /// <summary>
    /// التحقق من كلمة المرور
    /// ملاحظة: في الإنتاج يجب استخدام BCrypt أو مكتبة تشفير قوية
    /// </summary>
    private bool VerifyPassword(string password, string passwordHash)
    {
        // التحقق البسيط - في الإنتاج يجب استخدام BCrypt.Net-Next
        // return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        
        // للتطوير: مقارنة بسيطة (غير آمن للإنتاج)
        return HashPassword(password) == passwordHash;
    }
    
    /// <summary>
    /// تشفير كلمة المرور
    /// ملاحظة: في الإنتاج يجب استخدام BCrypt أو مكتبة تشفير قوية
    /// </summary>
    private string HashPassword(string password)
    {
        // في الإنتاج استخدم: BCrypt.Net.BCrypt.HashPassword(password)
        
        // للتطوير: SHA256 (غير موصى به للإنتاج)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    
    /// <summary>
    /// تحويل User إلى UserDto
    /// </summary>
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PreferredCurrency = user.PreferredCurrency,
            AlertSensitivity = user.AlertSensitivity
        };
    }
    
    #endregion
}
