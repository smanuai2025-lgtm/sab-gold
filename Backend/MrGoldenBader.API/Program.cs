using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MrGoldenBader.Infrastructure.Data;
using MrGoldenBader.Infrastructure.Hubs;
using MrGoldenBader.Infrastructure;
using MrGoldenBader.Infrastructure.Jobs;
using MrGoldenBader.Infrastructure.Seed;
using Serilog;

// إنشاء تطبيق الويب
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
// إعداد Serilog للتسجيل الاحترافي
// ═══════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════
// إضافة الخدمات
// ═══════════════════════════════════════════════════════════════════

// خدمات Infrastructure (DbContext, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// إعداد Controllers مع JSON عربي
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // الحفاظ على أسماء الخصائص كما هي
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ═══════════════════════════════════════════════════════════════════
// إعداد JWT Authentication
// ═══════════════════════════════════════════════════════════════════
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("مفتاح JWT غير موجود");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MrGoldenBader";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MrGoldenBaderClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    
    // دعم SignalR مع JWT
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════════
// إعداد SignalR للتحديثات اللحظية
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddSignalR();

// ═══════════════════════════════════════════════════════════════════
// إعداد Hangfire لجدولة المهام
// ═══════════════════════════════════════════════════════════════════
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
});

// ═══════════════════════════════════════════════════════════════════
// إعداد CORS
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // السماح بأي origin أثناء التطوير
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ═══════════════════════════════════════════════════════════════════
// إعداد Swagger مع دعم JWT
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mr. Golden Bader API",
        Version = "v1",
        Description = "واجهة برمجية لنظام متابعة وتحليل أسعار الذهب",
        Contact = new OpenApiContact
        {
            Name = "فريق التطوير",
            Email = "dev@mrgoldenbader.com"
        }
    });
    
    // إضافة دعم JWT في Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "أدخل رمز JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ═══════════════════════════════════════════════════════════════════
// بناء التطبيق
// ═══════════════════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════
// إعداد قاعدة البيانات (إنشاء وتهيئة)
// ═══════════════════════════════════════════════════════════════════
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // تطبيق الهجرات (Migrations) - يتعامل مع إنشاء قاعدة البيانات تلقائياً
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("✅ تم تطبيق الهجرات المعلقة");
        }
        else if (!await dbContext.Database.CanConnectAsync())
        {
            // إنشاء قاعدة البيانات فقط إذا لم تكن موجودة وليس هناك هجرات
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("✅ تم إنشاء قاعدة البيانات");
        }
        else
        {
            Log.Information("✅ قاعدة البيانات جاهزة");
        }
        
        // زرع البيانات الأساسية إذا لزم الأمر
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
        Log.Information("✅ تم زرع البيانات الأساسية");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "❌ خطأ في إعداد قاعدة البيانات");
    // لا نوقف التطبيق - يمكن أن تكون قاعدة البيانات موجودة فعلاً
}

// ═══════════════════════════════════════════════════════════════════
// إعداد Pipeline
// ═══════════════════════════════════════════════════════════════════

// Swagger في بيئة التطوير
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mr. Golden Bader API v1");
        c.RoutePrefix = string.Empty; // Swagger على الصفحة الرئيسية
    });
}

// تسجيل الطلبات
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowFrontend");

// المصادقة والتفويض
app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithTags("Health")
   .WithName("HealthCheck")
   .WithOpenApi();

// تعيين Controllers
app.MapControllers();

// تعيين SignalR Hub
app.MapHub<GoldPriceHub>("/hubs/goldprice");

// ═══════════════════════════════════════════════════════════════════
// إعداد Hangfire Dashboard والمهام المتكررة
// ═══════════════════════════════════════════════════════════════════
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Mr. Golden Bader - مهام النظام",
    DisplayStorageConnectionString = false
});

// تسجيل المهام المتكررة
// تسجيل المهام المتكررة
PriceUpdateJobs.RegisterRecurringJobs();
NewsUpdateJobs.RegisterNewsJobs();
RecommendationJobs.RegisterRecommendationJobs();
MarketMonitorJobs.RegisterJobs();
Log.Information("تم تسجيل المهام المتكررة (الأسعار + الأخبار + التوصيات + المراقبة اللحظية)");

// ═══════════════════════════════════════════════════════════════════
// تشغيل التطبيق
// ═══════════════════════════════════════════════════════════════════
Log.Information("═══════════════════════════════════════════════════════════════════");
Log.Information("    Mr. Golden Bader - نظام متابعة وتحليل أسعار الذهب");
Log.Information("═══════════════════════════════════════════════════════════════════");
Log.Information("بدء تشغيل النظام...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "فشل في تشغيل التطبيق");
}
finally
{
    Log.CloseAndFlush();
}
