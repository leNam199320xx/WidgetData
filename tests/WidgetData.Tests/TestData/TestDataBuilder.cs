using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Tests.TestData;

/// <summary>
/// Cung cấp các phương thức tạo dữ liệu test mẫu cho toàn bộ test suite.
/// </summary>
public static class TestDataBuilder
{
    // ─── DataSource ──────────────────────────────────────────────────────────

    public static DataSource CreateDataSource(int id = 1, string name = "Test DB", bool isActive = true) =>
        new()
        {
            Id = id,
            Name = name,
            SourceType = DataSourceType.SQLite,
            Description = "Cơ sở dữ liệu test SQLite",
            Host = "localhost",
            Port = 5432,
            DatabaseName = "testdb",
            Username = "admin",
            Password = "password",
            IsActive = isActive,
            CreatedBy = "user1",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    public static CreateDataSourceDto CreateDataSourceDto(string name = "New DataSource") =>
        new()
        {
            Name = name,
            SourceType = DataSourceType.SQLite,
            Description = "Mô tả nguồn dữ liệu",
            Host = "localhost",
            Port = 5432,
            DatabaseName = "newdb",
            Username = "dbuser",
            Password = "dbpass"
        };

    public static UpdateDataSourceDto UpdateDataSourceDto(string name = "Updated DataSource", bool isActive = true) =>
        new()
        {
            Name = name,
            SourceType = DataSourceType.PostgreSql,
            Description = "Mô tả đã cập nhật",
            Host = "newhost",
            Port = 5433,
            DatabaseName = "updateddb",
            Username = "newuser",
            Password = "newpass",
            IsActive = isActive
        };

    // ─── Widget ───────────────────────────────────────────────────────────────

    public static Widget CreateWidget(int id = 1, int dataSourceId = 1, bool isActive = true) =>
        new()
        {
            Id = id,
            Name = "Widget Test",
            WidgetType = WidgetType.Chart,
            Description = "Biểu đồ test",
            DataSourceId = dataSourceId,
            Configuration = "{\"query\":\"SELECT * FROM table\"}",
            ChartConfig = "{\"type\":\"bar\"}",
            IsActive = isActive,
            CacheEnabled = false,
            CacheTtlMinutes = 15,
            CreatedBy = "user1",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DataSource = CreateDataSource(dataSourceId)
        };

    public static CreateWidgetDto CreateWidgetDto(string name = "New Widget", int dataSourceId = 1) =>
        new()
        {
            Name = name,
            WidgetType = WidgetType.Table,
            Description = "Bảng dữ liệu mới",
            DataSourceId = dataSourceId,
            Configuration = "{\"query\":\"SELECT id, name FROM users\"}",
            ChartConfig = null,
            CacheEnabled = true,
            CacheTtlMinutes = 30
        };

    public static UpdateWidgetDto UpdateWidgetDto(string name = "Updated Widget", int dataSourceId = 1, bool isActive = true) =>
        new()
        {
            Name = name,
            WidgetType = WidgetType.Metric,
            Description = "Thông số đã cập nhật",
            DataSourceId = dataSourceId,
            Configuration = "{\"query\":\"SELECT COUNT(*) FROM orders\"}",
            ChartConfig = null,
            CacheEnabled = false,
            CacheTtlMinutes = 15,
            IsActive = isActive
        };

    // ─── WidgetExecution ──────────────────────────────────────────────────────

    public static WidgetExecution CreateExecution(int id = 1, int widgetId = 1,
        ExecutionStatus status = ExecutionStatus.Success) =>
        new()
        {
            Id = id,
            ExecutionId = Guid.NewGuid(),
            WidgetId = widgetId,
            Status = status,
            TriggeredBy = ExecutionTrigger.Manual,
            UserId = "user1",
            RowCount = 100,
            ExecutionTimeMs = 250,
            StartedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddMilliseconds(250)
        };

    public static WidgetExecution CreateRunningExecution(int id = 2, int widgetId = 1) =>
        new()
        {
            Id = id,
            ExecutionId = Guid.NewGuid(),
            WidgetId = widgetId,
            Status = ExecutionStatus.Running,
            TriggeredBy = ExecutionTrigger.Scheduler,
            StartedAt = DateTime.UtcNow
        };

    // ─── WidgetSchedule ───────────────────────────────────────────────────────

    public static WidgetSchedule CreateSchedule(int id = 1, int widgetId = 1, bool isEnabled = true) =>
        new()
        {
            Id = id,
            WidgetId = widgetId,
            CronExpression = "0 * * * *",
            Timezone = "UTC",
            IsEnabled = isEnabled,
            RetryOnFailure = true,
            MaxRetries = 3,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Widget = CreateWidget(widgetId)
        };

    public static CreateScheduleDto CreateScheduleDto(int widgetId = 1) =>
        new()
        {
            WidgetId = widgetId,
            CronExpression = "0 */6 * * *",
            Timezone = "Asia/Ho_Chi_Minh",
            IsEnabled = true,
            RetryOnFailure = false,
            MaxRetries = 3
        };

    public static UpdateScheduleDto UpdateScheduleDto(int widgetId = 1) =>
        new()
        {
            WidgetId = widgetId,
            CronExpression = "0 0 * * *",
            Timezone = "UTC",
            IsEnabled = false,
            RetryOnFailure = true,
            MaxRetries = 5
        };
}
