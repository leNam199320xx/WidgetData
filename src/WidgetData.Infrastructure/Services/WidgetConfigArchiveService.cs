using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class WidgetConfigArchiveService : IWidgetConfigArchiveService
{
    private readonly IWidgetConfigArchiveRepository _archiveRepo;
    private readonly IWidgetRepository _widgetRepo;

    public WidgetConfigArchiveService(IWidgetConfigArchiveRepository archiveRepo, IWidgetRepository widgetRepo)
    {
        _archiveRepo = archiveRepo;
        _widgetRepo = widgetRepo;
    }

    public async Task<IEnumerable<WidgetConfigArchiveDto>> GetAllAsync()
    {
        var archives = await _archiveRepo.GetAllAsync();
        return archives.Select(a => MapToDto(a, a.Widget?.Name));
    }

    public async Task<IEnumerable<WidgetConfigArchiveDto>> GetByWidgetIdAsync(int widgetId)
    {
        var archives = await _archiveRepo.GetByWidgetIdAsync(widgetId);
        var widget = await _widgetRepo.GetByIdAsync(widgetId);
        return archives.Select(a => MapToDto(a, widget?.Name));
    }

    public async Task<WidgetConfigArchiveDto?> CreateAsync(int widgetId, CreateWidgetConfigArchiveDto dto,
        string userId, string triggerSource = "Manual", int? scheduleId = null)
    {
        var widget = await _widgetRepo.GetByIdAsync(widgetId);
        if (widget == null) return null;

        var archive = new WidgetConfigArchive
        {
            WidgetId = widgetId,
            Configuration = widget.Configuration,
            ChartConfig = widget.ChartConfig,
            HtmlTemplate = widget.HtmlTemplate,
            Note = dto.Note,
            TriggerSource = triggerSource,
            ScheduleId = scheduleId,
            ArchivedBy = userId,
            ArchivedAt = DateTime.UtcNow
        };

        var created = await _archiveRepo.CreateAsync(archive);
        return MapToDto(created, widget.Name);
    }

    public async Task<WidgetDto?> RestoreAsync(int widgetId, int archiveId, string userId)
    {
        var archive = await _archiveRepo.GetByIdAsync(archiveId);
        if (archive == null || archive.WidgetId != widgetId) return null;

        var widget = await _widgetRepo.GetByIdAsync(widgetId);
        if (widget == null) return null;

        // Archive current config before overwriting (preserve current state)
        await _archiveRepo.CreateAsync(new WidgetConfigArchive
        {
            WidgetId = widgetId,
            Configuration = widget.Configuration,
            ChartConfig = widget.ChartConfig,
            HtmlTemplate = widget.HtmlTemplate,
            Note = $"Auto-archived before restore from archive #{archiveId}",
            TriggerSource = "OnSave",
            ArchivedBy = userId,
            ArchivedAt = DateTime.UtcNow
        });

        widget.Configuration = archive.Configuration;
        widget.ChartConfig = archive.ChartConfig;
        widget.HtmlTemplate = archive.HtmlTemplate;
        widget.UpdatedAt = DateTime.UtcNow;

        var updated = await _widgetRepo.UpdateAsync(widget);
        return new WidgetDto
        {
            Id = updated.Id,
            Name = updated.Name,
            FriendlyLabel = updated.FriendlyLabel,
            HelpText = updated.HelpText,
            WidgetType = updated.WidgetType,
            Description = updated.Description,
            DataSourceId = updated.DataSourceId,
            DataSourceName = updated.DataSource?.Name,
            Configuration = updated.Configuration,
            ChartConfig = updated.ChartConfig,
            HtmlTemplate = updated.HtmlTemplate,
            IsActive = updated.IsActive,
            CacheEnabled = updated.CacheEnabled,
            CacheTtlMinutes = updated.CacheTtlMinutes,
            LastExecutedAt = updated.LastExecutedAt,
            LastRowCount = updated.LastRowCount,
            CreatedBy = updated.CreatedBy,
            CreatedAt = updated.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int archiveId)
        => await _archiveRepo.DeleteAsync(archiveId);

    private static WidgetConfigArchiveDto MapToDto(WidgetConfigArchive a, string? widgetName) => new()
    {
        Id = a.Id,
        WidgetId = a.WidgetId,
        WidgetName = widgetName,
        Configuration = a.Configuration,
        ChartConfig = a.ChartConfig,
        HtmlTemplate = a.HtmlTemplate,
        Note = a.Note,
        TriggerSource = a.TriggerSource,
        ScheduleId = a.ScheduleId,
        ArchivedBy = a.ArchivedBy,
        ArchivedAt = a.ArchivedAt
    };
}
