using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

/// <summary>
/// واجهة المستودع العام - تحتوي على العمليات الأساسية
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// جلب كيان بالمعرف
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// جلب جميع الكيانات
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// إضافة كيان جديد
    /// </summary>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// تحديث كيان
    /// </summary>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// حذف كيان
    /// </summary>
    Task DeleteAsync(Guid id);
    
    /// <summary>
    /// حفظ التغييرات
    /// </summary>
    Task<int> SaveChangesAsync();
}
