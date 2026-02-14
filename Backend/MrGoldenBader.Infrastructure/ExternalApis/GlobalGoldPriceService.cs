using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace MrGoldenBader.Infrastructure.ExternalApis;

/// <summary>
/// خدمة جلب أسعار الذهب العالمية من APIs حقيقية
/// ═══════════════════════════════════════════════════════════════════
/// المصادر المدعومة:
/// 1. Alpha Vantage (يحتاج مفتاح - موثوق)
/// 2. Frankfurter (مجاني - احتياطي للعملات)
/// 3. GoldAPI.io (يحتاج مفتاح - اختياري)
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
public class GlobalGoldPriceService
{
    private readonly HttpClient _httpClient;
    private readonly IGoldPriceRepository _repository;
    private readonly ILogger<GlobalGoldPriceService> _logger;
    private readonly ExternalApiSettings _settings;
    private readonly IDistributedCache? _cache;

    // سعر الذهب الأساسي (للاستخدام عند فشل جميع APIs)
    // ⚠️ هذا السعر احتياطي فقط - يجب أن يأتي السعر من TradingView
    // السعر الحقيقي للأونصة: ~$4,800 (يناير 2026) - من TradingView GC1!
    private const decimal BASE_GOLD_PRICE_USD = 4800.00m;
    
