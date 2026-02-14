using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Interfaces;
using MrGoldenBader.Infrastructure.Data;
using MrGoldenBader.Infrastructure.Data.Repositories;
using MrGoldenBader.Infrastructure.Repositories;
using MrGoldenBader.Infrastructure.ExternalApis;
using MrGoldenBader.Infrastructure.Jobs;
using MrGoldenBader.Infrastructure.Seed;
using MrGoldenBader.Infrastructure.Services;

namespace MrGoldenBader.Infrastructure;

/// <summary>
/// إعداد حقن التبعيات لطبقة Infrastructure
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// إضافة خدمات Infrastructure
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ═══════════════════════════════════════════════════════════════════
        // إعداد SQL Server
        // ═══════════════════════════════════════════════════════════════════
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => 
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        
        // ═══════════════════════════════════════════════════════════════════
        // إعداد Redis Cache
        // ═══════════════════════════════════════════════════════════════════
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "MrGoldenBader:";
            });
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // إعداد External APIs
        // ═══════════════════════════════════════════════════════════════════
        services.Configure<ExternalApiSettings>(configuration.GetSection("ExternalApis"));
        services.Configure<MultiNewsApiSettings>(configuration.GetSection("NewsApis"));
        services.AddHttpClient<GlobalGoldPriceService>();
        services.AddHttpClient<KuwaitGoldPriceService>();
        services.AddHttpClient<MultiSourceNewsService>();
        
        // ═══════════════════════════════════════════════════════════════════
        // تسجيل المستودعات
        // ═══════════════════════════════════════════════════════════════════
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IGoldPriceRepository, GoldPriceRepository>();
        services.AddScoped<IKuwaitGoldPriceRepository, KuwaitGoldPriceRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        
        // ═══════════════════════════════════════════════════════════════════
        // تسجيل خدمات Application
        // ═══════════════════════════════════════════════════════════════════
        services.AddScoped<IGoldPriceService, GoldPriceServiceImpl>();
        services.AddScoped<NewsIntegrationService>();
        services.AddScoped<IRecommendationService, RecommendationEngine>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ReportService>();
        
        // ═══════════════════════════════════════════════════════════════════
        // تسجيل خدمة زرع البيانات
        // ═══════════════════════════════════════════════════════════════════
        services.AddScoped<DataSeeder>();
        
        // ═══════════════════════════════════════════════════════════════════
        // تسجيل مهام Hangfire
        // ═══════════════════════════════════════════════════════════════════
        services.AddScoped<PriceUpdateJobs>();
        services.AddScoped<NewsUpdateJobs>();
        services.AddScoped<RecommendationJobs>();
        services.AddScoped<MarketMonitorJobs>();
        
        return services;
    }
}

