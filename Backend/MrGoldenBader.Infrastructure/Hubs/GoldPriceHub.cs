using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MrGoldenBader.Infrastructure.Hubs;

/// <summary>
/// مركز SignalR للتحديثات اللحظية
/// يستخدم لبث أسعار الذهب والتنبيهات في الوقت الفعلي
/// </summary>
public class GoldPriceHub : Hub
{
    private readonly ILogger<GoldPriceHub> _logger;
    
    public GoldPriceHub(ILogger<GoldPriceHub> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// عند اتصال العميل
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("عميل متصل جديد: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// عند انقطاع الاتصال
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("عميل انقطع: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// الاشتراك في تحديثات الأسعار
    /// </summary>
    public async Task SubscribeToPriceUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PriceUpdates");
        _logger.LogInformation("اشتراك في تحديثات الأسعار: {ConnectionId}", Context.ConnectionId);
    }
    
    /// <summary>
    /// الاشتراك في التنبيهات
    /// </summary>
    public async Task SubscribeToAlerts()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Alerts");
        _logger.LogInformation("اشتراك في التنبيهات: {ConnectionId}", Context.ConnectionId);
    }
    
    /// <summary>
    /// إلغاء الاشتراك في تحديثات الأسعار
    /// </summary>
    public async Task UnsubscribeFromPriceUpdates()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "PriceUpdates");
    }
    
    /// <summary>
    /// إلغاء الاشتراك في التنبيهات
    /// </summary>
    public async Task UnsubscribeFromAlerts()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Alerts");
    }
}
