using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Data.Repositories;

/// <summary>
/// تنفيذ مستودع أسعار الذهب في الكويت
/// </summary>
public class KuwaitGoldPriceRepository : Repository<KuwaitGoldPrice>, IKuwaitGoldPriceRepository
{
    public KuwaitGoldPriceRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    public async Task<IEnumerable<KuwaitGoldPrice>> GetLatestAllKaratsAsync()
    {
        // جلب آخر سعر لكل عيار
        var karats = new[] { 24, 22, 21, 18 };
        var result = new List<KuwaitGoldPrice>();
        
        foreach (var karat in karats)
        {
            var price = await _dbSet
                .Where(p => p.Karat == karat)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync();
            
            if (price != null)
            {
                result.Add(price);
            }
        }
        
        return result;
    }
    
    public async Task<KuwaitGoldPrice?> GetLatestByKaratAsync(int karat)
    {
        return await _dbSet
            .Where(p => p.Karat == karat)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<KuwaitGoldPrice>> GetByDateRangeAndKaratAsync(DateTime from, DateTime to, int karat)
    {
        return await _dbSet
            .Where(p => p.Karat == karat && p.Timestamp >= from && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
    }
}
