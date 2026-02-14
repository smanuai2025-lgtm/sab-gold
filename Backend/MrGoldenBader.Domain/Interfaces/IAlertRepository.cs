using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

/// <summary>
/// واجهة مستودع التنبيهات
/// </summary>
public interface IAlertRepository : IRepository<Alert>
{
    /// <summary>
    /// جلب التنبيهات غير المقروءة
    /// </summary>
    Task<IEnumerable<Alert>> GetUnreadAsync();
    
    /// <summary>
    /// جلب التنبيهات حسب النوع
    /// </summary>
    Task<IEnumerable<Alert>> GetByTypeAsync(string type);
    
    /// <summary>
    /// تعليم التنبيه كمقروء
    /// </summary>
    Task MarkAsReadAsync(Guid id);
    
    /// <summary>
    /// تعليم جميع التنبيهات كمقروءة
    /// </summary>
    Task MarkAllAsReadAsync();
    
    /// <summary>
    /// جلب عدد التنبيهات غير المقروءة
    /// </summary>
    Task<int> GetUnreadCountAsync();
    
    /// <summary>
    /// جلب أحدث التنبيهات
    /// </summary>
    Task<IEnumerable<Alert>> GetRecentAsync(int count);
}
