using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class ExcelDataSourceStrategy : IDataSourceStrategy
{
    public bool CanHandle(DataSourceType sourceType) => sourceType == DataSourceType.Excel;

    public Task<object> LoadDataAsync(Widget widget, DataSource ds, IHttpClientFactory httpClientFactory)
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
}