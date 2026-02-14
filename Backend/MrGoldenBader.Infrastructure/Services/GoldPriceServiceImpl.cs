using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.ExternalApis;
using MrGoldenBader.Infrastructure.Hubs;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// واجهة Hub للأسعار - للاستخدام المشترك
/// </summary>
public interface IGoldPriceHub
{
    Task ReceiveGlobalPrice(GoldPriceDto price);
    Task ReceiveKuwaitPrices(List<KuwaitGoldPriceDto> prices);
    Task ReceiveAlert(AlertDto alert);
}

/// <summary>
/// تنفيذ خدمة أسعار الذهب
/// تجمع بين الأسعار العالمية وأسعار الكويت مع البث اللحظي
/// </summary>
public class GoldPriceServiceImpl : IGoldPriceService
{
    private readonly GlobalGoldPriceService _globalService;
    private readonly KuwaitGoldPriceService _kuwaitService;
    private readonly IGoldPriceRepository _globalRepository;
    private readonly IKuwaitGoldPriceRepository _kuwaitRepository;
    private readonly IHubContext<GoldPriceHub> _hubContext;
    private readonly ILogger<GoldPriceServiceImpl> _logger;

    public GoldPriceServiceImpl(
        GlobalGoldPriceService globalService,
        KuwaitGoldPriceService kuwaitService,
        IGoldPriceRepository globalRepository,
        IKuwaitGoldPriceRepository kuwaitRepository,
        IHubContext<GoldPriceHub> hubContext,
        ILogger<GoldPriceServiceImpl> logger)
    {
        _globalService = globalService;
        _kuwaitService = kuwaitService;
        _globalRepository = globalRepository;
        _kuwaitRepository = kuwaitRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// جلب السعر العالمي الحالي
    /// </summary>
    public async Task<GoldPriceDto?> GetCurrentGlobalPriceAsync()
    {
        var price = await _globalService.GetLatestPriceAsync();
        return price == null ? null : MapToDto(price);
    }

    /// <summary>
    /// جلب الأسعار العالمية التاريخية
    /// </summary>
    public async Task<IEnumerable<GoldPriceDto>> GetGlobalPriceHistoryAsync(int hours = 24)
    {
        var prices = await _globalRepository.GetLastHoursAsync(hours);
        return prices.Select(MapToDto);
    }

    /// <summary>
    /// جلب أسعار الكويت الحالية
    /// </summary>
    public async Task<IEnumerable<KuwaitGoldPriceDto>> GetCurrentKuwaitPricesAsync()
    {
        var prices = await _kuwaitService.GetLatestPricesAsync();
        return prices.Select(MapToKuwaitDto);
    }

    /// <summary>
    /// جلب سعر عيار محدد في الكويت
    /// </summary>
    public async Task<KuwaitGoldPriceDto?> GetKuwaitPriceByKaratAsync(int karat)
    {
        var price = await _kuwaitService.GetPriceByKaratAsync(karat);
        return price == null ? null : MapToKuwaitDto(price);
    }

    /// <summary>
    /// تحديث الأسعار من المصادر الخارجية وبثها
    /// </summary>
    public async Task RefreshPricesAsync()
    {
        _logger.LogInformation("بدء تحديث الأسعار...");
        
        try
        {
            // جلب السعر العالمي
            var globalPrice = await _globalService.FetchCurrentPriceAsync();
            if (globalPrice != null)
            {
                var globalDto = MapToDto(globalPrice);
                await _hubContext.Clients.Group("PriceUpdates")
                    .SendAsync("ReceiveGlobalPrice", globalDto);
                
                _logger.LogInformation("تم بث السعر العالمي: {Price} USD", globalPrice.Close);
            }
            
            // جلب أسعار الكويت
            var kuwaitPrices = await _kuwaitService.FetchCurrentPricesAsync();
            if (kuwaitPrices.Any())
            {
                var kuwaitDtos = kuwaitPrices.Select(MapToKuwaitDto).ToList();
                await _hubContext.Clients.Group("PriceUpdates")
                    .SendAsync("ReceiveKuwaitPrices", kuwaitDtos);
                
                _logger.LogInformation("تم بث {Count} أسعار للكويت", kuwaitPrices.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تحديث الأسعار");
        }
    }

    /// <summary>
    /// تحويل سعر الذهب العالمي إلى DTO
    /// </summary>
    private static GoldPriceDto MapToDto(GoldPrice price)
    {
        // حساب التغير من السعر السابق (للعرض)
        var changeValue = price.Close - price.Open;
        var changePercent = price.Open > 0 ? (changeValue / price.Open) * 100 : 0;
        
        return new GoldPriceDto
        {
            Timestamp = price.Timestamp,
            Open = price.Open,
            High = price.High,
            Low = price.Low,
            Close = price.Close,
            ChangeValue = Math.Round(changeValue, 2),
            ChangePercent = Math.Round(changePercent, 2),
            TimeFrame = price.TimeFrame
        };
    }

    /// <summary>
    /// تحويل سعر الكويت إلى DTO
    /// </summary>
    private static KuwaitGoldPriceDto MapToKuwaitDto(KuwaitGoldPrice price)
    {
        return new KuwaitGoldPriceDto
        {
            Timestamp = price.Timestamp,
            Karat = price.Karat,
            BuyPrice = price.BuyPrice,
            SellPrice = price.SellPrice,
            ChangePercent = price.ChangePercent ?? 0,
            ChangeValue = price.ChangeValue ?? 0
        };
    }
}

/// <summary>
/// Marker class لـ SignalR Hub
/// Hub الفعلي موجود في API project
/// </summary>
public class GoldPriceHubMarker : Hub
{
}

