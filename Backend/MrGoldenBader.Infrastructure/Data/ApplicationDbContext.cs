using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Infrastructure.Data;

/// <summary>
/// سياق قاعدة البيانات الرئيسي
/// يحتوي على جميع جداول SQL Server
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }
    
    /// <summary>
    /// جدول المستخدمين
    /// </summary>
    public DbSet<User> Users { get; set; }
    
    /// <summary>
    /// جدول أسعار الذهب العالمية
    /// </summary>
    public DbSet<GoldPrice> GoldPrices { get; set; }
    
    /// <summary>
    /// جدول أسعار الذهب في الكويت
    /// </summary>
    public DbSet<KuwaitGoldPrice> KuwaitGoldPrices { get; set; }
    
    /// <summary>
    /// جدول التوصيات
    /// </summary>
    public DbSet<Recommendation> Recommendations { get; set; }
    
    /// <summary>
    /// جدول التنبيهات
    /// </summary>
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<MarketReport> MarketReports { get; set; }
    
    /// <summary>
    /// جدول أداء النماذج
    /// </summary>
    public DbSet<ModelPerformance> ModelPerformances { get; set; }
    
    /// <summary>
    /// جدول الأخبار المخزنة مؤقتاً (Cache)
    /// </summary>
    public DbSet<CachedNews> CachedNews { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // إعدادات المستخدم
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
        });
        
        // إعدادات أسعار الذهب العالمية - فهارس زمنية للأداء
        modelBuilder.Entity<GoldPrice>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.Timestamp, e.TimeFrame });
            entity.Property(e => e.Open).HasPrecision(18, 4);
            entity.Property(e => e.High).HasPrecision(18, 4);
            entity.Property(e => e.Low).HasPrecision(18, 4);
            entity.Property(e => e.Close).HasPrecision(18, 4);
            entity.Property(e => e.Volume).HasPrecision(18, 4);
        });
        
        // إعدادات أسعار الكويت - فهارس للعيار والزمن
        modelBuilder.Entity<KuwaitGoldPrice>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.Karat, e.Timestamp });
            entity.Property(e => e.BuyPrice).HasPrecision(18, 4);
            entity.Property(e => e.SellPrice).HasPrecision(18, 4);
            entity.Property(e => e.ChangePercent).HasPrecision(10, 4);
            entity.Property(e => e.ChangeValue).HasPrecision(18, 4);
        });
        
        // إعدادات التوصيات
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 2);
            entity.Property(e => e.PriceAtRecommendation).HasPrecision(18, 4);
            entity.Property(e => e.TargetPrice).HasPrecision(18, 4);
            entity.Property(e => e.StopLoss).HasPrecision(18, 4);
            entity.Property(e => e.ActualResult).HasPrecision(18, 4);
        });
        
        // إعدادات التنبيهات
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
        });
        
        // إعدادات أداء النماذج
        modelBuilder.Entity<ModelPerformance>(entity =>
        {
            entity.HasIndex(e => e.EvaluationDate);
            entity.HasIndex(e => e.ModelName);
            entity.Property(e => e.AccuracyRate).HasPrecision(5, 2);
            entity.Property(e => e.MeanAbsoluteError).HasPrecision(18, 6);
        });
        
        // إعدادات الأخبار المخزنة مؤقتاً
        modelBuilder.Entity<CachedNews>(entity =>
        {
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.ApiSource);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.Title, e.SourceDomain }).IsUnique();
        });
    }
}
