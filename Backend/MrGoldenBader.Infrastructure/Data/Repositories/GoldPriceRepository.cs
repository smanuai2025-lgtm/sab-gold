using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Data.Repositories;

/// <summary>
/// تنفيذ مستودع أسعار الذهب العالمية
/// </summary>
public class GoldPriceRepository : Repository<GoldPrice>, IGoldPriceRepository
{
    public GoldPriceRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    public async Task<GoldPrice?> GetLatestAsync()
    {
        return await _dbSet
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<GoldPrice>> GetByDateRangeAsync(DateTime from, DateTime to, string timeFrame = "Hour")
    {
        return await _dbSet
            .Where(p => p.Timestamp >= from && p.Timestamp <= to && p.TimeFrame == timeFrame)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<GoldPrice>> GetLastHoursAsync(int hours)
    {
        var from = DateTime.UtcNow.AddHours(-hours);
        return await _dbSet
            .Where(p => p.Timestamp >= from)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<GoldPrice>> GetLastDaysAsync(int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        return await _dbSet
            .Where(p => p.Timestamp >= from && p.TimeFrame == "Day")
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
    }
}
