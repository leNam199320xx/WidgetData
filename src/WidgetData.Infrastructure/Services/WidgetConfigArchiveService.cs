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
        var items = await _archiveRepo.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<WidgetConfigArchiveDto>> GetByWidgetIdAsync(int widgetId)
    {
        var items = await _archiveRepo.GetByWidgetIdAsync(widgetId);
        return items.Select(MapToDto);
    }

    public async Task<WidgetConfigArchiveDto?> GetByIdAsync(int id)
    {
        var item = await _archiveRepo.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<WidgetConfigArchiveDto> CreateAsync(CreateWidgetConfigArchiveDto dto, string archivedBy)
    {
        var widget = await _widgetRepo.GetByIdAsync(dto.WidgetId);
        if (widget == null) throw new KeyNotFoundException($"Widget {dto.WidgetId} not found");

        var archive = new WidgetConfigArchive
        {
            WidgetId = dto.WidgetId,
            ScheduleId = dto.ScheduleId,
            Configuration = widget.Configuration,
            ChartConfig = widget.ChartConfig,
            HtmlTemplate = widget.HtmlTemplate,
            Note = dto.Note,
            ArchivedBy = archivedBy,
            ArchivedAt = DateTime.UtcNow
        };

        var created = await _archiveRepo.CreateAsync(archive);
        return MapToDto(created);
    }

    public async Task<WidgetConfigArchiveDto?> CreateForScheduleAsync(int widgetId, int scheduleId, string archivedBy)
    {
        var widget = await _widgetRepo.GetByIdAsync(widgetId);
        if (widget == null) return null;

        var archive = new WidgetConfigArchive
        {
            WidgetId = widgetId,
            ScheduleId = scheduleId,
            Configuration = widget.Configuration,
            ChartConfig = widget.ChartConfig,
            HtmlTemplate = widget.HtmlTemplate,
            Note = "Auto-archived by schedule",
            ArchivedBy = archivedBy,
            ArchivedAt = DateTime.UtcNow
        };

        var created = await _archiveRepo.CreateAsync(archive);
        return MapToDto(created);
    }

    public async Task<bool> RestoreAsync(int archiveId)
    {
        var archive = await _archiveRepo.GetByIdAsync(archiveId);
        if (archive == null) return false;

        var widget = await _widgetRepo.GetByIdAsync(archive.WidgetId);
        if (widget == null) return false;

        widget.Configuration = archive.Configuration;
        widget.ChartConfig = archive.ChartConfig;
        widget.HtmlTemplate = archive.HtmlTemplate;
        widget.UpdatedAt = DateTime.UtcNow;

        await _widgetRepo.UpdateAsync(widget);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var archive = await _archiveRepo.GetByIdAsync(id);
        if (archive == null) return false;
        await _archiveRepo.DeleteAsync(id);
        return true;
    }

    private static WidgetConfigArchiveDto MapToDto(WidgetConfigArchive a) => new()
    {
        Id = a.Id,
        WidgetId = a.WidgetId,
        WidgetName = a.Widget?.Name,
        ScheduleId = a.ScheduleId,
        Configuration = a.Configuration,
        ChartConfig = a.ChartConfig,
        HtmlTemplate = a.HtmlTemplate,
        Note = a.Note,
        ArchivedBy = a.ArchivedBy,
        ArchivedAt = a.ArchivedAt
    };
}
