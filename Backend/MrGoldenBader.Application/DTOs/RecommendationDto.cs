namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات التوصية
/// </summary>
public class RecommendationDto
{
    /// <summary>
    /// المعرف
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// نوع التوصية
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// نوع التوصية بالعربية
    /// </summary>
    public string TypeArabic => Type switch
    {
        "Buy" => "اشترِ الآن",
        "Hold" => "انتظر",
        "Sell" => "بع",
        _ => Type
    };
    
    /// <summary>
    /// السبب
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// درجة الثقة
    /// </summary>
    public decimal ConfidenceScore { get; set; }
    
    /// <summary>
    /// وصف درجة الثقة
    /// </summary>
    public string ConfidenceDescription => ConfidenceScore switch
    {
        >= 80 => "ثقة عالية جداً",
        >= 60 => "ثقة عالية",
        >= 40 => "ثقة متوسطة",
        >= 20 => "ثقة منخفضة",
        _ => "ثقة منخفضة جداً"
    };
    
    /// <summary>
    /// السعر عند التوصية
    /// </summary>
    public decimal PriceAtRecommendation { get; set; }
    
    /// <summary>
    /// السعر المستهدف
    /// </summary>
    public decimal? TargetPrice { get; set; }
    
    /// <summary>
    /// وقف الخسارة
    /// </summary>
    public decimal? StopLoss { get; set; }
    
    /// <summary>
    /// الأفق الزمني
    /// </summary>
    public string TimeHorizon { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ الإنشاء
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// تاريخ الانتهاء
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// هل التوصية نشطة
    /// </summary>
    public bool IsActive { get; set; }
}
