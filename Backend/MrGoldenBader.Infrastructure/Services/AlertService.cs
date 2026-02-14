using Microsoft.Extensions.Logging;
using MrGoldenBader.Application.DTOs;
using MrGoldenBader.Application.Interfaces;
using MrGoldenBader.Domain.Entities;
using MrGoldenBader.Domain.Interfaces;

namespace MrGoldenBader.Infrastructure.Services;

/// <summary>
/// خدمة إدارة التنبيهات
/// </summary>
public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepo;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IAlertRepository alertRepo, ILogger<AlertService> logger)
    {
        _alertRepo = alertRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<AlertDto>> GetUnreadAlertsAsync()
    {
        var alerts = await _alertRepo.GetUnreadAsync();
        return alerts.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<AlertDto>> GetAllAlertsAsync(int count = 50)
    {
        var alerts = await _alertRepo.GetRecentAsync(count);
        return alerts.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        return await _alertRepo.GetUnreadCountAsync();
    }

    public async Task MarkAsReadAsync(Guid alertId)
    {
        var alert = await _alertRepo.GetByIdAsync(alertId);
        if (alert != null)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            await _alertRepo.UpdateAsync(alert);
            await _alertRepo.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        var unreadAlerts = await _alertRepo.GetUnreadAsync();
        foreach (var alert in unreadAlerts)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            await _alertRepo.UpdateAsync(alert);
        }
        await _alertRepo.SaveChangesAsync();
    }

    public async Task<AlertDto> CreateAlertAsync(string type, string title, string message, string severity)
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _alertRepo.AddAsync(alert);
        await _alertRepo.SaveChangesAsync();

        _logger.LogInformation("تم إنشاء تنبيه جديد: {Title}", title);

        return MapToDto(alert);
    }

    private static AlertDto MapToDto(Alert alert)
    {
        return new AlertDto
        {
            Id = alert.Id,
            Type = alert.Type,
            Title = alert.Title,
            Message = alert.Message,
            Severity = alert.Severity,
            IsRead = alert.IsRead,
            CreatedAt = alert.CreatedAt
        };
    }
}
