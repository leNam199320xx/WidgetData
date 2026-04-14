using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repo;
    private readonly IWidgetConfigArchiveService _archiveService;

    public ScheduleService(IScheduleRepository repo, IWidgetConfigArchiveService archiveService)
    {
        _repo = repo;
        _archiveService = archiveService;
    }

    public async Task<IEnumerable<WidgetScheduleDto>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<WidgetScheduleDto> CreateAsync(CreateScheduleDto dto)
    {
        var entity = new WidgetSchedule
        {
            WidgetId = dto.WidgetId,
            CronExpression = dto.CronExpression,
            Timezone = dto.Timezone,
            IsEnabled = dto.IsEnabled,
            RetryOnFailure = dto.RetryOnFailure,
            MaxRetries = dto.MaxRetries,
            ArchiveConfigOnRun = dto.ArchiveConfigOnRun
        };
        var created = await _repo.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<WidgetScheduleDto?> UpdateAsync(int id, UpdateScheduleDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;
        entity.CronExpression = dto.CronExpression;
        entity.Timezone = dto.Timezone;
        entity.IsEnabled = dto.IsEnabled;
        entity.RetryOnFailure = dto.RetryOnFailure;
        entity.MaxRetries = dto.MaxRetries;
        entity.ArchiveConfigOnRun = dto.ArchiveConfigOnRun;
        entity.UpdatedAt = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    public async Task<bool> EnableAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsEnabled = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return true;
    }

    public async Task<bool> DisableAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsEnabled = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return true;
    }

    public async Task<WidgetScheduleDto?> TriggerAsync(int id, string triggeredBy)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;

        entity.LastRunAt = DateTime.UtcNow;
        entity.LastRunStatus = WidgetData.Domain.Enums.ExecutionStatus.Success;
        entity.UpdatedAt = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(entity);

        if (entity.ArchiveConfigOnRun)
            await _archiveService.CreateForScheduleAsync(entity.WidgetId, entity.Id, triggeredBy);

        return MapToDto(updated);
    }

    private static WidgetScheduleDto MapToDto(WidgetSchedule s) => new()
    {
        Id = s.Id,
        WidgetId = s.WidgetId,
        WidgetName = s.Widget?.Name,
        CronExpression = s.CronExpression,
        Timezone = s.Timezone,
        IsEnabled = s.IsEnabled,
        RetryOnFailure = s.RetryOnFailure,
        MaxRetries = s.MaxRetries,
        ArchiveConfigOnRun = s.ArchiveConfigOnRun,
        LastRunAt = s.LastRunAt,
        LastRunStatus = s.LastRunStatus,
        NextRunAt = s.NextRunAt,
        CreatedAt = s.CreatedAt
    };
}
