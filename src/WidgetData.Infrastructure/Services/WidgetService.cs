using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly IWidgetConfigArchiveRepository _archiveRepo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IAuditService _auditService;
    private readonly ILogger<WidgetService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantContext? _tenantContext;

    public WidgetService(IWidgetRepository widgetRepo, IExecutionRepository executionRepo,
        ApplicationDbContext context, IWidgetConfigArchiveRepository archiveRepo,
        IScheduleRepository scheduleRepo, IAuditService auditService, ILogger<WidgetService> logger,
        IHttpClientFactory httpClientFactory, ITenantContext? tenantContext = null)
    {
        _widgetRepo = widgetRepo;
        _executionRepo = executionRepo;
        _context = context;
        _archiveRepo = archiveRepo;
        _scheduleRepo = scheduleRepo;
        _auditService = auditService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = groupId, WidgetId = created.Id });
        if (distinctGroupIds.Any()) await _context.SaveChangesAsync();

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

        // Auto-archive current config before updating
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

    public async Task<WidgetExecutionDto> ExecuteAsync(int id, string userId, int? scheduleId = null)
    {
        var widget = await _widgetRepo.GetByIdAsync(id);
        if (widget == null) throw new KeyNotFoundException($"Widget {id} not found");

        // Archive config when triggered by a schedule that has ArchiveConfigOnRun = true
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

        // Run the actual data query and measure elapsed time
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var rawData = await GetDataAsync(id);
            sw.Stop();

            int rowCount = 0;
            if (rawData != null)
            {
                // rawData is anonymous { columns, rows } serialised as object
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

            return ds.SourceType switch
            {
                WidgetData.Domain.Enums.DataSourceType.SQLite => await GetDataFromSqliteAsync(widget, ds),
                WidgetData.Domain.Enums.DataSourceType.Csv => await GetDataFromCsvAsync(widget, ds),
                WidgetData.Domain.Enums.DataSourceType.Json => await GetDataFromJsonAsync(widget, ds),
                WidgetData.Domain.Enums.DataSourceType.Excel => await GetDataFromExcelAsync(widget, ds),
                WidgetData.Domain.Enums.DataSourceType.RestApi => await GetDataFromRestApiAsync(widget, ds),
                _ => new { message = "Data retrieval not implemented for this source type", widgetId = id }
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message, widgetId = id };
        }
    }

    // ── SQLite ────────────────────────────────────────────────────────────────

    private static async Task<object> GetDataFromSqliteAsync(Widget widget, DataSource ds)
    {
        if (string.IsNullOrWhiteSpace(ds.ConnectionString))
            return new { error = "Connection string is empty" };

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var query = config?.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return new { error = "No query configured for this widget" };

        // Strip SQL comments before validation to prevent bypass (e.g. "/**/SELECT" or "-- comment\nDROP")
        var strippedQuery = System.Text.RegularExpressions.Regex.Replace(
            query.TrimStart(), @"(/\*[\s\S]*?\*/|--[^\r\n]*)", " ").TrimStart();

        if (!strippedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return new { error = "Only SELECT queries are permitted for widget data retrieval" };

        string[] disallowedKeywords = ["INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE", "TRUNCATE", "MERGE", "ATTACH", "DETACH"];
        if (disallowedKeywords.Any(k => System.Text.RegularExpressions.Regex.IsMatch(
                strippedQuery, $@"\b{k}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
            return new { error = "Query contains disallowed SQL statements" };

        using var conn = new SqliteConnection(ds.ConnectionString);
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

    // ── CSV ───────────────────────────────────────────────────────────────────

    private static Task<object> GetDataFromCsvAsync(Widget widget, DataSource ds)
    {
        var filePath = ds.ConnectionString;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<object>(new { error = $"CSV file not found: {filePath}" });

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var delimiter = config?.GetValueOrDefault("delimiter")?.ToString() ?? ",";
        var hasHeader = config?.GetValueOrDefault("hasHeader")?.ToString()?.ToLower() != "false";
        char sep = delimiter.Length == 1 ? delimiter[0] : ',';

        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
            return Task.FromResult<object>(new { columns = Array.Empty<string>(), rows = Array.Empty<object>() });

        List<string> columns;
        int startLine;
        if (hasHeader)
        {
            columns = lines[0].Split(sep).Select(c => c.Trim('"', ' ')).ToList();
            startLine = 1;
        }
        else
        {
            var firstRow = lines[0].Split(sep);
            columns = Enumerable.Range(0, firstRow.Length).Select(i => $"col{i + 1}").ToList();
            startLine = 0;
        }

        var rows = new List<Dictionary<string, object?>>();
        for (int i = startLine; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i], sep);
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < columns.Count; c++)
                row[columns[c]] = c < parts.Count ? parts[c] : null;
            rows.Add(row);
        }
        return Task.FromResult<object>(new { columns, rows });
    }

    private static List<string> SplitCsvLine(string line, char sep)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    // Check for escaped double-quote ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip the second quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                if (ch == '"') { inQuotes = true; }
                else if (ch == sep) { result.Add(current.ToString()); current.Clear(); }
                else { current.Append(ch); }
            }
        }
        result.Add(current.ToString());
        return result;
    }

    // ── JSON file ─────────────────────────────────────────────────────────────

    private static Task<object> GetDataFromJsonAsync(Widget widget, DataSource ds)
    {
        var filePath = ds.ConnectionString;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<object>(new { error = $"JSON file not found: {filePath}" });

        var jsonText = File.ReadAllText(filePath);
        using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        // Support optional jsonPath config like "items" to dig into a nested array
        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var jsonPath = config?.GetValueOrDefault("jsonPath")?.ToString();

        System.Text.Json.JsonElement arrayEl = root;
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            foreach (var segment in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
                if (arrayEl.TryGetProperty(segment, out var child))
                    arrayEl = child;
        }

        if (arrayEl.ValueKind != System.Text.Json.JsonValueKind.Array)
            return Task.FromResult<object>(new { error = "JSON root or jsonPath is not an array." });

        var rows = new List<Dictionary<string, object?>>();
        var columns = new List<string>();

        foreach (var item in arrayEl.EnumerateArray())
        {
            if (item.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
            var row = new Dictionary<string, object?>();
            foreach (var prop in item.EnumerateObject())
            {
                if (!columns.Contains(prop.Name)) columns.Add(prop.Name);
                row[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.ToString();
            }
            rows.Add(row);
        }
        return Task.FromResult<object>(new { columns, rows });
    }

    // ── Excel ─────────────────────────────────────────────────────────────────

    private static Task<object> GetDataFromExcelAsync(Widget widget, DataSource ds)
    {
        var filePath = ds.ConnectionString;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<object>(new { error = $"Excel file not found: {filePath}" });

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var sheetName = config?.GetValueOrDefault("sheet")?.ToString();
        var hasHeader = config?.GetValueOrDefault("hasHeader")?.ToString()?.ToLower() != "false";

        using var workbook = new XLWorkbook(filePath);
        var ws = string.IsNullOrWhiteSpace(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheets.Worksheet(sheetName);

        var usedRange = ws.RangeUsed();
        if (usedRange == null)
            return Task.FromResult<object>(new { columns = Array.Empty<string>(), rows = Array.Empty<object>() });

        var allRows = usedRange.RowsUsed().ToList();
        if (allRows.Count == 0)
            return Task.FromResult<object>(new { columns = Array.Empty<string>(), rows = Array.Empty<object>() });

        List<string> columns;
        int startIdx;
        if (hasHeader)
        {
            columns = allRows[0].Cells().Select(c => c.GetString().Trim()).ToList();
            startIdx = 1;
        }
        else
        {
            columns = Enumerable.Range(1, allRows[0].CellCount()).Select(i => $"col{i}").ToList();
            startIdx = 0;
        }

        var rows = new List<Dictionary<string, object?>>();
        for (int r = startIdx; r < allRows.Count; r++)
        {
            var cells = allRows[r].Cells().ToList();
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < columns.Count; c++)
                row[columns[c]] = c < cells.Count ? cells[c].GetString() : null;
            rows.Add(row);
        }
        return Task.FromResult<object>(new { columns, rows });
    }

    // ── REST API ──────────────────────────────────────────────────────────────

    private async Task<object> GetDataFromRestApiAsync(Widget widget, DataSource ds)
    {
        var endpoint = ds.ApiEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            return new { error = "API endpoint is not configured." };

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var jsonPath = config?.GetValueOrDefault("jsonPath")?.ToString();

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        if (!string.IsNullOrWhiteSpace(ds.ApiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", ds.ApiKey);

        var response = await client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
            return new { error = $"API returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}" };

        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        System.Text.Json.JsonElement arrayEl = root;
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            foreach (var segment in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
                if (arrayEl.TryGetProperty(segment, out var child))
                    arrayEl = child;
        }

        if (arrayEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var rows = new List<Dictionary<string, object?>>();
            var columns = new List<string>();
            foreach (var item in arrayEl.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                var row = new Dictionary<string, object?>();
                foreach (var prop in item.EnumerateObject())
                {
                    if (!columns.Contains(prop.Name)) columns.Add(prop.Name);
                    row[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                }
                rows.Add(row);
            }
            return new { columns, rows };
        }

        // Non-array response: return as single-row flat object
        if (arrayEl.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var columns = new List<string>();
            var row = new Dictionary<string, object?>();
            foreach (var prop in arrayEl.EnumerateObject())
            {
                columns.Add(prop.Name);
                row[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.ToString();
            }
            return new { columns, rows = new List<Dictionary<string, object?>> { row } };
        }

        return new { error = "API response is not a JSON array or object." };
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
        LastActivityAt = w.LastActivityAt,
        InactivityAutoDisableEnabled = w.InactivityAutoDisableEnabled,
        InactivityThresholdDays = w.InactivityThresholdDays,
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
