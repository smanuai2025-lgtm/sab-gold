using Microsoft.AspNetCore.Mvc;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Infrastructure.Services;

namespace MrGoldenBader.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// جلب التقارير
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReportDto>>> GetReports([FromQuery] ReportPeriod? period)
    {
        var reports = await _reportService.GetReportsAsync(period);
        return Ok(reports);
    }

    /// <summary>
    /// توليد تقرير جديد يدوياً
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<ReportDto>> GenerateReport([FromQuery] ReportPeriod period)
    {
        var report = await _reportService.GenerateReportAsync(period);
        if (report == null)
        {
            return BadRequest("فشل توليد التقرير. تأكد من وجود بيانات كافية.");
        }
        return Ok(report);
    }
}
