using Microsoft.EntityFrameworkCore;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.Data;

namespace MrGoldenBader.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MarketReport?> GetByIdAsync(Guid id)
    {
        return await _context.MarketReports.FindAsync(id);
    }

    public async Task<List<MarketReport>> GetReportsAsync(ReportPeriod? period, int count = 10)
    {
        var query = _context.MarketReports.AsQueryable();

        if (period.HasValue)
        {
            query = query.Where(r => r.Period == period.Value);
        }

        return await query
            .OrderByDescending(r => r.DateTo)
            .Take(count)
            .ToListAsync();
    }

    public async Task<MarketReport> AddAsync(MarketReport report)
    {
        await _context.MarketReports.AddAsync(report);
        return report;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
