using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة التقارير - توليد محلي بدون AI خارجي
/// </summary>
public class ReportService
{
    private readonly IReportRepository _reportRepo;
    private readonly IGoldPriceRepository _priceRepo;
    private readonly NewsIntegrationService _newsService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepo,
        IGoldPriceRepository priceRepo,
        NewsIntegrationService newsService,
        ILogger<ReportService> logger)
    {
        _reportRepo = reportRepo;
        _priceRepo = priceRepo;
        _newsService = newsService;
        _logger = logger;
    }

    /// <summary>
    /// توليد تقرير للفترة المحددة (محلي بدون AI)
    /// </summary>
    public async Task<ReportDto?> GenerateReportAsync(ReportPeriod period)
    {
        try
        {
            // 1. تحديد الفترة الزمنية
            var (startDate, endDate) = GetDateRange(period);
            int hours = (int)(endDate - startDate).TotalHours;

            // 2. جلب بيانات الأسعار
            var prices = await _priceRepo.GetLastHoursAsync(hours);
            
            if (!prices.Any())
            {
                _logger.LogWarning("لا توجد بيانات أسعار للفترة المحددة للتقرير");
                return null;
            }

            var closePrice = prices.First().Close;
            var open = prices.Last().Open;
            var high = prices.Max(p => p.High);
            var low = prices.Min(p => p.Low);
            var change = closePrice - open;
            var changePercent = (double)(change / open) * 100;

            // 3. جلب الأخبار المهمة
            var newsImpact = await _newsService.GetNewsImpactSummaryAsync();
            var topNews = newsImpact.HighImpactNewsCached.Take(5).Select(n => n.Title).ToList();

            // 4. توليد محتوى التقرير محلياً
            var content = GenerateReportContent(period, open, closePrice, high, low, changePercent, topNews);

            // 5. حفظ التقرير
            var report = new MarketReport
            {
                Period = period,
                DateFrom = startDate,
                DateTo = endDate,
                OpenPrice = open,
                ClosePrice = closePrice,
                HighPrice = high,
                LowPrice = low,
                ChangePercent = changePercent,
                Title = content.Title,
                ExecutiveSummary = content.Summary,
                Recommendations = content.Recommendations,
                FutureOutlook = content.Outlook,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepo.AddAsync(report);
            await _reportRepo.SaveChangesAsync();

            return MapToDto(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في توليد التقرير");
            return null;
        }
    }

    /// <summary>
    /// توليد محتوى التقرير محلياً
    /// </summary>
    private (string Title, string Summary, string Recommendations, string Outlook) GenerateReportContent(
        ReportPeriod period, decimal open, decimal close, decimal high, decimal low, double changePercent, List<string> topNews)
    {
        var periodName = period switch
        {
            ReportPeriod.Daily => "اليومي",
            ReportPeriod.Weekly => "الأسبوعي",
            ReportPeriod.Monthly => "الشهري",
            ReportPeriod.Quarterly => "الربع سنوي",
            ReportPeriod.Annual => "السنوي",
            _ => period.ToString()
        };

        var trend = changePercent > 0 ? "صاعد" : "هابط";
        var title = $"📊 تقرير الذهب {periodName} - اتجاه {trend} ({Math.Abs(changePercent):F2}%)";

        var summary = $@"خلال هذه الفترة، سجل الذهب {(changePercent > 0 ? "ارتفاعاً" : "انخفاضاً")} بنسبة {Math.Abs(changePercent):F2}%.

📈 أداء السوق:
- الافتتاح: ${open:F2}
- الإغلاق: ${close:F2}
- الأعلى: ${high:F2}
- الأدنى: ${low:F2}

📰 أبرز الأخبار:
{(topNews.Any() ? string.Join("\n", topNews.Select(n => $"• {n}")) : "لا توجد أخبار بارزة")}";

        string recommendations;
        if (changePercent > 2)
            recommendations = "🟢 شراء: الاتجاه الصاعد يدعم فتح مراكز شراء مع وقف خسارة مناسب.";
        else if (changePercent < -2)
            recommendations = "🔴 بيع/انتظار: الضغط البيعي واضح. يُنصح بالحذر.";
        else
            recommendations = "🟡 انتظار: السوق متذبذب. يُفضل الانتظار لإشارات أوضح.";

        var outlook = changePercent > 0
            ? $"📈 الاتجاه إيجابي. مقاومة عند ${high:F2}، دعم عند ${low:F2}."
            : $"📉 الاتجاه سلبي. دعم عند ${low:F2}، مقاومة عند ${high:F2}.";

        return (title, summary, recommendations, outlook);
    }

    public async Task<List<ReportDto>> GetReportsAsync(ReportPeriod? period)
    {
        var reports = await _reportRepo.GetReportsAsync(period);
        return reports.Select(MapToDto).ToList();
    }

    private (DateTime Start, DateTime End) GetDateRange(ReportPeriod period)
    {
        var end = DateTime.UtcNow;
        var start = period switch
        {
            ReportPeriod.Daily => end.AddDays(-1),
            ReportPeriod.Weekly => end.AddDays(-7),
            ReportPeriod.Monthly => end.AddMonths(-1),
            ReportPeriod.Quarterly => end.AddMonths(-3),
            ReportPeriod.SemiAnnual => end.AddMonths(-6),
            ReportPeriod.NineMonths => end.AddMonths(-9),
            ReportPeriod.Annual => end.AddYears(-1),
            _ => end.AddDays(-1)
        };
        return (start, end);
    }

    private ReportDto MapToDto(MarketReport r)
    {
        return new ReportDto
        {
            Id = r.Id,
            Period = r.Period.ToString(),
            DateFrom = r.DateFrom,
            DateTo = r.DateTo,
            OpenPrice = r.OpenPrice,
            ClosePrice = r.ClosePrice,
            HighPrice = r.HighPrice,
            LowPrice = r.LowPrice,
            ChangePercent = r.ChangePercent,
            Title = r.Title,
            ExecutiveSummary = r.ExecutiveSummary,
            Recommendations = r.Recommendations,
            FutureOutlook = r.FutureOutlook,
            CreatedAt = r.CreatedAt
        };
    }
}
