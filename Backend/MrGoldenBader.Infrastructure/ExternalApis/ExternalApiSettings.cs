namespace MrGoldenBader.Infrastructure.ExternalApis;

/// <summary>
/// إعدادات APIs الخارجية لجلب البيانات
/// </summary>
public class ExternalApiSettings
{
    /// <summary>
    /// إعدادات API أسعار الذهب العالمية
    /// </summary>
    public GoldPriceApiSettings GoldPriceApi { get; set; } = new();
    
    /// <summary>
    /// إعدادات API أسعار الكويت
    /// </summary>
    public KuwaitGoldApiSettings KuwaitGoldApi { get; set; } = new();
    


    /// <summary>
    /// إعدادات Alpha Vantage API
    /// </summary>
    public AlphaVantageApiSettings AlphaVantageApi { get; set; } = new();
    
    /// <summary>
    /// مفتاح GoldAPI.io (اختياري)
    /// </summary>
    public string GoldApiKey { get; set; } = string.Empty;
}

/// <summary>
/// إعدادات API أسعار الذهب العالمية
/// </summary>
public class GoldPriceApiSettings
{
    /// <summary>
    /// رابط API الأساسي
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.gold-api.com/price";
    
    /// <summary>
    /// مفتاح API
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// فترة التحديث بالثواني
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// إعدادات API أسعار الكويت
/// </summary>
public class KuwaitGoldApiSettings
{
    /// <summary>
    /// رابط API الأساسي
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.kuwaitgold.com/prices";
    
    /// <summary>
    /// مفتاح API
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// فترة التحديث بالثواني
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 300;
}



/// <summary>
/// إعدادات Alpha Vantage API للأخبار والتحليل المالي
/// </summary>
public class AlphaVantageApiSettings
{
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public string ApiKey { get; set; } = string.Empty;
    public int RefreshIntervalSeconds { get; set; } = 3600; // افتراضياً كل ساعة
}
