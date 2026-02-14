using MrGoldenBader.Application.DTOs;

namespace MrGoldenBader.Application.Interfaces;

/// <summary>
/// واجهة خدمة التنبيهات
/// مسؤولة عن إدارة وإرسال التنبيهات
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// جلب التنبيهات غير المقروءة
    /// </summary>
    Task<IEnumerable<AlertDto>> GetUnreadAlertsAsync();
    
    /// <summary>
    /// جلب جميع التنبيهات
    /// </summary>
    Task<IEnumerable<AlertDto>> GetAllAlertsAsync(int count = 50);
    
    /// <summary>
    /// جلب عدد التنبيهات غير المقروءة
    /// </summary>
    Task<int> GetUnreadCountAsync();
    
    /// <summary>
    /// تعليم تنبيه كمقروء
    /// </summary>
    Task MarkAsReadAsync(Guid alertId);
    
    /// <summary>
    /// تعليم جميع التنبيهات كمقروءة
    /// </summary>
    Task MarkAllAsReadAsync();
    
    /// <summary>
    /// إنشاء تنبيه جديد
    /// </summary>
    Task<AlertDto> CreateAlertAsync(string type, string title, string message, string severity);
}
