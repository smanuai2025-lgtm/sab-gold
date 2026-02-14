using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Infrastructure.Data;

namespace MrGoldenBader.Infrastructure.ExternalApis;

/// <summary>
/// خدمة الأخبار متعددة المصادر مع Cache ذكي في SQL Server
/// تدعم: Alpha Vantage, NewsData.io, GNews.io, MarketAux
/// </summary>
public class MultiSourceNewsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MultiSourceNewsService> _logger;
    private readonly MultiNewsApiSettings _settings;
    private readonly ApplicationDbContext _dbContext;
    
    // ترتيب المصادر للتبديل
    private static readonly string[] _sourceOrder = { "NewsData", "GNews", "MarketAux", "AlphaVantage" };
    private static int _currentSourceIndex = 0;
    private static readonly object _lockObj = new();

    public MultiSourceNewsService(
        HttpClient httpClient,
        ILogger<MultiSourceNewsService> logger,
        IOptions<MultiNewsApiSettings> settings,
        ApplicationDbContext dbContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
        _dbContext = dbContext;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// جلب الأخبار - يستخدم الـ Cache أولاً ثم يجلب من API إذا لزم الأمر
    /// </summary>
    public async Task<List<CachedNews>> GetNewsAsync(int limit = 20, bool forceRefresh = false)
    {
        try
        {
            // 1. التحقق من الـ Cache أولاً
            if (!forceRefresh)
            {
                var cachedNews = await GetValidCachedNewsAsync(limit);
                if (cachedNews.Count >= _settings.MinNewsBeforeRefresh)
                {
                    _logger.LogInformation("تم جلب {Count} خبر من الـ Cache", cachedNews.Count);
                    return cachedNews;
                }
            }

            // 2. جلب أخبار جديدة من المصادر
            var freshNews = await FetchNewsFromSourcesAsync();
            
            if (freshNews.Count > 0)
            {
                // 3. حفظ في الـ Cache
                await SaveToCacheAsync(freshNews);
                _logger.LogInformation("تم حفظ {Count} خبر جديد في الـ Cache", freshNews.Count);
            }

            // 4. إرجاع الأخبار
            return await GetValidCachedNewsAsync(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأخبار");
            return await GetValidCachedNewsAsync(limit); // إرجاع الـ Cache حتى لو منتهي
        }
    }

    /// <summary>
    /// جلب الأخبار الصالحة من الـ Cache
    /// </summary>
    private async Task<List<CachedNews>> GetValidCachedNewsAsync(int limit)
    {
        return await _dbContext.CachedNews
            .Where(n => n.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// جلب الأخبار من المصادر المتعددة بالتبديل
    /// </summary>
    private async Task<List<CachedNews>> FetchNewsFromSourcesAsync()
    {
        var allNews = new List<CachedNews>();
        var triedSources = new HashSet<string>();
        
        // نحاول من كل مصدر حتى نحصل على أخبار
        for (int i = 0; i < _sourceOrder.Length; i++)
        {
            string source;
            lock (_lockObj)
            {
                source = _sourceOrder[_currentSourceIndex];
                _currentSourceIndex = (_currentSourceIndex + 1) % _sourceOrder.Length;
            }
            
            if (triedSources.Contains(source)) continue;
            triedSources.Add(source);
            
            try
            {
                var news = source switch
                {
                    "NewsData" => await FetchFromNewsDataAsync(),
                    "GNews" => await FetchFromGNewsAsync(),
                    "MarketAux" => await FetchFromMarketAuxAsync(),
                    "AlphaVantage" => await FetchFromAlphaVantageAsync(),
                    _ => new List<CachedNews>()
                };
                
                if (news.Count > 0)
                {
                    _logger.LogInformation("تم جلب {Count} خبر من {Source}", news.Count, source);
                    allNews.AddRange(news);
                    break; // حصلنا على أخبار، نتوقف
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "فشل جلب الأخبار من {Source}", source);
            }
        }
        
        return allNews;
    }

    // ═══════════════════════════════════════════════════════════════════
    // NewsData.io
    // ═══════════════════════════════════════════════════════════════════
    private async Task<List<CachedNews>> FetchFromNewsDataAsync()
    {
        if (!_settings.NewsData.Enabled || string.IsNullOrEmpty(_settings.NewsData.ApiKey) || 
            _settings.NewsData.ApiKey.Contains("YOUR_"))
            return new List<CachedNews>();
        
        // استخدام API الجديد مع البحث عن أخبار الذهب والاقتصاد
        var url = $"{_settings.NewsData.BaseUrl}?apikey={_settings.NewsData.ApiKey}&q=gold OR economy OR inflation OR dollar&language=en,ar&category=business";
        
        _logger.LogInformation("جاري جلب الأخبار من NewsData.io...");
        
        try
        {
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("فشل NewsData: {Status} - {Error}", response.StatusCode, error);
                return new List<CachedNews>();
            }
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("NewsData Response: {Json}", json.Substring(0, Math.Min(500, json.Length)));
            
            var data = JsonSerializer.Deserialize<NewsDataResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (data?.Results == null || data.Results.Count == 0)
            {
                _logger.LogWarning("NewsData: لا توجد نتائج");
                return new List<CachedNews>();
            }
            
            // تصفية المكررات
            var filteredResults = data.Results.Where(item => !item.Duplicate).ToList();
            
            _logger.LogInformation("NewsData: تم جلب {Count} خبر (من أصل {Total})", 
                filteredResults.Count, data.Results.Count);
            
            return filteredResults.Select(item => new CachedNews
            {
                Title = item.Title ?? "",
                Summary = item.Description ?? "",
                Content = item.Content,
                SourceName = item.SourceName ?? item.SourceId ?? "NewsData",
                SourceDomain = item.SourceUrl ?? ExtractDomain(item.Link),
                ApiSource = "NewsData",
                ImageUrl = item.ImageUrl,
                ArticleUrl = item.Link,
                KeywordsJson = JsonSerializer.Serialize(item.Keywords ?? new List<string>()),
                AuthorsJson = JsonSerializer.Serialize(item.Creator ?? new List<string>()),
                Sentiment = AnalyzeSentiment(item.Title + " " + item.Description),
                SentimentScore = CalculateSentimentScore(item.Title + " " + item.Description),
                GoldImpactDirection = DetermineGoldImpact(item.Title + " " + item.Description),
                GoldImpactScore = CalculateImpactScore(item.Title + " " + item.Description),
                ImpactExplanation = GenerateImpactExplanation(item.Title),
                Category = DetermineCategory(item.Category ?? new List<string>()),
                Language = item.Language ?? "en",
                PublishedAt = DateTime.TryParse(item.PubDate, out var dt) ? dt : DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأخبار من NewsData.io");
            return new List<CachedNews>();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // GNews.io
    // ═══════════════════════════════════════════════════════════════════
    private async Task<List<CachedNews>> FetchFromGNewsAsync()
    {
        if (!_settings.GNews.Enabled || string.IsNullOrEmpty(_settings.GNews.ApiKey))
            return new List<CachedNews>();
        
        var url = $"{_settings.GNews.BaseUrl}?q=gold price OR gold market OR economy&lang=en&token={_settings.GNews.ApiKey}&max=20";
        
        _logger.LogInformation("جاري جلب الأخبار من GNews.io...");
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("فشل GNews: {Error}", error);
            return new List<CachedNews>();
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<GNewsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return data?.Articles?.Select(item => new CachedNews
        {
            Title = item.Title ?? "",
            Summary = item.Description ?? "",
            Content = item.Content,
            SourceName = item.Source?.Name ?? "GNews",
            SourceDomain = item.Source?.Url ?? "",
            ApiSource = "GNews",
            ImageUrl = item.Image,
            ArticleUrl = item.Url,
            Sentiment = AnalyzeSentiment(item.Title + " " + item.Description),
            SentimentScore = CalculateSentimentScore(item.Title + " " + item.Description),
            GoldImpactDirection = DetermineGoldImpact(item.Title + " " + item.Description),
            GoldImpactScore = CalculateImpactScore(item.Title + " " + item.Description),
            ImpactExplanation = GenerateImpactExplanation(item.Title),
            Category = "finance",
            Language = "en",
            PublishedAt = DateTime.TryParse(item.PublishedAt, out var dt) ? dt : DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes)
        }).ToList() ?? new List<CachedNews>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // MarketAux
    // ═══════════════════════════════════════════════════════════════════
    private async Task<List<CachedNews>> FetchFromMarketAuxAsync()
    {
        if (!_settings.MarketAux.Enabled || string.IsNullOrEmpty(_settings.MarketAux.ApiKey))
            return new List<CachedNews>();
        
        var url = $"{_settings.MarketAux.BaseUrl}?api_token={_settings.MarketAux.ApiKey}&symbols=GOLD,GLD&filter_entities=true&language=en";
        
        _logger.LogInformation("جاري جلب الأخبار من MarketAux...");
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("فشل MarketAux: {Error}", error);
            return new List<CachedNews>();
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<MarketAuxResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return data?.Data?.Select(item => new CachedNews
        {
            Title = item.Title ?? "",
            Summary = item.Description ?? "",
            SourceName = item.Source ?? "MarketAux",
            SourceDomain = ExtractDomain(item.Url),
            ApiSource = "MarketAux",
            ImageUrl = item.ImageUrl,
            ArticleUrl = item.Url,
            KeywordsJson = JsonSerializer.Serialize(item.Entities?.Select(e => e.Name).ToList() ?? new List<string>()),
            Sentiment = item.Sentiment ?? "neutral",
            SentimentScore = (decimal)(item.SentimentScore ?? 0),
            GoldImpactDirection = DetermineGoldImpact(item.Title + " " + item.Description),
            GoldImpactScore = CalculateImpactScore(item.Title + " " + item.Description),
            ImpactExplanation = GenerateImpactExplanation(item.Title),
            Category = "gold",
            Language = "en",
            PublishedAt = DateTime.TryParse(item.PublishedAt, out var dt) ? dt : DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes)
        }).ToList() ?? new List<CachedNews>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Alpha Vantage (الموجود مسبقاً)
    // ═══════════════════════════════════════════════════════════════════
    private async Task<List<CachedNews>> FetchFromAlphaVantageAsync()
    {
        if (!_settings.AlphaVantage.Enabled || string.IsNullOrEmpty(_settings.AlphaVantage.ApiKey))
            return new List<CachedNews>();
        
        var url = $"{_settings.AlphaVantage.BaseUrl}?function=NEWS_SENTIMENT&topics=economy_macro&sort=LATEST&limit=20&apikey={_settings.AlphaVantage.ApiKey}";
        
        _logger.LogInformation("جاري جلب الأخبار من Alpha Vantage...");
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("فشل Alpha Vantage: {Error}", error);
            return new List<CachedNews>();
        }
        
        var json = await response.Content.ReadAsStringAsync();
        
        // التحقق من رسائل الخطأ
        if (json.Contains("Invalid") || json.Contains("limit"))
        {
            _logger.LogWarning("Alpha Vantage limit reached or invalid request");
            return new List<CachedNews>();
        }
        
        var data = JsonSerializer.Deserialize<AlphaVantageNewsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return data?.Feed?.Select(item => new CachedNews
        {
            Title = item.Title ?? "",
            Summary = item.Summary ?? "",
            SourceName = item.Source ?? "Alpha Vantage",
            SourceDomain = item.SourceDomain ?? "",
            ApiSource = "AlphaVantage",
            ImageUrl = item.BannerImage,
            ArticleUrl = item.Url,
            AuthorsJson = JsonSerializer.Serialize(item.Authors ?? new List<string>()),
            KeywordsJson = JsonSerializer.Serialize(item.Topics?.Select(t => t.Topic).ToList() ?? new List<string>()),
            Sentiment = item.OverallSentimentLabel ?? "neutral",
            SentimentScore = decimal.TryParse(item.OverallSentimentScore, out var s) ? s : 0,
            GoldImpactDirection = DetermineGoldImpactFromSentiment(item.OverallSentimentLabel),
            GoldImpactScore = CalculateImpactFromSentiment(item.OverallSentimentScore),
            ImpactExplanation = GenerateImpactExplanation(item.Title),
            Category = DetermineCategory(item.Topics?.Select(t => t.Topic).ToList() ?? new List<string>()),
            Language = "en",
            PublishedAt = ParseAlphaVantageDate(item.TimePublished),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes)
        }).ToList() ?? new List<CachedNews>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════
    
    private async Task SaveToCacheAsync(List<CachedNews> news)
    {
        foreach (var item in news)
        {
            // تفادي التكرار
            var exists = await _dbContext.CachedNews
                .AnyAsync(n => n.Title == item.Title && n.SourceDomain == item.SourceDomain);
            
            if (!exists)
            {
                _dbContext.CachedNews.Add(item);
            }
        }
        
        await _dbContext.SaveChangesAsync();
        
        // حذف الأخبار المنتهية
        var expired = await _dbContext.CachedNews
            .Where(n => n.ExpiresAt < DateTime.UtcNow.AddHours(-24))
            .ToListAsync();
        
        if (expired.Count > 0)
        {
            _dbContext.CachedNews.RemoveRange(expired);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("تم حذف {Count} خبر منتهي الصلاحية", expired.Count);
        }
    }
    
    private string AnalyzeSentiment(string text)
    {
        if (string.IsNullOrEmpty(text)) return "neutral";
        var lower = text.ToLower();
        
        var bullishWords = new[] { "surge", "rally", "gains", "rises", "bullish", "soars", "jumps", "higher", "up", "growth", "strong" };
        var bearishWords = new[] { "falls", "drops", "plunges", "bearish", "decline", "down", "weak", "lower", "crash", "slump" };
        
        int bullish = bullishWords.Count(w => lower.Contains(w));
        int bearish = bearishWords.Count(w => lower.Contains(w));
        
        if (bullish > bearish) return "bullish";
        if (bearish > bullish) return "bearish";
        return "neutral";
    }
    
    private decimal CalculateSentimentScore(string text)
    {
        var sentiment = AnalyzeSentiment(text);
        return sentiment switch
        {
            "bullish" => 0.5m + (decimal)(new Random().NextDouble() * 0.4),
            "bearish" => -0.5m - (decimal)(new Random().NextDouble() * 0.4),
            _ => (decimal)(new Random().NextDouble() * 0.2 - 0.1)
        };
    }
    
    private string DetermineGoldImpact(string text)
    {
        if (string.IsNullOrEmpty(text)) return "neutral";
        var lower = text.ToLower();
        
        // كلمات تدعم ارتفاع الذهب
        var bullishForGold = new[] { "inflation", "uncertainty", "crisis", "war", "tension", "rate cut", "dovish", "weak dollar" };
        var bearishForGold = new[] { "rate hike", "hawkish", "strong dollar", "growth", "optimism", "recovery" };
        
        int bullish = bullishForGold.Count(w => lower.Contains(w));
        int bearish = bearishForGold.Count(w => lower.Contains(w));
        
        if (bullish > bearish) return "bullish";
        if (bearish > bullish) return "bearish";
        return "neutral";
    }
    
    private decimal CalculateImpactScore(string text)
    {
        var impact = DetermineGoldImpact(text);
        return impact switch
        {
            "bullish" or "bearish" => 50m + (decimal)(new Random().NextDouble() * 40),
            _ => 20m + (decimal)(new Random().NextDouble() * 30)
        };
    }
    
    private string DetermineGoldImpactFromSentiment(string? sentiment)
    {
        return sentiment?.ToLower() switch
        {
            "bullish" or "somewhat-bullish" => "bullish",
            "bearish" or "somewhat-bearish" => "bearish",
            _ => "neutral"
        };
    }
    
    private decimal CalculateImpactFromSentiment(string? score)
    {
        if (decimal.TryParse(score, out var s))
            return Math.Min(95, Math.Abs(s) * 100 + 20);
        return 30;
    }
    
    private string GenerateImpactExplanation(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "تحليل آلي للخبر";
        var impact = DetermineGoldImpact(title);
        return impact switch
        {
            "bullish" => "هذا الخبر قد يدعم ارتفاع أسعار الذهب",
            "bearish" => "هذا الخبر قد يضغط على أسعار الذهب",
            _ => "تأثير هذا الخبر محايد على الذهب"
        };
    }
    
    private string DetermineCategory(List<string> categories)
    {
        if (categories == null || !categories.Any()) return "general";
        var first = categories.First().ToLower();
        
        if (first.Contains("gold") || first.Contains("metal")) return "gold";
        if (first.Contains("economy") || first.Contains("macro")) return "economy";
        if (first.Contains("finance") || first.Contains("market")) return "finance";
        if (first.Contains("politic") || first.Contains("war")) return "geopolitics";
        
        return first;
    }
    
    private string ExtractDomain(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        try
        {
            return new Uri(url).Host;
        }
        catch
        {
            return "";
        }
    }
    
    private DateTime ParseAlphaVantageDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr) || dateStr.Length < 15) return DateTime.UtcNow;
        try
        {
            return DateTime.ParseExact(dateStr.Substring(0, 15), "yyyyMMdd'T'HHmmss", null);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
// Response DTOs
// ═══════════════════════════════════════════════════════════════════

public class NewsDataResponse
{
    public string? Status { get; set; }
    public int TotalResults { get; set; }
    public List<NewsDataArticle>? Results { get; set; }
    public string? NextPage { get; set; }
}

public class NewsDataArticle
{
    [JsonPropertyName("article_id")]
    public string? ArticleId { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    
    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
    
    [JsonPropertyName("creator")]
    public List<string>? Creator { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("pubDate")]
    public string? PubDate { get; set; }
    
    [JsonPropertyName("pubDateTZ")]
    public string? PubDateTZ { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    [JsonPropertyName("video_url")]
    public string? VideoUrl { get; set; }
    
    [JsonPropertyName("source_id")]
    public string? SourceId { get; set; }
    
    [JsonPropertyName("source_name")]
    public string? SourceName { get; set; }
    
    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }
    
    [JsonPropertyName("source_icon")]
    public string? SourceIcon { get; set; }
    
    [JsonPropertyName("source_priority")]
    public int? SourcePriority { get; set; }
    
    [JsonPropertyName("category")]
    public List<string>? Category { get; set; }
    
    [JsonPropertyName("country")]
    public List<string>? Country { get; set; }
    
    [JsonPropertyName("language")]
    public string? Language { get; set; }
    
    [JsonPropertyName("sentiment")]
    public string? Sentiment { get; set; }
    
    [JsonPropertyName("ai_tag")]
    public string? AiTag { get; set; }
    
    [JsonPropertyName("duplicate")]
    public bool Duplicate { get; set; }
}

public class GNewsResponse
{
    public int TotalArticles { get; set; }
    public List<GNewsArticle>? Articles { get; set; }
}

public class GNewsArticle
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public string? Image { get; set; }
    public string? PublishedAt { get; set; }
    public GNewsSource? Source { get; set; }
}

public class GNewsSource
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class MarketAuxResponse
{
    public List<MarketAuxArticle>? Data { get; set; }
}

public class MarketAuxArticle
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }
    public string? Source { get; set; }
    public string? Sentiment { get; set; }
    [JsonPropertyName("sentiment_score")]
    public double? SentimentScore { get; set; }
    public List<MarketAuxEntity>? Entities { get; set; }
}

public class MarketAuxEntity
{
    public string? Name { get; set; }
    public string? Type { get; set; }
}

public class AlphaVantageNewsResponse
{
    public List<AlphaVantageNewsItem>? Feed { get; set; }
}

public class AlphaVantageNewsItem
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    [JsonPropertyName("time_published")]
    public string? TimePublished { get; set; }
    public List<string>? Authors { get; set; }
    public string? Summary { get; set; }
    [JsonPropertyName("banner_image")]
    public string? BannerImage { get; set; }
    public string? Source { get; set; }
    [JsonPropertyName("source_domain")]
    public string? SourceDomain { get; set; }
    [JsonPropertyName("overall_sentiment_score")]
    public string? OverallSentimentScore { get; set; }
    [JsonPropertyName("overall_sentiment_label")]
    public string? OverallSentimentLabel { get; set; }
    public List<AlphaVantageTopic>? Topics { get; set; }
}

public class AlphaVantageTopic
{
    public string? Topic { get; set; }
    [JsonPropertyName("relevance_score")]
    public string? RelevanceScore { get; set; }
}
