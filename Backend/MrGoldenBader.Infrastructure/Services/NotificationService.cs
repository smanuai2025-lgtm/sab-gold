using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Infrastructure.Hubs;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة التنبيهات المركزية
/// تدير إنشاء وإرسال التنبيهات اللحظية لجميع أجزاء النظام
/// </summary>
public class NotificationService
{
    private readonly IAlertRepository _alertRepo;
    private readonly IHubContext<GoldPriceHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IAlertRepository alertRepo,
        IHubContext<GoldPriceHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _alertRepo = alertRepo;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// إرسال تنبيه تغير الأسعار
    /// </summary>
    public async Task NotifyPriceChangeAsync(string type, decimal price, decimal change, string trend)
    {
        string icon = trend == "up" ? "📈" : "📉";
        string color = trend == "up" ? "green" : "red";
        string title = $"{icon} حركة {type}";
        string message = $"سجل {type} تغيراً {(trend == "up" ? "إيجابياً" : "سلبياً")} بقيمة {change}. السعر الحالي: {price}";

        await CreateAndBroadcastAlertAsync("Price", title, message, "Medium", new 
        { 
            Price = price, 
            Change = change, 
            Trend = trend,
            Color = color,
            Sound = "price_alert"
        });
    }

    /// <summary>
    /// إرسال تنبيه خبر عاجل
    /// </summary>
    public async Task NotifyUrgentNewsAsync(string title, string impactType, double score)
    {
        string icon = "📰";
        string severity = score >= 80 ? "Critical" : "High";
        string impactText = impactType == "bullish" ? "إيجابي قوي" : impactType == "bearish" ? "سلبي قوي" : "هام";
        
        await CreateAndBroadcastAlertAsync("News", $"{icon} خبر عاجل: {impactText}", title, severity, new
        {
            Impact = impactType,
            Score = score,
            Sound = "news_alert"
        });
    }

    /// <summary>
    /// إرسال تنبيه توصية جديدة
    /// </summary>
    public async Task NotifyRecommendationAsync(string type, string timeHorizon)
    {
        string icon = "💡";
        string title = type == "Buy" ? $"{icon} 💎 فرصة شراء قوية" : type == "Sell" ? $"{icon} 🔻 إشارة بيع" : $"{icon} ⏳ تحديث التوصية";
        string message = $"تم إصدار توصية {type} جديدة للمدى {timeHorizon}. تفقد التفاصيل الآن!";
        
        await CreateAndBroadcastAlertAsync("Recommendation", title, message, "Critical", new
        {
            Type = type,
            Horizon = timeHorizon,
            Sound = "recommendation_alert"
        });
    }

    /// <summary>
    /// إرسال تنبيه اقتراب من الهدف
    /// </summary>
    public async Task NotifyTargetApproachAsync(decimal currentPrice, decimal targetPrice, string type)
    {
        string icon = "🎯";
        await CreateAndBroadcastAlertAsync("Target", $"{icon} اقتراب من الهدف", 
            $"السعر الحالي {currentPrice} يقترب من هدف {type} ({targetPrice})! كن مستعداً.", "High", new
            {
                Sound = "target_alert"
            });
    }

    /// <summary>
    /// إنشاء وإذاعة التنبيه
    /// </summary>
    private async Task CreateAndBroadcastAlertAsync(string type, string title, string message, string severity, object metadata)
    {
        try
        {
            // 1. الحفظ في قاعدة البيانات
            var alert = new Alert
            {
                Type = type,
                Title = title,
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsSent = true,
                SentAt = DateTime.UtcNow
            };

            await _alertRepo.AddAsync(alert);
            await _alertRepo.SaveChangesAsync();

            // 2. البث عبر SignalR
            var alertDto = new NotificationAlert
            {
                Id = alert.Id,
                Type = type,
                Title = title,
                Message = message,
                Severity = severity,
                Metadata = metadata,
                CreatedAt = alert.CreatedAt
            };

            await _hubContext.Clients.All.SendAsync("ReceiveAlert", alertDto);
            
            _logger.LogInformation("تم إرسال تنبيه: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في إرسال التنبيه");
        }
    }
}

public class NotificationAlert : Alert
{
    public object? Metadata { get; set; }
}
