namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات التنبؤ
/// </summary>
public class PredictionDto
{
    /// <summary>
    /// الطابع الزمني
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// السعر المتوقع
    /// </summary>
    public decimal PredictedPrice { get; set; }
    
    /// <summary>
    /// نسبة الثقة (0-1)
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// وصف الثقة بالعربية
    /// </summary>
    public string ConfidenceDescription => Confidence switch
    {
        >= 0.9 => "ثقة عالية جداً",
        >= 0.75 => "ثقة عالية",
        >= 0.6 => "ثقة متوسطة",
        >= 0.4 => "ثقة منخفضة",
        _ => "ثقة ضعيفة"
    };
    
    /// <summary>
    /// الحد الأدنى
    /// </summary>
    public decimal LowerBound { get; set; }
    
    /// <summary>
    /// الحد الأعلى
    /// </summary>
    public decimal UpperBound { get; set; }
}

/// <summary>
/// استجابة التنبؤ الكاملة
/// </summary>
public class PredictionResultDto
{
    /// <summary>
    /// التنبؤات
    /// </summary>
    public List<PredictionDto> Predictions { get; set; } = new();
    
    /// <summary>
    /// الاتجاه العام
    /// </summary>
    public string Trend { get; set; } = "neutral";
    
    /// <summary>
    /// الاتجاه بالعربية
    /// </summary>
    public string TrendArabic => Trend switch
    {
        "bullish" => "صعودي",
        "bearish" => "هبوطي",
        _ => "محايد"
    };
    
    /// <summary>
    /// أيقونة الاتجاه
    /// </summary>
    public string TrendIcon => Trend switch
    {
        "bullish" => "📈",
        "bearish" => "📉",
        _ => "➖"
    };
    
    /// <summary>
    /// قوة الاتجاه (0-100)
    /// </summary>
    public double TrendStrength { get; set; }
    
    /// <summary>
    /// دقة النموذج
    /// </summary>
    public double ModelAccuracy { get; set; }
    
    /// <summary>
    /// تاريخ التوليد
    /// </summary>
    public DateTime GeneratedAt { get; set; }
    
    /// <summary>
    /// ملخص التنبؤ
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// طلب التنبؤ
/// </summary>
public class PredictionRequestDto
{
    /// <summary>
    /// عدد ساعات التنبؤ
    /// </summary>
    public int Hours { get; set; } = 24;
}
