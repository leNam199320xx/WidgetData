using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class WidgetService : IWidgetService
{
    private readonly IWidgetRepository _widgetRepo;
    private readonly IExecutionRepository _executionRepo;
    private readonly ApplicationDbContext _context;

    public WidgetService(IWidgetRepository widgetRepo, IExecutionRepository executionRepo, ApplicationDbContext context)
    {
        _widgetRepo = widgetRepo;
        _executionRepo = executionRepo;
        _context = context;
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
            CreatedBy = userId
        };
        var created = await _widgetRepo.CreateAsync(widget);

        var distinctGroupIds = dto.GroupIds.Distinct().ToList();
        foreach (var groupId in distinctGroupIds)
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = groupId, WidgetId = created.Id });
        if (distinctGroupIds.Any()) await _context.SaveChangesAsync();

        var resultDto = MapToDto(created);
        resultDto.GroupIds = distinctGroupIds;
        return resultDto;
    }

    public async Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return null;

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
        widget.IsActive = dto.IsActive;
        widget.UpdatedAt = DateTime.UtcNow;
        var updated = await _widgetRepo.UpdateAsync(widget);

        // Sync group members via context
        var existing = await _context.WidgetGroupMembers.Where(m => m.WidgetId == id).ToListAsync();
        var existingIds = existing.Select(m => m.WidgetGroupId).ToHashSet();
        var desired = dto.GroupIds.ToHashSet();
        foreach (var toRemove in existing.Where(m => !desired.Contains(m.WidgetGroupId)))
            _context.WidgetGroupMembers.Remove(toRemove);
        foreach (var toAdd in desired.Except(existingIds))
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = toAdd, WidgetId = id });
        await _context.SaveChangesAsync();

        var resultDto = MapToDto(updated);
        await EnrichGroupIdsAsync(new List<WidgetDto> { resultDto });
        return resultDto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return false;
        await _widgetRepo.DeleteAsync(id);
        return true;
    }

    public async Task<WidgetExecutionDto> ExecuteAsync(int id, string userId)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) throw new KeyNotFoundException($"Widget {id} not found");

        var execution = new WidgetExecution
        {
            WidgetId = id,
            Status = ExecutionStatus.Running,
            TriggeredBy = ExecutionTrigger.Manual,
            UserId = userId,
            StartedAt = DateTime.UtcNow
        };
        execution = await _executionRepo.CreateAsync(execution);

        await Task.Delay(100);
        execution.Status = ExecutionStatus.Success;
        execution.RowCount = 0;
        execution.ExecutionTimeMs = 100;
        execution.CompletedAt = DateTime.UtcNow;
        execution = await _executionRepo.UpdateAsync(execution);

        widget.LastExecutedAt = DateTime.UtcNow;
        widget.LastRowCount = execution.RowCount;
        await _widgetRepo.UpdateAsync(widget);

        return MapExecutionToDto(execution);
    }

    public async Task<object?> GetDataAsync(int id)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return null;

        try
        {
            var ds = widget.DataSource;
            if (ds == null) return new { error = "Data source not found" };

            if (ds.SourceType == WidgetData.Domain.Enums.DataSourceType.SQLite && !string.IsNullOrWhiteSpace(ds.ConnectionString))
            {
                var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
                var query = config?.GetValueOrDefault("query")?.ToString();
                if (string.IsNullOrWhiteSpace(query))
                    return new { error = "No query configured for this widget" };

                using var conn = new Microsoft.Data.Sqlite.SqliteConnection(ds.ConnectionString);
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                using var reader = await cmd.ExecuteReaderAsync();

                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                var rows = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                }

                return new { columns, rows };
            }

            return new { message = "Data retrieval not implemented for this source type", widgetId = id };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message, widgetId = id };
        }
    }

    public async Task<IEnumerable<WidgetExecutionDto>> GetHistoryAsync(int id)
    {
        var executions = await _executionRepo.GetByWidgetIdAsync(id);
        return executions.Select(MapExecutionToDto);
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
        CreatedBy = w.CreatedBy,
        CreatedAt = w.CreatedAt
    };

    private async Task EnrichGroupIdsAsync(List<WidgetDto> dtos)
    {
        var ids = dtos.Select(d => d.Id).ToList();
        var members = await _context.WidgetGroupMembers
            .Where(m => ids.Contains(m.WidgetId))
            .ToListAsync();
        foreach (var dto in dtos)
            dto.GroupIds = members.Where(m => m.WidgetId == dto.Id).Select(m => m.WidgetGroupId).ToList();
    }

    private static WidgetExecutionDto MapExecutionToDto(WidgetExecution e) => new()
    {
        Id = e.Id,
        ExecutionId = e.ExecutionId,
        WidgetId = e.WidgetId,
        ScheduleId = e.ScheduleId,
        Status = e.Status,
        TriggeredBy = e.TriggeredBy,
        UserId = e.UserId,
        RowCount = e.RowCount,
        ExecutionTimeMs = e.ExecutionTimeMs,
        ErrorMessage = e.ErrorMessage,
        ResultSummary = e.ResultSummary,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt
    };
}
