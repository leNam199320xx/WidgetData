using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Widgets;
using WidgetData.CrossCutting.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class WidgetCrudServiceTests
{
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly Mock<IJsonWidgetGroupMemberRepository> _groupMemberRepoMock;
    private readonly Mock<IWidgetConfigArchiveRepository> _archiveRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly WidgetCrudService _service;

    public WidgetCrudServiceTests()
    {
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _groupMemberRepoMock = new Mock<IJsonWidgetGroupMemberRepository>();
        _archiveRepoMock = new Mock<IWidgetConfigArchiveRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.WidgetGroups.AddRange(
            TestDataBuilder.CreateWidgetGroup(1, "Group 1"),
            TestDataBuilder.CreateWidgetGroup(2, "Group 2"));
        _context.SaveChanges();

        var loggerMock = new Mock<ILogger<WidgetCrudService>>();
        _service = new WidgetCrudService(_widgetRepoMock.Object, _groupMemberRepoMock.Object,
            _archiveRepoMock.Object, _auditServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_ConfigChanged_CreatesArchive()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto("Updated");
        var updated = new Widget
        {
            Id = 1,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            IsActive = dto.IsActive,
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _groupMemberRepoMock.Setup(r => r.GetByWidgetAsync(1)).ReturnsAsync(new List<WidgetGroupMember>());
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.WidgetId == 1 && a.TriggerSource == "OnSave")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConfigUnchanged_DoesNotCreateArchive()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto("Updated Name Only");
        dto.Configuration = existing.Configuration;
        dto.ChartConfig = existing.ChartConfig;
        dto.HtmlTemplate = existing.HtmlTemplate;
        var updated = new Widget
        {
            Id = 1,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            IsActive = dto.IsActive
        };
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _groupMemberRepoMock.Setup(r => r.GetByWidgetAsync(1)).ReturnsAsync(new List<WidgetGroupMember>());

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        _archiveRepoMock.Verify(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()), Times.Never);
    }
}