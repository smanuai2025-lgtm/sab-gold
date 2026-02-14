using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// وحدة تحكم التنبيهات
/// إدارة الإشعارات والتنبيهات اللحظية
/// </summary>
[AllowAnonymous]
public class AlertsController : BaseApiController
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;
    
    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }
    
    /// <summary>
    /// جلب التنبيهات غير المقروءة
    /// </summary>
    [HttpGet("unread")]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetUnreadAlerts()
    {
        try
        {
            var alerts = await _alertService.GetUnreadAlertsAsync();
            return Success(alerts, "تم جلب التنبيهات غير المقروءة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب التنبيهات");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// جلب جميع التنبيهات
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAllAlerts([FromQuery] int count = 50)
    {
        try
        {
            var alerts = await _alertService.GetAllAlertsAsync(count);
            return Success(alerts, $"تم جلب آخر {count} تنبيه");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب التنبيهات");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// جلب عدد التنبيهات غير المقروءة
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var count = await _alertService.GetUnreadCountAsync();
            return Success(count, $"عدد التنبيهات غير المقروءة: {count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في عد التنبيهات");
            return Error("حدث خطأ في جلب البيانات", 500);
        }
    }
    
    /// <summary>
    /// تعليم تنبيه كمقروء
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<ActionResult<bool>> MarkAsRead(Guid id)
    {
        try
        {
            await _alertService.MarkAsReadAsync(id);
            return Success(true, "تم تعليم التنبيه كمقروء");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تعليم التنبيه");
            return Error("حدث خطأ في تحديث البيانات", 500);
        }
    }
    
    /// <summary>
    /// تعليم جميع التنبيهات كمقروءة
    /// </summary>
    [HttpPost("read-all")]
    public async Task<ActionResult<bool>> MarkAllAsRead()
    {
        try
        {
            await _alertService.MarkAllAsReadAsync();
            return Success(true, "تم تعليم جميع التنبيهات كمقروءة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تعليم التنبيهات");
            return Error("حدث خطأ في تحديث البيانات", 500);
        }
    }
}
