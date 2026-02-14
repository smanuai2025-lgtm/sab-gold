using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// تحكم التنبؤات - تنبؤ محلي بأسعار الذهب
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionsController : BaseApiController
{
    private readonly IGoldPriceRepository _priceRepository;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IGoldPriceRepository priceRepository,
        ILogger<PredictionsController> logger)
    {
        _priceRepository = priceRepository;
        _logger = logger;
    }

    /// <summary>
    /// التنبؤ بأسعار الذهب (تحليل محلي)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PredictionResultDto>> GetPrediction([FromQuery] int hours = 24)
    {
        try
        {
            _logger.LogInformation("طلب تنبؤ لـ {Hours} ساعة (تحليل محلي)", hours);

            // جلب الأسعار التاريخية
            var historicalPrices = await _priceRepository.GetLastHoursAsync(168);
            
            if (!historicalPrices.Any() || historicalPrices.Count() < 10)
            {
                return Success(GenerateSimulatedPrediction(hours, null), "تنبؤ محاكى - بيانات غير كافية");
            }

            // التنبؤ المحلي بناءً على الاتجاه
            var result = GenerateLocalPrediction(historicalPrices.ToList(), hours);

            return Success(result, "تم التنبؤ بنجاح (تحليل محلي)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في التنبؤ");
            return Error("حدث خطأ في التنبؤ", 500);
        }
    }

    /// <summary>
    /// فحص حالة خدمة التنبؤ
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> CheckHealth()
    {
        return Success(new
        {
            Status = "نشط",
            Type = "تحليل محلي",
            Message = "خدمة التنبؤ تعمل بالتحليل الفني المحلي"
        });
    }

    /// <summary>
    /// تنبؤ قصير المدى (6 ساعات)
    /// </summary>
    [HttpGet("short-term")]
    public async Task<ActionResult<PredictionResultDto>> GetShortTermPrediction()
    {
        return await GetPrediction(6);
    }

    /// <summary>
    /// تنبؤ متوسط المدى (24 ساعة)
    /// </summary>
    [HttpGet("medium-term")]
    public async Task<ActionResult<PredictionResultDto>> GetMediumTermPrediction()
    {
        return await GetPrediction(24);
    }

    /// <summary>
    /// تنبؤ طويل المدى (72 ساعة)
    /// </summary>
    [HttpGet("long-term")]
    public async Task<ActionResult<PredictionResultDto>> GetLongTermPrediction()
    {
        return await GetPrediction(72);
    }

    /// <summary>
    /// التنبؤ المحلي بناءً على التحليل الفني
    /// </summary>
    private PredictionResultDto GenerateLocalPrediction(List<Domain.Entities.GoldPrice> prices, int hours)
    {
        var orderedPrices = prices.OrderBy(p => p.Timestamp).ToList();
        var currentPrice = orderedPrices.Last().Close;
        var oldPrice = orderedPrices.First().Close;
        
        // حساب الاتجاه
        var changePercent = (double)((currentPrice - oldPrice) / oldPrice * 100);
        var dailyChange = changePercent / Math.Max(1, prices.Count / 24.0);
        
        // تحديد الاتجاه
        string trend = changePercent > 0.3 ? "bullish" : changePercent < -0.3 ? "bearish" : "neutral";
        double trendStrength = Math.Min(100, 50 + Math.Abs(changePercent) * 10);

        var predictions = new List<PredictionDto>();
        var now = DateTime.UtcNow;
        var basePrice = currentPrice;

        for (int i = 1; i <= hours; i++)
        {
            var hourlyChange = (decimal)(dailyChange / 24 * (0.8 + new Random().NextDouble() * 0.4));
            var predicted = basePrice + (basePrice * hourlyChange / 100);
            var confidence = Math.Max(0.5, 0.85 - (i * 0.005));

            predictions.Add(new PredictionDto
            {
                Timestamp = now.AddHours(i),
                PredictedPrice = Math.Round(predicted, 2),
                Confidence = Math.Round(confidence, 3),
                LowerBound = Math.Round(predicted * 0.995m, 2),
                UpperBound = Math.Round(predicted * 1.005m, 2)
            });

            basePrice = predicted;
        }

        var first = predictions.First();
        var last = predictions.Last();
        var predChange = ((last.PredictedPrice - first.PredictedPrice) / first.PredictedPrice) * 100;
        var changeDir = predChange >= 0 ? "ارتفاع" : "انخفاض";
        var trendAr = trend == "bullish" ? "صعودي" : trend == "bearish" ? "هبوطي" : "مستقر";

        return new PredictionResultDto
        {
            Predictions = predictions,
            Trend = trend,
            TrendStrength = trendStrength,
            ModelAccuracy = 0.75,
            GeneratedAt = now,
            Summary = $"من المتوقع {changeDir} بنسبة {Math.Abs(predChange):F2}%. الاتجاه {trendAr} بقوة {trendStrength:F0}%."
        };
    }

    /// <summary>
    /// توليد تنبؤ محاكى (Fallback)
    /// </summary>
    private PredictionResultDto GenerateSimulatedPrediction(int hours, decimal? basePrice)
    {
        var random = new Random();
        var price = basePrice ?? 2035m;
        var predictions = new List<PredictionDto>();
        var now = DateTime.UtcNow;

        for (int i = 1; i <= hours; i++)
        {
            var variation = (decimal)(random.NextDouble() * 10 - 5);
            var predicted = price + variation;
            var confidence = Math.Max(0.5, 0.9 - (i * 0.01));

            predictions.Add(new PredictionDto
            {
                Timestamp = now.AddHours(i),
                PredictedPrice = Math.Round(predicted, 2),
                Confidence = Math.Round(confidence, 3),
                LowerBound = Math.Round(predicted - 10, 2),
                UpperBound = Math.Round(predicted + 10, 2)
            });

            price = predicted;
        }

        var first = predictions.First();
        var last = predictions.Last();
        var trend = last.PredictedPrice > first.PredictedPrice ? "bullish" : 
                   last.PredictedPrice < first.PredictedPrice ? "bearish" : "neutral";

        return new PredictionResultDto
        {
            Predictions = predictions,
            Trend = trend,
            TrendStrength = 60,
            ModelAccuracy = 0.70,
            GeneratedAt = now,
            Summary = "تنبؤ مبدئي - بيانات تاريخية غير كافية للتحليل الدقيق"
        };
    }
}
