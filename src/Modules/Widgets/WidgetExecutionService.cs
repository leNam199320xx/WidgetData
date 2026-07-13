using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

public class WidgetExecutionService : IWidgetExecutionService
{
    private readonly IWidgetRepository _widgetRepo;
    private readonly IExecutionRepository _executionRepo;
    private readonly IWidgetConfigArchiveRepository _archiveRepo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IAuditService _auditService;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<IDataSourceStrategy> _strategies;
    private readonly ITenantContext? _tenantContext;

    public WidgetExecutionService(IWidgetRepository widgetRepo, IExecutionRepository executionRepo,
        IWidgetConfigArchiveRepository archiveRepo, IScheduleRepository scheduleRepo, IAuditService auditService,
        ILogger logger, IHttpClientFactory httpClientFactory, IEnumerable<IDataSourceStrategy> strategies,
        ITenantContext? tenantContext = null)
    {
        _widgetRepo = widgetRepo;
        _executionRepo = executionRepo;
        _archiveRepo = archiveRepo;
        _scheduleRepo = scheduleRepo;
        _auditService = auditService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _strategies = strategies;
        _tenantContext = tenantContext;
    }

    public async Task<WidgetExecutionDto> ExecuteAsync(int id, string userId, int? scheduleId = null)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) throw new KeyNotFoundException($"Widget {id} not found");

        if (scheduleId.HasValue)
        {
            var schedule = await _scheduleRepo.GetByIdAsync(scheduleId.Value);
            if (schedule != null && schedule.ArchiveConfigOnRun)
            {
                await _archiveRepo.CreateAsync(new WidgetConfigArchive
                {
                    WidgetId = id,
                    Configuration = widget.Configuration,
                    ChartConfig = widget.ChartConfig,
                    HtmlTemplate = widget.HtmlTemplate,
                    TriggerSource = "Schedule",
                    ScheduleId = scheduleId,
                    ArchivedBy = userId,
                    ArchivedAt = DateTime.UtcNow
                });
            }
        }

        var execution = new WidgetExecution
        {
            WidgetId = id,
            Status = ExecutionStatus.Running,
            TriggeredBy = ExecutionTrigger.Manual,
            UserId = userId,
            StartedAt = DateTime.UtcNow
        };
        execution = await _executionRepo.CreateAsync(execution);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var rawData = await GetDataAsync(id);
            sw.Stop();

            int rowCount = 0;
            if (rawData != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(rawData);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("rows", out var rowsEl)
                    && rowsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    rowCount = rowsEl.GetArrayLength();
            }

            execution.Status = ExecutionStatus.Success;
            execution.RowCount = rowCount;
            execution.ExecutionTimeMs = sw.ElapsedMilliseconds;
            execution.ResultSummary = $"{rowCount} rows in {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            sw.Stop();
            execution.Status = ExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.ExecutionTimeMs = sw.ElapsedMilliseconds;
            _logger.LogWarning(ex, "Widget {WidgetId} execution failed", id);
        }

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

            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(ds.SourceType));
            if (strategy == null)
                return new { error = $"Only JSON data source is supported. Current source type: {ds.SourceType}", widgetId = id };

            return await strategy.LoadDataAsync(widget, ds, _httpClientFactory);
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
