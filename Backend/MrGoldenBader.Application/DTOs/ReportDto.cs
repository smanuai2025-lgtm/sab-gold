namespace MrGoldenBader.Application.DTOs;

public class ReportDto
{
    public Guid Id { get; set; }
    public string Period { get; set; } // Daily, Weekly, etc.
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public double ChangePercent { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string FutureOutlook { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
