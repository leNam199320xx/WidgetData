using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

/// <summary>
/// Test tính năng WidgetForm (trang WidgetConfigure) - bao gồm tất cả các bước:
/// Bước 1: Thông tin (Name, FriendlyLabel, HelpText, WidgetType, GroupIds)
/// Bước 2: Dữ liệu (DataSourceId, Configuration, ChartConfig, Cache)
/// Bước 3: Lịch chạy (Schedule)
/// Bước 4: Xuất kết quả (Export/Delivery)
/// Bước 5: Idea Board (chỉ khi WidgetType = Form)
/// Sử dụng db test đã có sẵn (InMemory DB + TestDataBuilder).
/// </summary>
public class WidgetFormTests
{
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly Mock<IExecutionRepository> _executionRepoMock;
    private readonly Mock<IWidgetConfigArchiveRepository> _archiveRepoMock;
    private readonly Mock<IScheduleRepository> _scheduleRepoMock;
    private readonly ApplicationDbContext _context;
    private readonly WidgetService _service;

    public WidgetFormTests()
    {
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _executionRepoMock = new Mock<IExecutionRepository>();
        _archiveRepoMock = new Mock<IWidgetConfigArchiveRepository>();
        _scheduleRepoMock = new Mock<IScheduleRepository>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Seed test WidgetGroups vào InMemory DB (db test đã có sẵn)
        _context.WidgetGroups.AddRange(
            TestDataBuilder.CreateWidgetGroup(1, "Nhóm Kinh doanh"),
            TestDataBuilder.CreateWidgetGroup(2, "Nhóm Kỹ thuật"),
            TestDataBuilder.CreateWidgetGroup(3, "Nhóm Quản lý")
        );
        _context.SaveChanges();

        var auditServiceMock = new Mock<IAuditService>();
        var loggerMock = new Mock<ILogger<WidgetService>>();
        _service = new WidgetService(_widgetRepoMock.Object, _executionRepoMock.Object, _context,
            _archiveRepoMock.Object, _scheduleRepoMock.Object, auditServiceMock.Object, loggerMock.Object);
    }

