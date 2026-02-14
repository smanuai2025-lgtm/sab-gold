using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Data.Repositories;

/// <summary>
/// تنفيذ مستودع التوصيات
/// </summary>
public class RecommendationRepository : Repository<Recommendation>, IRecommendationRepository
{
    public RecommendationRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    public async Task<Recommendation?> GetActiveAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive && (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<Recommendation>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Recommendation>> GetSuccessfulAsync()
    {
        return await _dbSet
            .Where(r => r.WasSuccessful == true)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<decimal> GetSuccessRateAsync()
    {
        var total = await _dbSet.CountAsync(r => r.WasSuccessful.HasValue);
        if (total == 0) return 0;
        
        var successful = await _dbSet.CountAsync(r => r.WasSuccessful == true);
        return (decimal)successful / total * 100;
    }
}
