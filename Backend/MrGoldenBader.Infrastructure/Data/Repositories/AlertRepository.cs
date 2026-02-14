using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Data.Repositories;

/// <summary>
/// تنفيذ مستودع التنبيهات
/// </summary>
public class AlertRepository : Repository<Alert>, IAlertRepository
{
    public AlertRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    public async Task<IEnumerable<Alert>> GetUnreadAsync()
    {
        return await _dbSet
            .Where(a => !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Alert>> GetByTypeAsync(string type)
    {
        return await _dbSet
            .Where(a => a.Type == type)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    
    public async Task MarkAsReadAsync(Guid id)
    {
        var alert = await GetByIdAsync(id);
        if (alert != null)
        {
            alert.IsRead = true;
            alert.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }
    }
    
    public async Task MarkAllAsReadAsync()
    {
        var unreadAlerts = await _dbSet.Where(a => !a.IsRead).ToListAsync();
        foreach (var alert in unreadAlerts)
        {
            alert.IsRead = true;
            alert.UpdatedAt = DateTime.UtcNow;
        }
        await SaveChangesAsync();
    }
    
    public async Task<int> GetUnreadCountAsync()
    {
        return await _dbSet.CountAsync(a => !a.IsRead);
    }
    
    public async Task<IEnumerable<Alert>> GetRecentAsync(int count)
    {
        return await _dbSet
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
