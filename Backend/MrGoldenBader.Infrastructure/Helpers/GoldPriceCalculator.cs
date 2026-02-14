namespace MrGoldenBader.Infrastructure.Helpers;

/// <summary>
/// حاسبة أسعار الذهب المركزية
/// ═══════════════════════════════════════════════════════════════════
/// المعادلة الموحدة (مطابقة لموقع دار السبائك):
/// Gram Price = (Gold Ounce Price in USD ÷ 31.1035) × USD to KWD × Karat Ratio
/// ═══════════════════════════════════════════════════════════════════
/// هذا الملف هو المصدر الوحيد لحساب أسعار الذهب في جميع أنحاء النظام
/// </summary>
public static class GoldPriceCalculator
{
    // ═══════════════════════════════════════════════════════════════════
    // الثوابت الأساسية
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// عدد الجرامات في الأونصة (ثابت عالمي)
    /// </summary>
    public const decimal OUNCE_TO_GRAM = 31.1035m;
    
    /// <summary>
    /// سعر صرف الدولار للدينار الكويتي
    /// </summary>
    public const decimal USD_TO_KWD = 0.308m;
    
    // ═══════════════════════════════════════════════════════════════════
    // نسب العيارات المعتمدة (ثابتة)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// نسب نقاوة العيارات
    /// عيار 24 هو الأساس (1.0 = 100%)
    /// </summary>
    public static readonly Dictionary<int, decimal> KaratRatios = new()
    {
        { 24, 1.0m },     // 100% نقاوة - الأساس (بدون ضرب في نسبة)
        { 22, 0.916m },   // 91.6% نقاوة
        { 21, 0.875m },   // 87.5% نقاوة
        { 18, 0.750m },   // 75.0% نقاوة
        { 14, 0.585m },   // 58.5% نقاوة
        { 12, 0.500m },   // 50.0% نقاوة
        { 10, 0.417m },   // 41.7% نقاوة
        { 9,  0.375m }    // 37.5% نقاوة
    };
    
    // ═══════════════════════════════════════════════════════════════════
    // المعادلة المركزية الوحيدة لحساب سعر الجرام
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// حساب سعر الجرام بالدينار الكويتي
    /// المعادلة: (سعر الأونصة بالدولار ÷ 31.1035) × سعر الصرف × نسبة العيار
    /// مطابقة لموقع دار السبائك (daralsabaek.com)
    /// </summary>
    /// <param name="ouncepriceUsd">سعر الأونصة بالدولار العالمي الحالي</param>
    /// <param name="karat">العيار (24, 22, 21, 18, 14, 12, 10, 9)</param>
    /// <param name="usdToKwd">سعر صرف الدولار للدينار (اختياري، الافتراضي 0.308)</param>
    /// <returns>سعر الجرام بالدينار الكويتي (3 منازل عشرية)</returns>
    public static decimal CalculateGramPrice(decimal ouncepriceUsd, int karat, decimal? usdToKwd = null)
    {
        // التحقق من صحة العيار
        if (!KaratRatios.ContainsKey(karat))
        {
            throw new ArgumentException($"عيار غير صالح: {karat}. العيارات المتاحة: {string.Join(", ", KaratRatios.Keys)}");
        }
        
        var exchangeRate = usdToKwd ?? USD_TO_KWD;
        var karatRatio = KaratRatios[karat];
        
        // المعادلة الموحدة (مطابقة لدار السبائك)
        var gramPrice = (ouncepriceUsd / OUNCE_TO_GRAM) * exchangeRate * karatRatio;
        
        // التقريب إلى 3 منازل عشرية
        return Math.Round(gramPrice, 3);
    }
    
    /// <summary>
    /// حساب سعر جرام عيار 24 (الأساس)
    /// </summary>
    public static decimal CalculateGram24K(decimal ouncepriceUsd, decimal? usdToKwd = null)
    {
        return CalculateGramPrice(ouncepriceUsd, 24, usdToKwd);
    }
    
    /// <summary>
    /// حساب أسعار جميع العيارات من سعر الأونصة
    /// </summary>
    /// <param name="ouncepriceUsd">سعر الأونصة بالدولار</param>
    /// <param name="usdToKwd">سعر الصرف (اختياري)</param>
    /// <returns>قاموس يحتوي على سعر كل عيار</returns>
    public static Dictionary<int, decimal> CalculateAllKarats(decimal ouncepriceUsd, decimal? usdToKwd = null)
    {
        return KaratRatios.Keys.ToDictionary(
            karat => karat,
            karat => CalculateGramPrice(ouncepriceUsd, karat, usdToKwd)
        );
    }
    
    /// <summary>
    /// الحصول على نسبة العيار
    /// </summary>
    public static decimal GetKaratRatio(int karat)
    {
        return KaratRatios.TryGetValue(karat, out var ratio) ? ratio : 0;
    }
    
    /// <summary>
    /// التحقق من صحة العيار
    /// </summary>
    public static bool IsValidKarat(int karat)
    {
        return KaratRatios.ContainsKey(karat);
    }
}
