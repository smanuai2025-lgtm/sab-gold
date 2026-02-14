using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Infrastructure.Helpers;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// وحدة تحكم أسعار الذهب
/// تتعامل مع الأسعار العالمية والمحلية في الكويت
/// </summary>
[AllowAnonymous]
public class PricesController : BaseApiController
{
    private readonly IGoldPriceService _goldPriceService;
    private readonly ILogger<PricesController> _logger;
    
    public PricesController(IGoldPriceService goldPriceService, ILogger<PricesController> logger)
    {
        _goldPriceService = goldPriceService;
        _logger = logger;
    }
    
    /// <summary>
    /// جلب السعر العالمي الحالي للأونصة
    /// </summary>
    [HttpGet("global")]
    public async Task<ActionResult<GoldPriceDto>> GetGlobalPrice()
    {
        try
        {
            var price = await _goldPriceService.GetCurrentGlobalPriceAsync();
            if (price == null)
            {
                return Error("لا توجد بيانات أسعار متاحة حالياً", 404);
            }
            return Success(price, "تم جلب السعر العالمي بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب السعر العالمي");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// جلب الأسعار العالمية التاريخية
    /// </summary>
    [HttpGet("global/history")]
    public async Task<ActionResult<IEnumerable<GoldPriceDto>>> GetGlobalPriceHistory([FromQuery] int hours = 24)
    {
        try
        {
            var prices = await _goldPriceService.GetGlobalPriceHistoryAsync(hours);
            return Success(prices, $"تم جلب أسعار آخر {hours} ساعة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأسعار التاريخية");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// جلب أسعار الكويت لجميع العيارات
    /// </summary>
    [HttpGet("kuwait")]
    public async Task<ActionResult<IEnumerable<KuwaitGoldPriceDto>>> GetKuwaitPrices()
    {
        try
        {
            var prices = await _goldPriceService.GetCurrentKuwaitPricesAsync();
            return Success(prices, "تم جلب أسعار الكويت بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب أسعار الكويت");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// جلب سعر الكويت لعيار محدد
    /// </summary>
    [HttpGet("kuwait/{karat}")]
    public async Task<ActionResult<KuwaitGoldPriceDto>> GetKuwaitPriceByKarat(int karat)
    {
        try
        {
            // استخدام المعادلة المركزية للتحقق من صحة العيار
            if (!GoldPriceCalculator.IsValidKarat(karat))
            {
                return Error($"العيار {karat} غير صالح. العيارات المتاحة: {string.Join(", ", GoldPriceCalculator.KaratRatios.Keys)}");
            }
            
            var price = await _goldPriceService.GetKuwaitPriceByKaratAsync(karat);
            if (price == null)
            {
                return Error($"لا توجد بيانات لعيار {karat}", 404);
            }
            return Success(price, $"تم جلب سعر عيار {karat} بنجاح");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب سعر عيار {Karat}", karat);
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
}
