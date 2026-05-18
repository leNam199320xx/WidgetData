using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data.Json.Repositories;

namespace WidgetData.Infrastructure.Services;

public class WidgetGroupService : IWidgetGroupService
{
    private readonly IJsonWidgetGroupRepository _groupRepo;
    private readonly IJsonWidgetGroupMemberRepository _memberRepo;

    public WidgetGroupService(
        IJsonWidgetGroupRepository groupRepo,
        IJsonWidgetGroupMemberRepository memberRepo)
    {
        _groupRepo = groupRepo;
        _memberRepo = memberRepo;
    }

    public async Task<IEnumerable<WidgetGroupDto>> GetAllAsync()
    {
        var groups = await _groupRepo.GetAllAsync();
        return groups.Select(MapToDto);
    }

    public async Task<WidgetGroupDto?> GetByIdAsync(int id)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        return group == null ? null : MapToDto(group);
    }

    public async Task<WidgetGroupDto> CreateAsync(CreateWidgetGroupDto dto, string userId)
    {
        var all = await _groupRepo.GetAllAsync();
        var newId = all.Any() ? all.Max(g => g.Id) + 1 : 1;

        var group = new WidgetGroup
        {
            Id = newId,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedBy = userId
        };
        await _groupRepo.CreateAsync(group);

        foreach (var widgetId in dto.WidgetIds.Distinct())
        {
            await _memberRepo.CreateAsync(new WidgetGroupMember
            {
                WidgetGroupId = group.Id,
                WidgetId = widgetId
            });
        }

        return (await GetByIdAsync(group.Id)) ?? MapToDto(group);
    }

    public async Task<WidgetGroupDto?> UpdateAsync(int id, UpdateWidgetGroupDto dto)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null) return null;

        group.Name = dto.Name;
        group.Description = dto.Description;
        group.IsActive = dto.IsActive;
        group.UpdatedAt = DateTime.UtcNow;
        await _groupRepo.UpdateAsync(group);

        // Sync members  — composite key: groupId * 1_000_000 + widgetId
        var existing = (await _memberRepo.GetByGroupAsync(id)).ToList();
        var existingWidgetIds = existing.Select(m => m.WidgetId).ToHashSet();
        var desired = dto.WidgetIds.ToHashSet();

        foreach (var toRemove in existing.Where(m => !desired.Contains(m.WidgetId)))
        {
            var compositeId = unchecked(toRemove.WidgetGroupId * 1_000_000 + toRemove.WidgetId);
            await _memberRepo.DeleteAsync(compositeId);
        }

        foreach (var toAdd in desired.Except(existingWidgetIds))
        {
            await _memberRepo.CreateAsync(new WidgetGroupMember
            {
                WidgetGroupId = id,
                WidgetId = toAdd
            });
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var members = await _memberRepo.GetByGroupAsync(id);
        foreach (var m in members)
        {
            var compositeId = unchecked(m.WidgetGroupId * 1_000_000 + m.WidgetId);
            await _memberRepo.DeleteAsync(compositeId);
        }
        return await _groupRepo.DeleteAsync(id);
    }

    private static WidgetGroupDto MapToDto(WidgetGroup g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Description = g.Description,
        IsActive = g.IsActive,
        CreatedBy = g.CreatedBy,
        CreatedAt = g.CreatedAt,
        WidgetIds = g.Members.Select(m => m.WidgetId).ToList()
    };
}
