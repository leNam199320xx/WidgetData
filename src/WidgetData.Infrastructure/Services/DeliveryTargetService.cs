using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DeliveryTargetService : IDeliveryTargetService
{
    private readonly IDeliveryTargetRepository _repo;
    private readonly ILogger _logger;

    public DeliveryTargetService(IDeliveryTargetRepository repo, ILogger logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId)
    {
        var targets = await _repo.GetByWidgetAsync(widgetId);
        return targets.Select(MapToDto);
    }

    public async Task<DeliveryTargetDto?> GetTargetByIdAsync(int id)
    {
        var target = await _repo.GetByIdAsync(id);
        return target == null ? null : MapToDto(target);
    }

    public async Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId)
    {
        var target = new DeliveryTarget
        {
            WidgetId = dto.WidgetId,
            Name = dto.Name,
            Type = dto.Type,
            Configuration = dto.Configuration,
            IsEnabled = dto.IsEnabled,
            CreatedBy = userId
        };
        var created = await _repo.CreateAsync(target);
        return MapToDto(created);
    }

    public async Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto)
    {
        var target = await _repo.GetByIdAsync(id);
        if (target == null) return null;
        target.Name = dto.Name;
        target.Type = dto.Type;
        target.Configuration = dto.Configuration;
        target.IsEnabled = dto.IsEnabled;
        target.UpdatedAt = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(target);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteTargetAsync(int id)
    {
        var target = await _repo.GetByIdAsync(id);
        if (target == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    private static DeliveryTargetDto MapToDto(DeliveryTarget t) => new()
    {
        Id = t.Id,
        WidgetId = t.WidgetId,
        Name = t.Name,
        Type = t.Type,
        Configuration = t.Configuration,
        IsEnabled = t.IsEnabled,
        CreatedBy = t.CreatedBy,
        CreatedAt = t.CreatedAt
    };
}