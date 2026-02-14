namespace MrGoldenBader.Infrastructure.ExternalApis;

/// <summary>
/// إعدادات APIs الأخبار المتعددة
/// </summary>
public class MultiNewsApiSettings
{
    /// <summary>
    /// إعدادات Alpha Vantage
    /// </summary>
    public AlphaVantageSettings AlphaVantage { get; set; } = new();
    
    /// <summary>
    /// إعدادات NewsData.io
    /// </summary>
    public NewsDataSettings NewsData { get; set; } = new();
    
    /// <summary>
    /// إعدادات GNews.io
    /// </summary>
    public GNewsSettings GNews { get; set; } = new();
    
    /// <summary>
    /// إعدادات MarketAux
    /// </summary>
    public MarketAuxSettings MarketAux { get; set; } = new();
    
    /// <summary>
    /// مدة صلاحية الـ Cache بالدقائق
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// الحد الأدنى لعدد الأخبار قبل الجلب من API جديد
    /// </summary>
    public int MinNewsBeforeRefresh { get; set; } = 5;
}

public class AlphaVantageSettings
{
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int DailyLimit { get; set; } = 25;
}

public class NewsDataSettings
{
    public string BaseUrl { get; set; } = "https://newsdata.io/api/1/news";
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int DailyLimit { get; set; } = 200;
}

public class GNewsSettings
{
    public string BaseUrl { get; set; } = "https://gnews.io/api/v4/search";
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int DailyLimit { get; set; } = 100;
}

public class MarketAuxSettings
{
    public string BaseUrl { get; set; } = "https://api.marketaux.com/v1/news/all";
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int DailyLimit { get; set; } = 100;
}
