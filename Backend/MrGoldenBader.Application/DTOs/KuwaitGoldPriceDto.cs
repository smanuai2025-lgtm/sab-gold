namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات سعر الذهب في الكويت
/// </summary>
public class KuwaitGoldPriceDto
{
    /// <summary>
    /// العيار
    /// </summary>
    public int Karat { get; set; }
    
    /// <summary>
    /// اسم العيار بالعربية
    /// </summary>
    public string KaratName => Karat switch
    {
        24 => "ذهب عيار 24",
        22 => "ذهب عيار 22",
        21 => "ذهب عيار 21",
        18 => "ذهب عيار 18",
        _ => $"عيار {Karat}"
    };
    
    /// <summary>
    /// سعر الشراء بالدينار
    /// </summary>
    public decimal BuyPrice { get; set; }
    
    /// <summary>
    /// سعر البيع بالدينار
    /// </summary>
    public decimal SellPrice { get; set; }
    
    /// <summary>
    /// الفرق بين البيع والشراء
    /// </summary>
    public decimal Spread => SellPrice - BuyPrice;
    
    /// <summary>
    /// نسبة التغير
    /// </summary>
    public decimal? ChangePercent { get; set; }
    
    /// <summary>
    /// قيمة التغير
    /// </summary>
    public decimal? ChangeValue { get; set; }
    
    /// <summary>
    /// الطابع الزمني
    /// </summary>
    public DateTime Timestamp { get; set; }
}
