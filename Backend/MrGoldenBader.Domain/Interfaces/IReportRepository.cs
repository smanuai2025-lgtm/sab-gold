using MrGoldenBader.Domain.Entities;

namespace MrGoldenBader.Domain.Interfaces;

public interface IReportRepository
{
    Task<MarketReport?> GetByIdAsync(Guid id);
    Task<List<MarketReport>> GetReportsAsync(ReportPeriod? period, int count = 10);
    Task<MarketReport> AddAsync(MarketReport report);
    Task SaveChangesAsync();
}
