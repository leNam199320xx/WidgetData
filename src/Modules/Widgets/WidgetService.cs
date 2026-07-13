using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

public class WidgetService : IWidgetService
{
    private readonly IWidgetCrudService _crud;
    private readonly IWidgetExecutionService _execution;

    public WidgetService(IWidgetRepository widgetRepo, IExecutionRepository executionRepo,
        IJsonWidgetGroupMemberRepository groupMemberRepo, IWidgetConfigArchiveRepository archiveRepo,
        IScheduleRepository scheduleRepo, IAuditService auditService, ILogger<WidgetService> logger,
        IHttpClientFactory httpClientFactory, IEnumerable<IDataSourceStrategy> strategies,
        ITenantContext? tenantContext = null)
    {
        _crud = new WidgetCrudService(widgetRepo, groupMemberRepo, archiveRepo, auditService, logger, tenantContext);
        _execution = new WidgetExecutionService(widgetRepo, executionRepo, archiveRepo, scheduleRepo, auditService, logger, httpClientFactory,
            strategies.ToArray(), tenantContext);
    }

    public Task<IEnumerable<WidgetDto>> GetAllAsync() => _crud.GetAllAsync();
    public Task<WidgetDto?> GetByIdAsync(int id) => _crud.GetByIdAsync(id);
    public Task<WidgetDto> CreateAsync(CreateWidgetDto dto, string userId) => _crud.CreateAsync(dto, userId);
    public Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto) => _crud.UpdateAsync(id, dto);
    public Task<bool> DeleteAsync(int id) => _crud.DeleteAsync(id);
    public Task<WidgetExecutionDto> ExecuteAsync(int id, string userId, int? scheduleId = null) => _execution.ExecuteAsync(id, userId, scheduleId);
    public Task<object?> GetDataAsync(int id) => _execution.GetDataAsync(id);
    public Task<IEnumerable<WidgetExecutionDto>> GetHistoryAsync(int id) => _execution.GetHistoryAsync(id);
}
