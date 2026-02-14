using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MrGoldenBader.Infrastructure.ExternalApis;

/// <summary>
/// خدمة جلب أسعار الذهب في الكويت
/// تجلب أسعار جميع العيارات (24, 22, 21, 18)
/// تحسب الأسعار من السعر العالمي الحالي
/// </summary>
public class KuwaitGoldPriceService
{
    private readonly HttpClient _httpClient;
    private readonly IKuwaitGoldPriceRepository _repository;
    private readonly IGoldPriceRepository _globalPriceRepository;
    private readonly ILogger<KuwaitGoldPriceService> _logger;
    private readonly ExternalApiSettings _settings;

    // العيارات المتاحة في الكويت (مثل دار السبائك)
    private static readonly int[] Karats = { 24, 22, 21, 18 };

    public KuwaitGoldPriceService(
        HttpClient httpClient,
        IKuwaitGoldPriceRepository repository,
        IGoldPriceRepository globalPriceRepository,
        ILogger<KuwaitGoldPriceService> logger,
        IOptions<ExternalApiSettings> settings)
    {
        _httpClient = httpClient;
        _repository = repository;
        _globalPriceRepository = globalPriceRepository;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// جلب أسعار جميع العيارات من API خارجي
    /// </summary>
    public async Task<List<KuwaitGoldPrice>> FetchCurrentPricesAsync()
    {
        try
        {
            _logger.LogInformation("جاري جلب أسعار الذهب في الكويت...");
            
            var prices = await GetPricesFromApiAsync();
            
            if (prices.Any())
            {
                foreach (var price in prices)
                {
                    await _repository.AddAsync(price);
                }
                await _repository.SaveChangesAsync();
                
                _logger.LogInformation(
                    "تم جلب {Count} أسعار للكويت في {Time}", 
                    prices.Count, 
                    DateTime.UtcNow);
            }
            
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب أسعار الكويت");
            return new List<KuwaitGoldPrice>();
        }
    }

    /// <summary>
    /// جلب الأسعار من API الخارجي أو حسابها من السعر العالمي
    /// ═══════════════════════════════════════════════════════════════════
    /// المعادلة الموحدة (من GoldPriceCalculator):
    /// Gram Price = (Gold Ounce Price in USD ÷ 31.1035) × USD to KWD Exchange Rate × Karat Ratio
    /// ═══════════════════════════════════════════════════════════════════
    /// </summary>
    private async Task<List<KuwaitGoldPrice>> GetPricesFromApiAsync()
    {
        try
        {
            var prices = new List<KuwaitGoldPrice>();
            var now = DateTime.UtcNow;
            
            // ═══════════════════════════════════════════════════════════════════
            // جلب سعر الأونصة العالمي الحالي من قاعدة البيانات
            // ═══════════════════════════════════════════════════════════════════
            var globalPrice = await _globalPriceRepository.GetLatestAsync();
            
            decimal globalOuncePrice;
            if (globalPrice != null && globalPrice.Close > 0)
            {
                globalOuncePrice = globalPrice.Close;
                _logger.LogInformation("حساب أسعار الكويت من السعر العالمي: ${Price}/oz", globalOuncePrice);
            }
            else
            {
                // قيمة افتراضية في حالة عدم توفر سعر عالمي
                globalOuncePrice = 2625.85m;
                _logger.LogWarning("لا يتوفر سعر عالمي، استخدام القيمة الافتراضية: ${Price}/oz", globalOuncePrice);
            }
            
            // ═══════════════════════════════════════════════════════════════════
            // جلب آخر سعر لحساب التغير
            // ═══════════════════════════════════════════════════════════════════
            var previousPrices = (await _repository.GetLatestAllKaratsAsync()).ToDictionary(p => p.Karat, p => p.BuyPrice);
            
            foreach (var karat in Karats)
            {
                // ═══════════════════════════════════════════════════════════════════
                // استخدام المعادلة المركزية الموحدة من GoldPriceCalculator
                // لا هامش تاجر - السعر الخام فقط
                // ═══════════════════════════════════════════════════════════════════
                var gramPrice = Helpers.GoldPriceCalculator.CalculateGramPrice(
                    globalOuncePrice, 
                    karat,
                    Helpers.GoldPriceCalculator.USD_TO_KWD
                );
                
                // سعر الشراء = سعر البيع = السعر الخام (بدون هامش)
                var buyPrice = gramPrice;
                var sellPrice = gramPrice;
                
                // ═══════════════════════════════════════════════════════════════════
                // حساب التغير مقارنة بالسعر السابق
                // ═══════════════════════════════════════════════════════════════════
                decimal changeValue = 0;
                decimal changePercent = 0;
                
                if (previousPrices.ContainsKey(karat) && previousPrices[karat] > 0)
                {
                    var previousPrice = previousPrices[karat];
                    changeValue = Math.Round(buyPrice - previousPrice, 3);
                    changePercent = Math.Round((changeValue / previousPrice) * 100, 2);
                }
                
                prices.Add(new KuwaitGoldPrice
                {
                    Id = Guid.NewGuid(),
                    Timestamp = now,
                    Karat = karat,
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    ChangePercent = changePercent,
                    ChangeValue = changeValue,
                    Source = "Calculated", // محسوب من GoldPriceCalculator
                    CreatedAt = now
                });
            }
            
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب أسعار الكويت");
            return new List<KuwaitGoldPrice>();
        }
    }

    /// <summary>
    /// جلب آخر أسعار من المخزن المؤقت أو API
    /// </summary>
    public async Task<List<KuwaitGoldPrice>> GetLatestPricesAsync()
    {
        // جلب آخر الأسعار من قاعدة البيانات
        var latestFromDb = (await _repository.GetLatestAllKaratsAsync()).ToList();
        
        // إذا لم يمض أكثر من 5 دقائق على آخر تحديث، استخدم القيم المخزنة
        if (latestFromDb.Any() && 
            (DateTime.UtcNow - latestFromDb.First().Timestamp).TotalSeconds < _settings.KuwaitGoldApi.RefreshIntervalSeconds)
        {
            return latestFromDb;
        }
        
        // جلب أسعار جديدة
        var freshPrices = await FetchCurrentPricesAsync();
        return freshPrices.Any() ? freshPrices : latestFromDb;
    }

    /// <summary>
    /// جلب سعر عيار محدد
    /// </summary>
    public async Task<KuwaitGoldPrice?> GetPriceByKaratAsync(int karat)
    {
        if (!Karats.Contains(karat))
        {
            _logger.LogWarning("عيار غير صالح: {Karat}", karat);
            return null;
        }
        
        return await _repository.GetLatestByKaratAsync(karat);
    }
}

/// <summary>
/// نموذج استجابة API أسعار الكويت
/// </summary>
public class KuwaitGoldApiResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("prices")]
    public List<KuwaitKaratPrice> Prices { get; set; } = new();
}

/// <summary>
/// سعر عيار واحد
/// </summary>
public class KuwaitKaratPrice
{
    [JsonPropertyName("karat")]
    public int Karat { get; set; }
    
    [JsonPropertyName("buy")]
    public decimal BuyPrice { get; set; }
    
    [JsonPropertyName("sell")]
    public decimal SellPrice { get; set; }
    
    [JsonPropertyName("change")]
    public decimal Change { get; set; }
    
    [JsonPropertyName("change_percent")]
    public decimal ChangePercent { get; set; }
}