    // ─── Bước 1: Thông tin - FriendlyLabel ───────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithFriendlyLabel_SetsFriendlyLabelOnEntity()
    {
        const string friendlyLabel = "Báo cáo doanh thu tháng";
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta(friendlyLabel: friendlyLabel);
        var created = BuildCreatedWidget(dto, id: 10, friendlyLabel: friendlyLabel);
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(friendlyLabel, result.FriendlyLabel);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.FriendlyLabel == friendlyLabel)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullFriendlyLabel_MapsAsNull()
    {
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta(friendlyLabel: null!);
        dto.FriendlyLabel = null;
        var created = BuildCreatedWidget(dto, id: 11, friendlyLabel: null);
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Null(result.FriendlyLabel);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.FriendlyLabel == null)), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_MapsFriendlyLabelCorrectly()
    {
        const string friendlyLabel = "Biểu đồ bán hàng theo khu vực";
        var widget = TestDataBuilder.CreateWidgetWithMeta(id: 1, friendlyLabel: friendlyLabel);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(friendlyLabel, result.FriendlyLabel);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFriendlyLabel()
    {
        const string newLabel = "Báo cáo quý mới";
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto("Updated Widget");
        dto.FriendlyLabel = newLabel;

        var updated = BuildUpdatedWidget(dto, id: 1, friendlyLabel: newLabel);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal(newLabel, result.FriendlyLabel);
        _widgetRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Widget>(w => w.FriendlyLabel == newLabel)), Times.Once);
    }

    // ─── Bước 1: Thông tin - HelpText ────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithHelpText_SetsHelpTextOnEntity()
    {
        const string helpText = "Hiển thị doanh thu theo tháng của từng khu vực";
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta(helpText: helpText);
        var created = BuildCreatedWidget(dto, id: 20, helpText: helpText);
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(helpText, result.HelpText);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.HelpText == helpText)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullHelpText_MapsAsNull()
    {
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta();
        dto.HelpText = null;
        var created = BuildCreatedWidget(dto, id: 21, helpText: null);
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Null(result.HelpText);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.HelpText == null)), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_MapsHelpTextCorrectly()
    {
        const string helpText = "Chỉ số tổng hợp theo ngày, dùng cho quản lý cấp cao";
        var widget = TestDataBuilder.CreateWidgetWithMeta(id: 5, helpText: helpText);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(helpText, result.HelpText);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesHelpText()
    {
        const string newHelpText = "Bảng tổng hợp đơn hàng mới nhất";
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.HelpText = newHelpText;

        var updated = BuildUpdatedWidget(dto, id: 1, helpText: newHelpText);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal(newHelpText, result.HelpText);
        _widgetRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Widget>(w => w.HelpText == newHelpText)), Times.Once);
    }

    // ─── Bước 1: Thông tin - FriendlyLabel + HelpText cùng lúc ───────────────

    [Fact]
    public async Task CreateAsync_WithBothFriendlyLabelAndHelpText_SetsAllFields()
    {
        const string label = "Doanh thu tháng 12";
        const string helpText = "Tổng doanh thu tháng 12 theo từng chi nhánh";
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta(
            friendlyLabel: label, helpText: helpText);
        var created = BuildCreatedWidget(dto, id: 30, friendlyLabel: label, helpText: helpText, createdBy: "admin");
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "admin");

        Assert.Equal(label, result.FriendlyLabel);
        Assert.Equal(helpText, result.HelpText);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal("admin", result.CreatedBy);
    }

    // ─── Bước 1: Thông tin - GroupIds (Thuộc nhóm) ───────────────────────────

    [Fact]
    public async Task CreateAsync_WithGroupIds_CreatesWidgetGroupMembers()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Widget Nhóm KD");
        dto.GroupIds = new List<int> { 1, 2 };

        var created = new Widget
        {
            Id = 100,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == 100).ToList();
        Assert.Equal(2, members.Count);
        Assert.Contains(members, m => m.WidgetGroupId == 1);
        Assert.Contains(members, m => m.WidgetGroupId == 2);
        Assert.Equal(2, result.GroupIds.Count);
        Assert.Contains(1, result.GroupIds);
        Assert.Contains(2, result.GroupIds);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyGroupIds_NoGroupMembersCreated()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Widget Không Nhóm");
        dto.GroupIds = new List<int>();

        var created = new Widget
        {
            Id = 101,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == 101).ToList();
        Assert.Empty(members);
        Assert.Empty(result.GroupIds);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateGroupIds_DeduplicatesGroups()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Widget Trùng Nhóm");
        dto.GroupIds = new List<int> { 1, 1, 2, 2 };

        var created = new Widget
        {
            Id = 102,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == 102).ToList();
        Assert.Equal(2, members.Count);
    }

    [Fact]
    public async Task UpdateAsync_AddsNewGroupIds_CreatesNewGroupMembers()
    {
        const int widgetId = 200;
        // Existing: widget in group 1 only
        _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = 1, WidgetId = widgetId });
        await _context.SaveChangesAsync();

        var existing = TestDataBuilder.CreateWidget(widgetId);
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.GroupIds = new List<int> { 1, 2, 3 }; // add groups 2 and 3

        var updated = BuildUpdatedWidget(dto, id: widgetId);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(widgetId)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        await _service.UpdateAsync(widgetId, dto);

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == widgetId).ToList();
        Assert.Equal(3, members.Count);
        Assert.Contains(members, m => m.WidgetGroupId == 1);
        Assert.Contains(members, m => m.WidgetGroupId == 2);
        Assert.Contains(members, m => m.WidgetGroupId == 3);
    }

    [Fact]
    public async Task UpdateAsync_RemovesOldGroupIds_DeletesGroupMembers()
    {
        const int widgetId = 201;
        // Existing: widget in groups 1, 2, 3
        _context.WidgetGroupMembers.AddRange(
            new WidgetGroupMember { WidgetGroupId = 1, WidgetId = widgetId },
            new WidgetGroupMember { WidgetGroupId = 2, WidgetId = widgetId },
            new WidgetGroupMember { WidgetGroupId = 3, WidgetId = widgetId }
        );
        await _context.SaveChangesAsync();

        var existing = TestDataBuilder.CreateWidget(widgetId);
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.GroupIds = new List<int> { 2 }; // keep only group 2

        var updated = BuildUpdatedWidget(dto, id: widgetId);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(widgetId)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        await _service.UpdateAsync(widgetId, dto);

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == widgetId).ToList();
        Assert.Single(members);
        Assert.Equal(2, members[0].WidgetGroupId);
    }

    [Fact]
    public async Task UpdateAsync_ClearsAllGroupIds_WhenEmptyGroupIdsPassed()
    {
        const int widgetId = 202;
        _context.WidgetGroupMembers.AddRange(
            new WidgetGroupMember { WidgetGroupId = 1, WidgetId = widgetId },
            new WidgetGroupMember { WidgetGroupId = 2, WidgetId = widgetId }
        );
        await _context.SaveChangesAsync();

        var existing = TestDataBuilder.CreateWidget(widgetId);
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.GroupIds = new List<int>(); // remove all groups

        var updated = BuildUpdatedWidget(dto, id: widgetId);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(widgetId)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        await _service.UpdateAsync(widgetId, dto);

        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == widgetId).ToList();
        Assert.Empty(members);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsWidgetsWithGroupIds()
    {
        const int widgetId1 = 300;
        const int widgetId2 = 301;
        _context.WidgetGroupMembers.AddRange(
            new WidgetGroupMember { WidgetGroupId = 1, WidgetId = widgetId1 },
            new WidgetGroupMember { WidgetGroupId = 2, WidgetId = widgetId1 },
            new WidgetGroupMember { WidgetGroupId = 3, WidgetId = widgetId2 }
        );
        await _context.SaveChangesAsync();

        var widgets = new List<Widget>
        {
            TestDataBuilder.CreateWidget(widgetId1),
            TestDataBuilder.CreateWidget(widgetId2)
        };
        _widgetRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(widgets);

        var result = (await _service.GetAllAsync()).ToList();

        var w1 = result.Single(w => w.Id == widgetId1);
        var w2 = result.Single(w => w.Id == widgetId2);
        Assert.Equal(2, w1.GroupIds.Count);
        Assert.Contains(1, w1.GroupIds);
        Assert.Contains(2, w1.GroupIds);
        Assert.Single(w2.GroupIds);
        Assert.Contains(3, w2.GroupIds);
    }

    // ─── Bước 1: Thông tin - WidgetType (tất cả loại hiển thị) ──────────────

    [Theory]
    [InlineData(WidgetType.Table)]
    [InlineData(WidgetType.Chart)]
    [InlineData(WidgetType.Metric)]
    [InlineData(WidgetType.Map)]
    [InlineData(WidgetType.Form)]
    public async Task CreateAsync_AllWidgetTypes_SetsCorrectWidgetType(WidgetType widgetType)
    {
        var dto = TestDataBuilder.CreateWidgetDto($"Widget {widgetType}");
        dto.WidgetType = widgetType;

        var created = new Widget
        {
            Id = (int)widgetType + 400,
            Name = dto.Name,
            WidgetType = widgetType,
            DataSourceId = dto.DataSourceId,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(widgetType, result.WidgetType);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.WidgetType == widgetType)), Times.Once);
    }

    // ─── Bước 2: Dữ liệu - Cache ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithCacheEnabled_SetsCacheSettings()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Widget Cache");
        dto.CacheEnabled = true;
        dto.CacheTtlMinutes = 60;

        var created = new Widget
        {
            Id = 500,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CacheEnabled = true,
            CacheTtlMinutes = 60,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.True(result.CacheEnabled);
        Assert.Equal(60, result.CacheTtlMinutes);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.CacheEnabled == true && w.CacheTtlMinutes == 60)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCacheDisabled_SetsCacheOff()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Widget No Cache");
        dto.CacheEnabled = false;
        dto.CacheTtlMinutes = 15;

        var created = new Widget
        {
            Id = 501,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CacheEnabled = false,
            CacheTtlMinutes = 15,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.False(result.CacheEnabled);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.CacheEnabled == false)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCacheEnabled_UpdatesCacheSettings()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        existing.CacheEnabled = false;
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.CacheEnabled = true;
        dto.CacheTtlMinutes = 120;

        var updated = BuildUpdatedWidget(dto, id: 1);
        updated.CacheEnabled = true;
        updated.CacheTtlMinutes = 120;
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.True(result.CacheEnabled);
        Assert.Equal(120, result.CacheTtlMinutes);
        _widgetRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Widget>(w => w.CacheEnabled == true && w.CacheTtlMinutes == 120)), Times.Once);
    }

    // ─── Bước 2: Dữ liệu - Configuration & ChartConfig ───────────────────────

    [Fact]
    public async Task CreateAsync_WithChartConfig_SetsChartConfig()
    {
        const string chartConfig = "{\"type\":\"bar\",\"xAxis\":\"thang\",\"yAxis\":\"doanh_thu\"}";
        var dto = TestDataBuilder.CreateWidgetDto("Biểu đồ doanh thu");
        dto.WidgetType = WidgetType.Chart;
        dto.ChartConfig = chartConfig;

        var created = new Widget
        {
            Id = 600,
            Name = dto.Name,
            WidgetType = WidgetType.Chart,
            DataSourceId = dto.DataSourceId,
            ChartConfig = chartConfig,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(chartConfig, result.ChartConfig);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.ChartConfig == chartConfig)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonChartWidget_ChartConfigIsNull()
    {
        var dto = TestDataBuilder.CreateWidgetDto("Bảng số liệu");
        dto.WidgetType = WidgetType.Table;
        dto.ChartConfig = null;

        var created = new Widget
        {
            Id = 601,
            Name = dto.Name,
            WidgetType = WidgetType.Table,
            DataSourceId = dto.DataSourceId,
            ChartConfig = null,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Null(result.ChartConfig);
    }

    // ─── Config Archive (Lịch sử cấu hình) ───────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenConfigurationChanged_CreatesArchive()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        existing.Configuration = "{\"query\":\"SELECT * FROM old_table\"}";
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.Configuration = "{\"query\":\"SELECT * FROM new_table\"}";

        var updated = BuildUpdatedWidget(dto, id: 1);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        await _service.UpdateAsync(1, dto);

        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.WidgetId == 1 &&
            a.TriggerSource == "OnSave" &&
            a.Configuration == "{\"query\":\"SELECT * FROM old_table\"}")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenChartConfigChanged_CreatesArchive()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        existing.ChartConfig = "{\"type\":\"bar\"}";
        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.ChartConfig = "{\"type\":\"line\"}";

        var updated = BuildUpdatedWidget(dto, id: 1);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        await _service.UpdateAsync(1, dto);

        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.WidgetId == 1 && a.ChartConfig == "{\"type\":\"bar\"}")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenConfigNotChanged_DoesNotCreateArchive()
    {
        const string config = "{\"query\":\"SELECT * FROM sales\"}";
        var existing = TestDataBuilder.CreateWidget(1);
        existing.Configuration = config;
        existing.ChartConfig = null;
        existing.HtmlTemplate = null;

        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.Configuration = config; // same
        dto.ChartConfig = null;     // same
        dto.HtmlTemplate = null;    // same

        var updated = BuildUpdatedWidget(dto, id: 1);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);

        await _service.UpdateAsync(1, dto);

        _archiveRepoMock.Verify(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()), Times.Never);
    }

    // ─── Bước 5: Idea Board - WidgetType.Form ─────────────────────────────────

    [Fact]
    public async Task CreateAsync_FormTypeWidget_SetsWidgetTypeToForm()
    {
        var dto = TestDataBuilder.CreateWidgetDtoWithMeta(name: "idea_board_feedback");
        dto.WidgetType = WidgetType.Form;
        dto.FriendlyLabel = "Hộp góp ý";
        dto.HelpText = "Nhận góp ý từ người dùng qua Idea Board";

        var created = new Widget
        {
            Id = 700,
            Name = dto.Name,
            FriendlyLabel = dto.FriendlyLabel,
            HelpText = dto.HelpText,
            WidgetType = WidgetType.Form,
            DataSourceId = dto.DataSourceId,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(WidgetType.Form, result.WidgetType);
        Assert.Equal("Hộp góp ý", result.FriendlyLabel);
        Assert.Equal("Nhận góp ý từ người dùng qua Idea Board", result.HelpText);
        _widgetRepoMock.Verify(r => r.CreateAsync(
            It.Is<Widget>(w => w.WidgetType == WidgetType.Form)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangeToFormType_UpdatesWidgetType()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        existing.WidgetType = WidgetType.Table;

        var dto = TestDataBuilder.UpdateWidgetDto();
        dto.WidgetType = WidgetType.Form;

        var updated = BuildUpdatedWidget(dto, id: 1);
        updated.WidgetType = WidgetType.Form;
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal(WidgetType.Form, result.WidgetType);
    }

    // ─── Kịch bản đầy đủ (toàn bộ form) ─────────────────────────────────────

    [Fact]
    public async Task FullWidgetFormFlow_CreateWithAllFields_MapsCorrectly()
    {
        // Mô phỏng người dùng điền đầy đủ WidgetConfigure form qua 5 bước
        var dto = new CreateWidgetDto
        {
            Name = "monthly_revenue_table",
            FriendlyLabel = "Báo cáo doanh thu tháng",
            HelpText = "Hiển thị doanh thu theo tháng của từng khu vực",
            Description = "Bảng tổng hợp doanh thu hàng tháng",
            WidgetType = WidgetType.Table,
            DataSourceId = 1,
            Configuration = "{\"query\":\"SELECT month, region, revenue FROM monthly_sales\"}",
            ChartConfig = null,
            HtmlTemplate = null,
            CacheEnabled = true,
            CacheTtlMinutes = 30,
            GroupIds = new List<int> { 1, 2 }
        };

        var created = new Widget
        {
            Id = 800,
            Name = dto.Name,
            FriendlyLabel = dto.FriendlyLabel,
            HelpText = dto.HelpText,
            Description = dto.Description,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            Configuration = dto.Configuration,
            ChartConfig = dto.ChartConfig,
            HtmlTemplate = dto.HtmlTemplate,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            IsActive = true,
            CreatedBy = "admin",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "admin");

        // Bước 1: Thông tin
        Assert.Equal("monthly_revenue_table", result.Name);
        Assert.Equal("Báo cáo doanh thu tháng", result.FriendlyLabel);
        Assert.Equal("Hiển thị doanh thu theo tháng của từng khu vực", result.HelpText);
        Assert.Equal(WidgetType.Table, result.WidgetType);
        Assert.Equal("admin", result.CreatedBy);

        // Bước 2: Dữ liệu
        Assert.Equal(1, result.DataSourceId);
        Assert.Equal("{\"query\":\"SELECT month, region, revenue FROM monthly_sales\"}", result.Configuration);
        Assert.True(result.CacheEnabled);
        Assert.Equal(30, result.CacheTtlMinutes);

        // Bước 1: GroupIds
        Assert.Equal(2, result.GroupIds.Count);
        Assert.Contains(1, result.GroupIds);
        Assert.Contains(2, result.GroupIds);
        var members = _context.WidgetGroupMembers.Where(m => m.WidgetId == 800).ToList();
        Assert.Equal(2, members.Count);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Widget BuildCreatedWidget(CreateWidgetDto dto, int id,
        string? friendlyLabel = null, string? helpText = null, string createdBy = "user1") =>
        new()
        {
            Id = id,
            Name = dto.Name,
            FriendlyLabel = friendlyLabel ?? dto.FriendlyLabel,
            HelpText = helpText ?? dto.HelpText,
            WidgetType = dto.WidgetType,
            Description = dto.Description,
            DataSourceId = dto.DataSourceId,
            Configuration = dto.Configuration,
            ChartConfig = dto.ChartConfig,
            HtmlTemplate = dto.HtmlTemplate,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            IsActive = true,
            CreatedBy = createdBy,
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };

    private static Widget BuildUpdatedWidget(UpdateWidgetDto dto, int id,
        string? friendlyLabel = null, string? helpText = null) =>
        new()
        {
            Id = id,
            Name = dto.Name,
            FriendlyLabel = friendlyLabel ?? dto.FriendlyLabel,
            HelpText = helpText ?? dto.HelpText,
            WidgetType = dto.WidgetType,
            Description = dto.Description,
            DataSourceId = dto.DataSourceId,
            Configuration = dto.Configuration,
            ChartConfig = dto.ChartConfig,
            HtmlTemplate = dto.HtmlTemplate,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            IsActive = dto.IsActive,
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
}
