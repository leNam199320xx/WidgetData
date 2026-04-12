using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class WidgetGroupService : IWidgetGroupService
{
    private readonly ApplicationDbContext _context;

    public WidgetGroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WidgetGroupDto>> GetAllAsync()
    {
        var groups = await _context.WidgetGroups
            .Include(g => g.Members)
            .ToListAsync();
        return groups.Select(MapToDto);
    }

    public async Task<WidgetGroupDto?> GetByIdAsync(int id)
    {
        var group = await _context.WidgetGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);
        return group == null ? null : MapToDto(group);
    }

    public async Task<WidgetGroupDto> CreateAsync(CreateWidgetGroupDto dto, string userId)
    {
        var group = new WidgetGroup
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedBy = userId
        };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        foreach (var widgetId in dto.WidgetIds.Distinct())
        {
            _context.WidgetGroupMembers.Add(new WidgetGroupMember
            {
                WidgetGroupId = group.Id,
                WidgetId = widgetId
            });
        }
        await _context.SaveChangesAsync();

        return await GetByIdAsync(group.Id) ?? MapToDto(group);
    }

    public async Task<WidgetGroupDto?> UpdateAsync(int id, UpdateWidgetGroupDto dto)
    {
        var group = await _context.WidgetGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return null;

        group.Name = dto.Name;
        group.Description = dto.Description;
        group.IsActive = dto.IsActive;
        group.UpdatedAt = DateTime.UtcNow;

        // Sync members
        var existing = group.Members.Select(m => m.WidgetId).ToHashSet();
        var desired = dto.WidgetIds.ToHashSet();

        foreach (var toRemove in existing.Except(desired))
            _context.WidgetGroupMembers.Remove(group.Members.First(m => m.WidgetId == toRemove));

        foreach (var toAdd in desired.Except(existing))
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = id, WidgetId = toAdd });

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await _context.WidgetGroups.FindAsync(id);
        if (group == null) return false;
        _context.WidgetGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
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
