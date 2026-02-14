using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MrGoldenBader.Domain.Entities;

/// <summary>
/// كيان الخبر المخزن مؤقتاً في SQL Server
/// يحتوي على نتائج تحليل الأخبار من مصادر متعددة
/// </summary>
[Table("CachedNews")]
public class CachedNews
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // ═══════════════════════════════════════════════════════════════════
    // البيانات الأساسية
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// عنوان الخبر
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// الملخص/الوصف
    /// </summary>
    [MaxLength(2000)]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// المحتوى الكامل (إن توفر)
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// مصدر الخبر (اسم الموقع)
    /// </summary>
    [MaxLength(200)]
    public string SourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// دومين المصدر
    /// </summary>
    [MaxLength(200)]
    public string SourceDomain { get; set; } = string.Empty;
    
    /// <summary>
    /// API المستخدم (AlphaVantage, NewsData, GNews, MarketAux)
    /// </summary>
    [MaxLength(50)]
    public string ApiSource { get; set; } = string.Empty;
    
    // ═══════════════════════════════════════════════════════════════════
    // المحتوى الغني
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// رابط صورة البانر
    /// </summary>
    [MaxLength(1000)]
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// رابط المقال الأصلي
    /// </summary>
    [MaxLength(1000)]
    public string? ArticleUrl { get; set; }
    
    /// <summary>
    /// أسماء الكتّاب (JSON Array)
    /// </summary>
    public string? AuthorsJson { get; set; }
    
    /// <summary>
    /// المواضيع/الكلمات المفتاحية (JSON Array)
    /// </summary>
    public string? KeywordsJson { get; set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // تحليل المشاعر والتأثير
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// المشاعر العامة (bullish, bearish, neutral)
    /// </summary>
    [MaxLength(50)]
    public string Sentiment { get; set; } = "neutral";
    
    /// <summary>
    /// درجة المشاعر (-1 إلى 1)
    /// </summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal SentimentScore { get; set; }
    
    /// <summary>
    /// اتجاه التأثير على الذهب (bullish, bearish, neutral)
    /// </summary>
    [MaxLength(50)]
    public string GoldImpactDirection { get; set; } = "neutral";
    
    /// <summary>
    /// درجة التأثير (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal GoldImpactScore { get; set; }
    
    /// <summary>
    /// شرح التأثير
    /// </summary>
    [MaxLength(500)]
    public string? ImpactExplanation { get; set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // التصنيف
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// فئة الخبر (gold, economy, finance, geopolitics, etc.)
    /// </summary>
    [MaxLength(50)]
    public string Category { get; set; } = "general";
    
    /// <summary>
    /// اللغة الأصلية
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en";
    
    // ═══════════════════════════════════════════════════════════════════
    // التواريخ
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// تاريخ نشر الخبر الأصلي
    /// </summary>
    public DateTime PublishedAt { get; set; }
    
    /// <summary>
    /// تاريخ جلب الخبر من API
    /// </summary>
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// تاريخ انتهاء الصلاحية في الـ Cache
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // ═══════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════
    
    [NotMapped]
    public List<string> Authors => string.IsNullOrEmpty(AuthorsJson) 
        ? new List<string>() 
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(AuthorsJson) ?? new List<string>();
    
    [NotMapped]
    public List<string> Keywords => string.IsNullOrEmpty(KeywordsJson) 
        ? new List<string>() 
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(KeywordsJson) ?? new List<string>();
    
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    [NotMapped]
    public bool IsHighImpact => GoldImpactScore >= 60;
}
