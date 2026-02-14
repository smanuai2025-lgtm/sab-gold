namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// ملخص تأثير الأخبار على الذهب
/// </summary>
public class NewsImpactSummaryDto
{
    /// <summary>
    /// إجمالي عدد الأخبار المحللة
    /// </summary>
    public int TotalNews { get; set; }
    
    /// <summary>
    /// عدد الأخبار الصعودية
    /// </summary>
    public int BullishCount { get; set; }
    
    /// <summary>
    /// عدد الأخبار الهبوطية
    /// </summary>
    public int BearishCount { get; set; }
    
    /// <summary>
    /// عدد الأخبار المحايدة
    /// </summary>
    public int NeutralCount { get; set; }
    
    /// <summary>
    /// متوسط درجة التأثير
    /// </summary>
    public double AverageImpact { get; set; }
    
    /// <summary>
    /// الاتجاه العام
    /// </summary>
    public string OverallDirection { get; set; } = "neutral";
    
    /// <summary>
    /// الاتجاه بالعربية
    /// </summary>
    public string OverallDirectionArabic => OverallDirection switch
    {
        "bullish" => "صعودي",
        "bearish" => "هبوطي",
        _ => "محايد"
    };
    
    /// <summary>
    /// أيقونة الاتجاه
    /// </summary>
    public string DirectionIcon => OverallDirection switch
    {
        "bullish" => "📈",
        "bearish" => "📉",
        _ => "➖"
    };
    
    /// <summary>
    /// السعر الحالي
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// درجة التأثير الإجمالية
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// تغير السعر
    /// </summary>
    public decimal PriceChange { get; set; }
    
    /// <summary>
    /// نسبة تغير السعر
    /// </summary>
    public decimal PriceChangePercent { get; set; }
    
    /// <summary>
    /// الأخبار عالية التأثير
    /// </summary>
    public List<AnalyzedNewsDto> HighImpactNews { get; set; } = new();
    
    /// <summary>
    /// تاريخ التحليل
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// الملخص النصي
    /// </summary>
    public string Summary => GenerateSummary();
    
    private string GenerateSummary()
    {
        if (TotalNews == 0)
            return "لا توجد أخبار محللة حالياً";
            
        var directionText = OverallDirectionArabic;
        return $"تم تحليل {TotalNews} خبر. " +
               $"الاتجاه العام {directionText} بمتوسط تأثير {AverageImpact:F0}%. " +
               $"({BullishCount} صعودي، {BearishCount} هبوطي، {NeutralCount} محايد)";
    }
}

/// <summary>
/// لوحة معلومات متكاملة
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// السعر العالمي الحالي
    /// </summary>
    public GoldPriceDto? GlobalPrice { get; set; }
    
    /// <summary>
    /// أسعار الكويت
    /// </summary>
    public List<KuwaitGoldPriceDto> KuwaitPrices { get; set; } = new();
    
    /// <summary>
    /// ملخص الأخبار
    /// </summary>
    public NewsImpactSummaryDto? NewsImpact { get; set; }
    
    /// <summary>
    /// التوصية الحالية
    /// </summary>
    public RecommendationDto? Recommendation { get; set; }
    
    /// <summary>
    /// التنبؤ القصير المدى
    /// </summary>
    public PredictionResultDto? ShortTermPrediction { get; set; }
    
    /// <summary>
    /// عدد التنبيهات غير المقروءة
    /// </summary>
    public int UnreadAlerts { get; set; }
    
    /// <summary>
    /// حالة السوق
    /// </summary>
    public string MarketStatus { get; set; } = "مفتوح";
    
    /// <summary>
    /// آخر تحديث
    /// </summary>
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}
