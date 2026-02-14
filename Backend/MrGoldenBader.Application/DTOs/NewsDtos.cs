namespace MrGoldenBader.Application.DTOs;

/// <summary>
/// نموذج نقل بيانات الخبر الخام
/// </summary>
public class RawNewsDto
{
    /// <summary>
    /// معرف الخبر
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// عنوان الخبر
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// ملخص الخبر
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// مصدر الخبر
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// رابط الخبر الأصلي
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ النشر
    /// </summary>
    public DateTime PublishedAt { get; set; }
    
    /// <summary>
    /// اللغة
    /// </summary>
    public string Language { get; set; } = "ar";
    
    /// <summary>
    /// هل تم تحليله؟
    /// </summary>
    public bool IsAnalyzed { get; set; }
}

/// <summary>
/// نموذج نقل بيانات الخبر المحلل - شامل وغني بالتفاصيل
/// </summary>
public class AnalyzedNewsDto
{
    // ═══════════════════════════════════════════════════════════════════
    // البيانات الأساسية
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// معرف الخبر
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// عنوان الخبر
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// الملخص الذكي
    /// </summary>
    public string SmartSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// مصدر الخبر (اسم الموقع)
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// دومين المصدر (مثال: reuters.com)
    /// </summary>
    public string SourceDomain { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ النشر
    /// </summary>
    public DateTime PublishedAt { get; set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // المحتوى الغني (Rich Content)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// رابط صورة البانر الرئيسية
    /// </summary>
    public string BannerImage { get; set; } = string.Empty;
    
    /// <summary>
    /// رابط المقال الأصلي الكامل
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// أسماء الكتّاب/المؤلفين
    /// </summary>
    public List<string> Authors { get; set; } = new();
    
    /// <summary>
    /// المواضيع/التصنيفات (economy, finance, technology, etc.)
    /// </summary>
    public List<TopicDto> Topics { get; set; } = new();
    
    // ═══════════════════════════════════════════════════════════════════
    // تحليل المشاعر (Sentiment Analysis)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// المشاعر العامة (positive, negative, neutral, bullish, bearish)
    /// </summary>
    public string Sentiment { get; set; } = "neutral";
    
    /// <summary>
    /// درجة المشاعر (-1 إلى 1)
    /// </summary>
    public double SentimentScore { get; set; }
    
    /// <summary>
    /// تصنيف المشاعر (Bullish, Bearish, Neutral, Somewhat-Bullish, Somewhat-Bearish)
    /// </summary>
    public string SentimentLabel { get; set; } = "Neutral";
    
    /// <summary>
    /// وصف المشاعر بالعربية
    /// </summary>
    public string SentimentArabic => Sentiment?.ToLower() switch
    {
        "bullish" or "positive" => "إيجابي",
        "bearish" or "negative" => "سلبي",
        "somewhat-bullish" => "إيجابي نسبياً",
        "somewhat-bearish" => "سلبي نسبياً",
        _ => "محايد"
    };
    
    // ═══════════════════════════════════════════════════════════════════
    // تحليل التأثير على الذهب
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// اتجاه التأثير على الذهب (bullish, bearish, neutral)
    /// </summary>
    public string GoldImpactDirection { get; set; } = "neutral";
    
    /// <summary>
    /// نوع التأثير على الذهب (للتوافقية)
    /// </summary>
    public string ImpactType { get; set; } = "neutral";
    
    /// <summary>
    /// وصف التأثير بالعربية
    /// </summary>
    public string ImpactTypeArabic => GoldImpactDirection?.ToLower() switch
    {
        "bullish" => "📈 صعودي للذهب",
        "bearish" => "📉 هبوطي للذهب",
        _ => "➡️ محايد"
    };
    
    /// <summary>
    /// درجة التأثير (0-100)
    /// </summary>
    public double ImpactScore { get; set; }
    
    /// <summary>
    /// درجة الثقة في التحليل (0-100)
    /// </summary>
    public double ConfidenceScore { get; set; }
    
    /// <summary>
    /// شرح التأثير على الذهب
    /// </summary>
    public string ImpactExplanation { get; set; } = string.Empty;
    
    /// <summary>
    /// نسبة ارتباط الخبر بالذهب (Ticker Relevance)
    /// </summary>
    public double GoldRelevanceScore { get; set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // ربط الخبر بحركة السعر
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// سعر الذهب وقت نشر الخبر
    /// </summary>
    public decimal PriceAtPublication { get; set; }
    
    /// <summary>
    /// تغير السعر بعد الخبر (بالدولار)
    /// </summary>
    public decimal PriceChange { get; set; }
    
    /// <summary>
    /// نسبة تغير السعر بعد الخبر (%)
    /// </summary>
    public decimal PriceChangePercent { get; set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // الكيانات المستخرجة (NER)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// الدول المذكورة
    /// </summary>
    public List<string> Countries { get; set; } = new();
    
    /// <summary>
    /// البنوك المركزية المذكورة
    /// </summary>
    public List<string> CentralBanks { get; set; } = new();
    
    /// <summary>
    /// الشخصيات المذكورة
    /// </summary>
    public List<string> Persons { get; set; } = new();
    
    /// <summary>
    /// المنظمات والمؤسسات
    /// </summary>
    public List<string> Organizations { get; set; } = new();
    
    /// <summary>
    /// الأسواق المالية المذكورة
    /// </summary>
    public List<string> Markets { get; set; } = new();
    
    // ═══════════════════════════════════════════════════════════════════
    // تصنيف الخبر
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// فئة الخبر الرئيسية
    /// </summary>
    public string Category { get; set; } = "general";
    
    /// <summary>
    /// أيقونة الفئة
    /// </summary>
    public string CategoryIcon => Category?.ToLower() switch
    {
        "gold" or "xau" => "🥇",
        "fed" or "central_bank" or "interest_rate" => "🏦",
        "economy" or "economy_macro" => "📊",
        "finance" or "financial_markets" => "💹",
        "geopolitics" or "war" => "🌍",
        "inflation" or "cpi" => "📈",
        "dollar" or "forex" => "💵",
        "etf" or "funds" => "📦",
        _ => "📰"
    };
    
    /// <summary>
    /// اسم الفئة بالعربية
    /// </summary>
    public string CategoryArabic => Category?.ToLower() switch
    {
        "gold" or "xau" => "أخبار الذهب",
        "fed" or "central_bank" or "interest_rate" => "البنوك المركزية",
        "economy" or "economy_macro" => "الاقتصاد الكلي",
        "finance" or "financial_markets" => "الأسواق المالية",
        "geopolitics" or "war" => "أخبار جيوسياسية",
        "inflation" or "cpi" => "التضخم",
        "dollar" or "forex" => "العملات",
        "etf" or "funds" => "صناديق الاستثمار",
        _ => "أخبار عامة"
    };
    
    /// <summary>
    /// الكلمات المفتاحية
    /// </summary>
    public List<string> Keywords { get; set; } = new();
    
    // ═══════════════════════════════════════════════════════════════════
    // معلومات إضافية
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// اللغة الأصلية للخبر
    /// </summary>
    public string Language { get; set; } = "en";
    
    /// <summary>
    /// النموذج المستخدم في التحليل
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;
    
    /// <summary>
    /// تاريخ التحليل
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// هل هذا خبر عاجل؟
    /// </summary>
    public bool IsBreaking => ImpactScore >= 80;
    
    /// <summary>
    /// هل الخبر مهم جداً؟
    /// </summary>
    public bool IsHighImpact => ImpactScore >= 60;
}

/// <summary>
/// نموذج الموضوع/التصنيف
/// </summary>
public class TopicDto
{
    public string Name { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    
    public string NameArabic => Name?.ToLower() switch
    {
        "economy_macro" => "الاقتصاد الكلي",
        "finance" => "المالية",
        "technology" => "التكنولوجيا",
        "manufacturing" => "الصناعة",
        "earnings" => "الأرباح",
        "ipo" => "الاكتتابات",
        "mergers_and_acquisitions" => "الاندماجات",
        "real_estate" => "العقارات",
        "energy_transportation" => "الطاقة والنقل",
        "blockchain" => "البلوك تشين",
        "retail_wholesale" => "التجزئة",
        "life_sciences" => "العلوم الحياتية",
        _ => Name ?? "عام"
    };
    
    public string Icon => Name?.ToLower() switch
    {
        "economy_macro" => "📊",
        "finance" => "💰",
        "technology" => "💻",
        "manufacturing" => "🏭",
        "earnings" => "📈",
        "ipo" => "🎯",
        "mergers_and_acquisitions" => "🤝",
        "real_estate" => "🏠",
        "energy_transportation" => "⚡",
        "blockchain" => "🔗",
        "retail_wholesale" => "🛒",
        "life_sciences" => "🧬",
        _ => "📌"
    };
}

/// <summary>
/// إحصائيات الأخبار
/// </summary>
public class NewsStatsDto
{
    public int TotalNews { get; set; }
    public int AnalyzedNews { get; set; }
    public int BullishNews { get; set; }
    public int BearishNews { get; set; }
    public int NeutralNews { get; set; }
    public double AverageImpactScore { get; set; }
    public DateTime LastUpdate { get; set; }
}
