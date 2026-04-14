using Microsoft.EntityFrameworkCore;
using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Tests.Services;

public class ExportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IWidgetService> _widgetServiceMock;
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _widgetServiceMock = new Mock<IWidgetService>();
        _service = new ExportService(_context, _widgetServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── GetContentType ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("csv", "text/csv")]
    [InlineData("txt", "text/plain")]
    [InlineData("excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("pdf", "application/pdf")]
    [InlineData("html", "text/html")]
    [InlineData("unknown", "application/octet-stream")]
    public void GetContentType_ReturnsCorrectMimeType(string format, string expected)
    {
        Assert.Equal(expected, _service.GetContentType(format));
    }

    [Fact]
    public void GetContentType_IsCaseInsensitive()
    {
        Assert.Equal(_service.GetContentType("CSV"), _service.GetContentType("csv"));
        Assert.Equal(_service.GetContentType("PDF"), _service.GetContentType("pdf"));
    }

    // ─── GetFileName ──────────────────────────────────────────────────────────

    [Fact]
    public void GetFileName_CsvFormat_ContainsWidgetIdAndCsvExtension()
    {
        var name = _service.GetFileName(5, "csv");

        Assert.Contains("widget_5_export_", name);
        Assert.EndsWith(".csv", name);
    }

    [Fact]
    public void GetFileName_ExcelFormat_UsesXlsxExtension()
    {
        var name = _service.GetFileName(3, "excel");

        Assert.EndsWith(".xlsx", name);
    }

    [Fact]
    public void GetFileName_PdfFormat_UsesPdfExtension()
    {
        var name = _service.GetFileName(1, "pdf");

        Assert.EndsWith(".pdf", name);
    }

    // ─── ExportAsync – error paths ────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_NonExistentWidget_ThrowsKeyNotFoundException()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((WidgetDto?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ExportAsync(99, "csv"));
    }

    [Fact]
    public async Task ExportAsync_UnsupportedFormat_ThrowsNotSupportedException()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "W" });

        await Assert.ThrowsAsync<NotSupportedException>(() => _service.ExportAsync(1, "docx"));
    }

    // ─── ExportAsync – CSV output ─────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_CsvFormat_NoExecutions_ReturnsHeaderOnlyBytes()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "TestWidget" });

        var bytes = await _service.ExportAsync(1, "csv");

        Assert.NotEmpty(bytes);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("ExecutionId", text);
        Assert.Contains("Status", text);
    }

    [Fact]
    public async Task ExportAsync_CsvFormat_WithExecutions_IncludesDataRows()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "W" });
        _context.WidgetExecutions.Add(new WidgetExecution
        {
            ExecutionId = Guid.NewGuid(),
            WidgetId = 1,
            Status = ExecutionStatus.Success,
            TriggeredBy = ExecutionTrigger.Manual,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ExecutionTimeMs = 100,
            RowCount = 10
        });
        await _context.SaveChangesAsync();

        var bytes = await _service.ExportAsync(1, "csv");
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("Success", text);
        Assert.Contains("Manual", text);
    }

    // ─── ExportAsync – TXT output ─────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_TxtFormat_ContainsTitleAndHeaders()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "My Widget" });

        var bytes = await _service.ExportAsync(1, "txt");
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("My Widget", text);
        Assert.Contains("ExecutionId", text);
    }

    // ─── ExportAsync – HTML output ────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_HtmlFormat_ContainsDocTypeAndTable()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "Dashboard" });

        var bytes = await _service.ExportAsync(1, "html");
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("<!DOCTYPE html>", text);
        Assert.Contains("<table>", text);
        Assert.Contains("Dashboard", text);
    }

    // ─── ExportAsync – Excel output ───────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_ExcelFormat_ReturnsNonEmptyBytes()
    {
        _widgetServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new WidgetDto { Id = 1, Name = "W" });

        var bytes = await _service.ExportAsync(1, "excel");

        Assert.NotEmpty(bytes);
        // Excel (OOXML) files start with PK (ZIP signature)
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }
}
