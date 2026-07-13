using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

public class WidgetCrudService : IWidgetCrudService
{
    private readonly IWidgetRepository _widgetRepo;
    private readonly IJsonWidgetGroupMemberRepository _groupMemberRepo;
    private readonly IWidgetConfigArchiveRepository _archiveRepo;
    private readonly IAuditService _auditService;
    private readonly ILogger _logger;
    private readonly ITenantContext? _tenantContext;

    public WidgetCrudService(IWidgetRepository widgetRepo, IJsonWidgetGroupMemberRepository groupMemberRepo,
        IWidgetConfigArchiveRepository archiveRepo, IAuditService auditService, ILogger logger,
        ITenantContext? tenantContext = null)
    {
        _widgetRepo = widgetRepo;
        _groupMemberRepo = groupMemberRepo;
        _archiveRepo = archiveRepo;
        _auditService = auditService;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<WidgetDto>> GetAllAsync()
    {
        var widgets = await _widgetRepo.GetAllAsync();
        var dtos = widgets.Select(MapToDto).ToList();
        await EnrichGroupIdsAsync(dtos);
        return dtos;
    }

    public async Task<WidgetDto?> GetByIdAsync(int id)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return null;
        var dto = MapToDto(widget);
        await EnrichGroupIdsAsync(new List<WidgetDto> { dto });
        return dto;
    }

    public async Task<WidgetDto> CreateAsync(CreateWidgetDto dto, string userId)
    {
        var widget = new Widget
        {
            Name = dto.Name,
            FriendlyLabel = dto.FriendlyLabel,
            HelpText = dto.HelpText,
            WidgetType = dto.WidgetType,
            Description = dto.Description,
            DataSourceId = dto.DataSourceId,
            Configuration = dto.Configuration,
            ChartConfig = dto.ChartConfig,
            HtmlTemplate = dto.HtmlTemplate,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            InactivityAutoDisableEnabled = dto.InactivityAutoDisableEnabled,
            InactivityThresholdDays = dto.InactivityThresholdDays,
            CreatedBy = userId,
            TenantId = _tenantContext?.CurrentTenantId
        };
        var created = await _widgetRepo.CreateAsync(widget);

        var distinctGroupIds = dto.GroupIds.Distinct().ToList();
        foreach (var groupId in distinctGroupIds)
            await _groupMemberRepo.CreateAsync(new WidgetGroupMember { WidgetGroupId = groupId, WidgetId = created.Id });

        _logger.LogInformation("Widget {WidgetId} '{Name}' created by user {UserId}", created.Id, created.Name, userId);
        await _auditService.LogAsync("CreateWidget", "Widget", created.Id.ToString(), newValues: new { created.Name, created.DataSourceId }, userId: userId);

        var resultDto = MapToDto(created);
        resultDto.GroupIds = distinctGroupIds;
        return resultDto;
    }

    public async Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return null;

        var configChanged = widget.Configuration != dto.Configuration
            || widget.ChartConfig != dto.ChartConfig
            || widget.HtmlTemplate != dto.HtmlTemplate;
        if (configChanged)
        {
            await _archiveRepo.CreateAsync(new WidgetConfigArchive
            {
                WidgetId = id,
                Configuration = widget.Configuration,
                ChartConfig = widget.ChartConfig,
                HtmlTemplate = widget.HtmlTemplate,
                TriggerSource = "OnSave",
                ArchivedAt = DateTime.UtcNow
            });
        }

        widget.Name = dto.Name;
        widget.FriendlyLabel = dto.FriendlyLabel;
        widget.HelpText = dto.HelpText;
        widget.WidgetType = dto.WidgetType;
        widget.Description = dto.Description;
        widget.DataSourceId = dto.DataSourceId;
        widget.Configuration = dto.Configuration;
        widget.ChartConfig = dto.ChartConfig;
        widget.HtmlTemplate = dto.HtmlTemplate;
        widget.CacheEnabled = dto.CacheEnabled;
        widget.CacheTtlMinutes = dto.CacheTtlMinutes;
        widget.InactivityAutoDisableEnabled = dto.InactivityAutoDisableEnabled;
        widget.InactivityThresholdDays = dto.InactivityThresholdDays;
        widget.IsActive = dto.IsActive;
        widget.UpdatedAt = DateTime.UtcNow;
        var updated = await _widgetRepo.UpdateAsync(widget);

        var existing = await _groupMemberRepo.GetByWidgetAsync(id);
        var existingIds = existing.Select(m => m.WidgetGroupId).ToHashSet();
        var desired = dto.GroupIds.ToHashSet();
        foreach (var toRemove in existing.Where(m => !desired.Contains(m.WidgetGroupId)).ToList())
            await _groupMemberRepo.DeleteAsync(ToCompositeId(toRemove.WidgetGroupId, toRemove.WidgetId));
        foreach (var toAdd in desired.Except(existingIds))
            await _groupMemberRepo.CreateAsync(new WidgetGroupMember { WidgetGroupId = toAdd, WidgetId = id });

        var resultDto = MapToDto(updated);
        await EnrichGroupIdsAsync(new List<WidgetDto> { resultDto });

        _logger.LogInformation("Widget {WidgetId} '{Name}' updated", id, updated.Name);
        await _auditService.LogAsync("UpdateWidget", "Widget", id.ToString(), newValues: new { updated.Name, updated.IsActive });

        return resultDto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return false;
        await _widgetRepo.DeleteAsync(id);
        _logger.LogInformation("Widget {WidgetId} '{Name}' deleted", id, widget.Name);
        await _auditService.LogAsync("DeleteWidget", "Widget", id.ToString(), oldValues: new { widget.Name });
        return true;
    }

    private static WidgetDto MapToDto(Widget w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        FriendlyLabel = w.FriendlyLabel,
        HelpText = w.HelpText,
        WidgetType = w.WidgetType,
        Description = w.Description,
        DataSourceId = w.DataSourceId,
        DataSourceName = w.DataSource?.Name,
        Configuration = w.Configuration,
        ChartConfig = w.ChartConfig,
        HtmlTemplate = w.HtmlTemplate,
        IsActive = w.IsActive,
        CacheEnabled = w.CacheEnabled,
        CacheTtlMinutes = w.CacheTtlMinutes,
        LastExecutedAt = w.LastExecutedAt,
        LastRowCount = w.LastRowCount,
        LastActivityAt = w.LastActivityAt,
        InactivityAutoDisableEnabled = w.InactivityAutoDisableEnabled,
        InactivityThresholdDays = w.InactivityThresholdDays,
        CreatedBy = w.CreatedBy,
        CreatedAt = w.CreatedAt
    };

    private async Task EnrichGroupIdsAsync(List<WidgetDto> dtos)
    {
        foreach (var dto in dtos)
            dto.GroupIds = (await _groupMemberRepo.GetByWidgetAsync(dto.Id))
                .Select(m => m.WidgetGroupId)
                .ToList();
    }

    private static int ToCompositeId(int groupId, int widgetId)
        => unchecked((groupId * 1_000_000) + widgetId);
}
