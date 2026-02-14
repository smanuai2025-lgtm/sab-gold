namespace MrGoldenBader.Domain.Entities;

public enum ReportPeriod
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,      // 3 أشهر
    SemiAnnual,     // 6 أشهر
    NineMonths,     // 9 أشهر (3 أرباع)
    Annual
}

/// <summary>
/// تقرير السوق الشامل
/// يحتوي على ملخص الأداء، تحليل الذكاء الاصطناعي، التوصيات، والتوقعات المستقبلية
/// </summary>
public class MarketReport : BaseEntity
{
    /// <summary>
    /// نوع الفترة: يومي، أسبوعي...
    /// </summary>
    public ReportPeriod Period { get; set; }
    
    /// <summary>
    /// تاريخ بداية الفترة التي يغطيها التقرير
    /// </summary>
    public DateTime DateFrom { get; set; }
    
    /// <summary>
    /// تاريخ نهاية الفترة
    /// </summary>
    public DateTime DateTo { get; set; }
    
    /// <summary>
    /// أعلى سعر خلال الفترة
    /// </summary>
    public decimal HighPrice { get; set; }
    
    /// <summary>
    /// أدنى سعر خلال الفترة
    /// </summary>
    public decimal LowPrice { get; set; }
    
    /// <summary>
    /// السعر في بداية الفترة
    /// </summary>
    public decimal OpenPrice { get; set; }
    
    /// <summary>
    /// السعر في نهاية الفترة
    /// </summary>
    public decimal ClosePrice { get; set; }
    
    /// <summary>
    /// نسبة التغير
    /// </summary>
    public double ChangePercent { get; set; }

    /// <summary>
    /// العنوان الرئيسي للتقرير (يولده AI)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// الملخص التنفيذي وأهم الأحداث (AI)
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// التوصية الاستراتيجية للفترة القادمة (بيع/شراء/انتظار)
    /// </summary>
    public string Recommendations { get; set; } = string.Empty;

    /// <summary>
    /// التوقعات المستقبلية والسيناريوهات المحتملة (AI Forecast)
    /// </summary>
    public string FutureOutlook { get; set; } = string.Empty;
    
    /// <summary>
    /// بيانات إضافية بصيغة JSON (للرسم البياني أو التفاصيل)
    /// </summary>
    public string? ReportDataJson { get; set; }
}
