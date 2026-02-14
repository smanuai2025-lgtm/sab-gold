using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace MrGoldenBader.Infrastructure.Seed;

/// <summary>
/// خدمة زرع البيانات الأولية
/// تُنشئ المستخدم الافتراضي وبيانات تجريبية للتطوير
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// تنفيذ زرع البيانات
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // التأكد من إنشاء قاعدة البيانات
            await _context.Database.MigrateAsync();
            
            // زرع المستخدم الافتراضي
            await SeedDefaultUserAsync();
            
            _logger.LogInformation("تم زرع البيانات الأولية بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في زرع البيانات الأولية");
            throw;
        }
    }

    /// <summary>
    /// إنشاء المستخدم الافتراضي (المالك)
    /// </summary>
    private async Task SeedDefaultUserAsync()
    {
        // التحقق من وجود مستخدم
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("المستخدم الافتراضي موجود مسبقاً");
            return;
        }

        var defaultUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "owner",
            Email = "owner@mrgoldenbader.local",
            PasswordHash = HashPassword("GoldenBader2024!"),
            FullName = "المالك",
            IsActive = true,
            AlertSensitivity = 2, // متوازن
            PreferredCurrency = "KWD",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(defaultUser);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("تم إنشاء المستخدم الافتراضي: {Username}", defaultUser.Username);
    }

    /// <summary>
    /// تشفير كلمة المرور باستخدام SHA256
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
