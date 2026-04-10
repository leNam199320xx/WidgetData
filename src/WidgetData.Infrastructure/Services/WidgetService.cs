using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class WidgetService : IWidgetService
{
    private readonly IWidgetRepository _widgetRepo;
    private readonly IExecutionRepository _executionRepo;

    public WidgetService(IWidgetRepository widgetRepo, IExecutionRepository executionRepo)
    {
        _widgetRepo = widgetRepo;
        _executionRepo = executionRepo;
    }

    public async Task<IEnumerable<WidgetDto>> GetAllAsync()
    {
        var widgets = await _widgetRepo.GetAllAsync();
        return widgets.Select(MapToDto);
    }

    public async Task<WidgetDto?> GetByIdAsync(int id)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        return widget == null ? null : MapToDto(widget);
    }

    public async Task<WidgetDto> CreateAsync(CreateWidgetDto dto, string userId)
    {
        var widget = new Widget
        {
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            Description = dto.Description,
            DataSourceId = dto.DataSourceId,
            Configuration = dto.Configuration,
            ChartConfig = dto.ChartConfig,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            CreatedBy = userId
        };
        var created = await _widgetRepo.CreateAsync(widget);
        return MapToDto(created);
    }

    public async Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) return null;
        widget.Name = dto.Name;
        widget.WidgetType = dto.WidgetType;
        widget.Description = dto.Description;
        widget.DataSourceId = dto.DataSourceId;
        widget.Configuration = dto.Configuration;
        widget.ChartConfig = dto.ChartConfig;
        widget.CacheEnabled = dto.CacheEnabled;
        widget.CacheTtlMinutes = dto.CacheTtlMinutes;
        widget.IsActive = dto.IsActive;
        widget.UpdatedAt = DateTime.UtcNow;
        var updated = await _widgetRepo.UpdateAsync(widget);
        return MapToDto(updated);
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
        return new { message = "Data retrieval not implemented for this source type", widgetId = id };
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
        WidgetType = w.WidgetType,
        Description = w.Description,
        DataSourceId = w.DataSourceId,
        DataSourceName = w.DataSource?.Name,
        Configuration = w.Configuration,
        ChartConfig = w.ChartConfig,
        IsActive = w.IsActive,
        CacheEnabled = w.CacheEnabled,
        CacheTtlMinutes = w.CacheTtlMinutes,
        LastExecutedAt = w.LastExecutedAt,
        LastRowCount = w.LastRowCount,
        CreatedBy = w.CreatedBy,
        CreatedAt = w.CreatedAt
    };

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
