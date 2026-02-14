using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// تحكم التوصيات - إدارة توصيات التداول
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : BaseApiController
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// جلب التوصية النشطة الحالية
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<RecommendationDto>> GetActiveRecommendation()
    {
        try
        {
            var recommendation = await _recommendationService.GetActiveRecommendationAsync();
            
            if (recommendation == null)
            {
                return Success<RecommendationDto?>(null, "لا توجد توصية نشطة حالياً");
            }

            return Success(recommendation, "التوصية النشطة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب التوصية النشطة");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// جلب آخر التوصيات
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecommendationDto>>> GetLatestRecommendations([FromQuery] int count = 10)
    {
        try
        {
            var recommendations = await _recommendationService.GetLatestRecommendationsAsync(count);
            return Success(recommendations, $"آخر {count} توصيات");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب التوصيات");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// توليد توصية جديدة (يدوي)
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<RecommendationDto>> GenerateRecommendation()
    {
        try
        {
            _logger.LogInformation("طلب توليد توصية يدوي");
            
            var recommendation = await _recommendationService.GenerateRecommendationAsync();
            
            if (recommendation == null)
            {
                return Error("لم يتمكن النظام من توليد توصية", 400);
            }

            return Success(recommendation, "تم توليد التوصية بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في توليد التوصية");
            return Error("حدث خطأ في توليد التوصية", 500);
        }
    }

    /// <summary>
    /// جلب نسبة نجاح التوصيات
    /// </summary>
    [HttpGet("success-rate")]
    public async Task<ActionResult<object>> GetSuccessRate()
    {
        try
        {
            var successRate = await _recommendationService.GetSuccessRateAsync();
            return Success(new
            {
                SuccessRate = successRate,
                Description = GetSuccessRateDescription(successRate)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حساب نسبة النجاح");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// تحديث نتيجة توصية
    /// </summary>
    [HttpPut("{id}/result")]
    public async Task<ActionResult<object>> UpdateResult(Guid id, [FromBody] UpdateResultRequest request)
    {
        try
        {
            await _recommendationService.UpdateRecommendationResultAsync(
                id, request.WasSuccessful, request.ActualPrice);
            
            return Success(new { Message = "تم تحديث النتيجة" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تحديث النتيجة");
            return Error("حدث خطأ", 500);
        }
    }

    /// <summary>
    /// إحصائيات التوصيات
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        try
        {
            var recommendations = await _recommendationService.GetLatestRecommendationsAsync(100);
            var list = recommendations.ToList();

            var stats = new
            {
                TotalRecommendations = list.Count,
                BuyCount = list.Count(r => r.Type == "buy"),
                SellCount = list.Count(r => r.Type == "sell"),
                HoldCount = list.Count(r => r.Type == "hold"),
                AverageConfidence = list.Any() ? list.Average(r => r.ConfidenceScore) : 0,
                SuccessRate = await _recommendationService.GetSuccessRateAsync(),
                LastRecommendation = list.FirstOrDefault()?.CreatedAt
            };

            return Success(stats, "إحصائيات التوصيات");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الإحصائيات");
            return Error("حدث خطأ", 500);
        }
    }

    private static string GetSuccessRateDescription(double rate)
    {
        return rate switch
        {
            >= 80 => "ممتاز 🌟",
            >= 60 => "جيد جداً ✅",
            >= 40 => "متوسط ⚠️",
            _ => "يحتاج تحسين 📊"
        };
    }
}

public class UpdateResultRequest
{
    public bool WasSuccessful { get; set; }
    public decimal ActualPrice { get; set; }
}