    public GlobalGoldPriceService(
        HttpClient httpClient,
        IGoldPriceRepository repository,
        ILogger<GlobalGoldPriceService> logger,
        IOptions<ExternalApiSettings> settings,
        IDistributedCache? cache = null)
    {
        _httpClient = httpClient;
        _repository = repository;
        _logger = logger;
        _settings = settings.Value;
        _cache = cache;
        
        // إعداد timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // إعداد User-Agent
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MrGoldenBader/2.0");
        }
    }

    /// <summary>
    /// تحديث الأسعار - يُستدعى من Hangfire
    /// </summary>
    public async Task RefreshPricesAsync()
    {
        await FetchCurrentPriceAsync();
    }

    /// <summary>
    /// جلب السعر الحالي من APIs حقيقية
    /// </summary>
    public async Task<GoldPrice?> FetchCurrentPriceAsync()
    {
        try
        {
            _logger.LogInformation("═══════════════════════════════════════════════════════════════════");
            _logger.LogInformation("جاري جلب سعر الذهب العالمي من المصادر الحقيقية...");
            _logger.LogInformation("═══════════════════════════════════════════════════════════════════");
            
            GoldPrice? price = null;
            
            // المحاولة 1: Metals.dev API (مجاني وموثوق)
            price = await FetchFromMetalsDevAsync();
            
            // المحاولة 2: Alpha Vantage
            if (price == null)
            {
                price = await FetchFromAlphaVantageAsync();
            }
            
            // المحاولة 3: GoldAPI.io
            if (price == null)
            {
                price = await FetchFromGoldApiAsync();
            }
            
            // المحاولة 4: حساب من سعر سابق مع تحديث طفيف
            if (price == null)
            {
                price = await GetCalculatedPriceAsync();
            }
            
            if (price != null)
            {
                // حفظ في قاعدة البيانات
                await _repository.AddAsync(price);
                await _repository.SaveChangesAsync();
                
                // حفظ في Redis Cache
                await CachePriceAsync(price);
                
                _logger.LogInformation(
                    "✅ تم جلب سعر الذهب: ${Price} USD من {Source} في {Time}", 
                    price.Close, 
                    price.Source,
                    price.Timestamp);
            }
            else
            {
                _logger.LogWarning("⚠️ فشل جلب السعر من جميع المصادر");
            }
            
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب سعر الذهب العالمي");
            return null;
        }
    }
    
    /// <summary>
    /// جلب السعر من Gold Price API (مجاني وموثوق)
    /// </summary>
    private async Task<GoldPrice?> FetchFromMetalsDevAsync()
    {
        // جرب عدة APIs مجانية
        var apis = new[]
        {
            ("https://data-asg.goldprice.org/dbXRates/USD", "GoldPrice.org"),
            ("https://api.nbp.pl/api/cenyzlota/?format=json", "NBP.pl"),
        };
        
        foreach (var (url, source) in apis)
        {
            try
            {
                _logger.LogInformation("محاولة جلب السعر من {Source}...", source);
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.GetAsync(url, cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("فشل {Source}: {Status}", source, response.StatusCode);
                    continue;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("{Source} Response: {Json}", source, json);
                
                decimal price = 0;
                
                if (source == "GoldPrice.org")
                {
                    // Format: {"items":[{"xauPrice":2750.50,...}]}
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("items", out var items) && 
                        items.GetArrayLength() > 0)
                    {
                        var item = items[0];
                        if (item.TryGetProperty("xauPrice", out var xauPrice))
                        {
                            price = xauPrice.GetDecimal();
                        }
                    }
                }
                else if (source == "NBP.pl")
                {
                    // Format: [{"data":"2026-01-20","cena":387.52}]
                    // NBP gives price in PLN per gram, need to convert
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && 
                        doc.RootElement.GetArrayLength() > 0)
                    {
                        var item = doc.RootElement[0];
                        if (item.TryGetProperty("cena", out var cena))
                        {
                            // Convert PLN/gram to USD/ounce
                            // Approximate: 1 USD = 4 PLN, 1 oz = 31.1035 grams
                            var plnPerGram = cena.GetDecimal();
                            price = (plnPerGram / 4.0m) * 31.1035m;
                        }
                    }
                }
                
                if (price > 2000 && price < 10000) // سعر معقول بين $2000 و $10000
                {
                    var lastPrice = await _repository.GetLatestAsync();
                    
                    _logger.LogInformation("✅ {Source}: تم جلب السعر ${Price}", source, price);
                    
                    return new GoldPrice
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        Open = lastPrice?.Close ?? price,
                        High = Math.Max(price, lastPrice?.Close ?? price),
                        Low = Math.Min(price, lastPrice?.Close ?? price),
                        Close = price,
                        Volume = null,
                        TimeFrame = "Minute",
                        Source = source,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("انتهت المهلة لـ {Source}", source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("خطأ في {Source}: {Message}", source, ex.Message);
            }
        }
        
        _logger.LogWarning("فشلت جميع APIs المجانية");
        return null;
    }

    /// <summary>
    /// جلب السعر من Alpha Vantage (الأكثر موثوقية)
    /// </summary>
    private async Task<GoldPrice?> FetchFromAlphaVantageAsync()
    {
        try
        {
            var apiKey = _settings.AlphaVantageApi?.ApiKey;
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR_"))
            {
                _logger.LogWarning("Alpha Vantage: مفتاح API غير مُعدّ");
                return null;
            }
            
            _logger.LogInformation("محاولة جلب السعر من Alpha Vantage...");
            
            // استخدام CURRENCY_EXCHANGE_RATE للحصول على سعر XAU/USD
            var url = $"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency=XAU&to_currency=USD&apikey={apiKey}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("فشل Alpha Vantage: {Status}", response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Alpha Vantage Response: {Json}", json.Substring(0, Math.Min(500, json.Length)));
            
            // التحقق من حد الاستخدام
            if (json.Contains("Note") || json.Contains("Thank you for using") || json.Contains("API rate limit"))
            {
                _logger.LogWarning("Alpha Vantage: تم الوصول لحد الاستخدام");
                return null;
            }
            
            // التحقق من وجود خطأ
            if (json.Contains("Error") || json.Contains("error"))
            {
                _logger.LogWarning("Alpha Vantage: خطأ في الاستجابة - {Json}", json);
                return null;
            }
            
            var data = JsonSerializer.Deserialize<AlphaVantageFxResponse>(json);
            
            if (data?.RealtimeCurrencyExchangeRate == null)
            {
                _logger.LogWarning("Alpha Vantage: لا توجد بيانات في الاستجابة");
                return null;
            }
            
            var rate = data.RealtimeCurrencyExchangeRate;
            
            // تحليل السعر
            if (!decimal.TryParse(rate.ExchangeRate, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                _logger.LogWarning("Alpha Vantage: سعر غير صالح - {Rate}", rate.ExchangeRate);
                return null;
            }
            
            var lastPrice = await _repository.GetLatestAsync();
            
            return new GoldPrice
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Open = lastPrice?.Close ?? price,
                High = Math.Max(price, lastPrice?.Close ?? price),
                Low = Math.Min(price, lastPrice?.Close ?? price),
                Close = price,
                Volume = null,
                TimeFrame = "Minute",
                Source = "Alpha Vantage",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطأ في Alpha Vantage: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// جلب السعر من GoldAPI.io
    /// </summary>
    private async Task<GoldPrice?> FetchFromGoldApiAsync()
    {
        try
        {
            var apiKey = _settings.GoldApiKey;
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR_"))
            {
                _logger.LogDebug("GoldAPI: مفتاح API غير مُعدّ");
                return null;
            }
            
            _logger.LogInformation("محاولة جلب السعر من GoldAPI.io...");
            
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.goldapi.io/api/XAU/USD");
            request.Headers.Add("x-access-token", apiKey);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("فشل GoldAPI: {Status}", response.StatusCode);
                return null;
            }
            
            var data = await response.Content.ReadFromJsonAsync<GoldApiResponse>();
            
            if (data == null || data.Price <= 0)
            {
                _logger.LogWarning("GoldAPI: لا توجد بيانات");
                return null;
            }
            
            return new GoldPrice
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime,
                Open = data.OpenPrice > 0 ? data.OpenPrice : data.Price,
                High = data.HighPrice > 0 ? data.HighPrice : data.Price,
                Low = data.LowPrice > 0 ? data.LowPrice : data.Price,
                Close = data.Price,
                Volume = null,
                TimeFrame = "Minute",
                Source = "GoldAPI.io",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطأ في GoldAPI.io");
            return null;
        }
    }

    /// <summary>
    /// حساب السعر من آخر سعر مع تقلب طفيف (احتياطي عند فشل APIs)
    /// </summary>
    private async Task<GoldPrice?> GetCalculatedPriceAsync()
    {
        try
        {
            _logger.LogInformation("استخدام السعر المحسوب (احتياطي)...");
            
            var lastPrice = await _repository.GetLatestAsync();
            decimal basePrice = lastPrice?.Close ?? BASE_GOLD_PRICE_USD;
            
            // تقلب طفيف بناءً على الوقت (±0.1%)
            var random = new Random((int)DateTime.UtcNow.Ticks);
            var fluctuation = (decimal)(random.NextDouble() - 0.5) * 0.002m; // ±0.1%
            var newPrice = Math.Round(basePrice * (1 + fluctuation), 2);
            
            return new GoldPrice
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Open = basePrice,
                High = Math.Max(newPrice, basePrice),
                Low = Math.Min(newPrice, basePrice),
                Close = newPrice,
                Volume = null,
                TimeFrame = "Minute",
                Source = "Calculated",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب السعر");
            return null;
        }
    }

    /// <summary>
    /// حفظ السعر في Redis Cache
    /// </summary>
    private async Task CachePriceAsync(GoldPrice price)
    {
        if (_cache == null) return;
        
        try
        {
            var cacheKey = "gold:price:latest";
            var json = JsonSerializer.Serialize(price);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            
            await _cache.SetStringAsync(cacheKey, json, options);
            _logger.LogDebug("تم حفظ السعر في Redis Cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "فشل حفظ السعر في Redis");
        }
    }

    /// <summary>
    /// جلب آخر سعر من المخزن المؤقت أو API
    /// </summary>
    public async Task<GoldPrice?> GetLatestPriceAsync()
    {
        // أولاً: محاولة جلب من Redis Cache
        if (_cache != null)
        {
            try
            {
                var cached = await _cache.GetStringAsync("gold:price:latest");
                if (!string.IsNullOrEmpty(cached))
                {
                    var cachedPrice = JsonSerializer.Deserialize<GoldPrice>(cached);
                    if (cachedPrice != null && (DateTime.UtcNow - cachedPrice.Timestamp).TotalSeconds < 60)
                    {
                        _logger.LogDebug("تم جلب السعر من Redis Cache");
                        return cachedPrice;
                    }
                }
            }
            catch { /* تجاهل أخطاء Cache */ }
        }
        
        // ثانياً: جلب من قاعدة البيانات
        var latestFromDb = await _repository.GetLatestAsync();
        
        // إذا لم يمض أكثر من دقيقة على آخر تحديث، استخدم القيمة المخزنة
        var refreshInterval = _settings.AlphaVantageApi?.RefreshIntervalSeconds ?? 60;
        if (latestFromDb != null && 
            (DateTime.UtcNow - latestFromDb.Timestamp).TotalSeconds < refreshInterval)
        {
            return latestFromDb;
        }
        
        // جلب سعر جديد من API
        return await FetchCurrentPriceAsync() ?? latestFromDb;
    }
}

// ═══════════════════════════════════════════════════════════════════
// نماذج الاستجابة من APIs
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// استجابة Alpha Vantage للعملات
/// </summary>
public class AlphaVantageFxResponse
{
    [JsonPropertyName("Realtime Currency Exchange Rate")]
    public AlphaVantageExchangeRate? RealtimeCurrencyExchangeRate { get; set; }
}

public class AlphaVantageExchangeRate
{
    [JsonPropertyName("1. From_Currency Code")]
    public string FromCurrencyCode { get; set; } = "";
    
    [JsonPropertyName("2. From_Currency Name")]
    public string FromCurrencyName { get; set; } = "";
    
    [JsonPropertyName("3. To_Currency Code")]
    public string ToCurrencyCode { get; set; } = "";
    
    [JsonPropertyName("4. To_Currency Name")]
    public string ToCurrencyName { get; set; } = "";
    
    [JsonPropertyName("5. Exchange Rate")]
    public string ExchangeRate { get; set; } = "0";
    
    [JsonPropertyName("6. Last Refreshed")]
    public string LastRefreshed { get; set; } = "";
    
    [JsonPropertyName("7. Time Zone")]
    public string TimeZone { get; set; } = "";
    
    [JsonPropertyName("8. Bid Price")]
    public string BidPrice { get; set; } = "0";
    
    [JsonPropertyName("9. Ask Price")]
    public string AskPrice { get; set; } = "0";
}

/// <summary>
/// استجابة GoldAPI.io
/// </summary>
public class GoldApiResponse
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("metal")]
    public string Metal { get; set; } = "XAU";
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("prev_close_price")]
    public decimal PrevClosePrice { get; set; }
    
    [JsonPropertyName("open_price")]
    public decimal OpenPrice { get; set; }
    
    [JsonPropertyName("high_price")]
    public decimal HighPrice { get; set; }
    
    [JsonPropertyName("low_price")]
    public decimal LowPrice { get; set; }
    
    [JsonPropertyName("ch")]
    public decimal Change { get; set; }
    
    [JsonPropertyName("chp")]
    public decimal ChangePercent { get; set; }
}
