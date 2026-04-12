using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> HasWidgetAccessAsync(string userId, int widgetId, string action)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin")) return true;

        // Check direct widget permission
        var widgetPerm = await _context.UserWidgetPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.WidgetId == widgetId);
        if (widgetPerm != null)
            return action.ToLower() switch
            {
                "view" => widgetPerm.CanView,
                "execute" => widgetPerm.CanExecute,
                "edit" => widgetPerm.CanEdit,
                _ => false
            };

        // Check group permissions
        var groupIds = await _context.WidgetGroupMembers
            .Where(m => m.WidgetId == widgetId)
            .Select(m => m.WidgetGroupId)
            .ToListAsync();

        if (!groupIds.Any()) return false;

        var groupPerms = await _context.UserGroupPermissions
            .Where(p => p.UserId == userId && groupIds.Contains(p.GroupId))
            .ToListAsync();

        return action.ToLower() switch
        {
            "view" => groupPerms.Any(p => p.CanView),
            "execute" => groupPerms.Any(p => p.CanExecute),
            "edit" => groupPerms.Any(p => p.CanEdit),
            _ => false
        };
    }

    public async Task<IEnumerable<int>> GetAccessibleWidgetIdsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Enumerable.Empty<int>();

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            return await _context.Widgets.Select(w => w.Id).ToListAsync();
        }

        var directIds = await _context.UserWidgetPermissions
            .Where(p => p.UserId == userId && p.CanView)
            .Select(p => p.WidgetId)
            .ToListAsync();

        var groupIds = await _context.UserGroupPermissions
            .Where(p => p.UserId == userId && p.CanView)
            .Select(p => p.GroupId)
            .ToListAsync();

        var groupWidgetIds = await _context.WidgetGroupMembers
            .Where(m => groupIds.Contains(m.WidgetGroupId))
            .Select(m => m.WidgetId)
            .ToListAsync();

        return directIds.Union(groupWidgetIds).Distinct();
    }

    public async Task<IEnumerable<UserPermissionDto>> GetWidgetPermissionsAsync(int widgetId)
    {
        var perms = await _context.UserWidgetPermissions
            .Include(p => p.User)
            .Include(p => p.Widget)
            .Where(p => p.WidgetId == widgetId)
            .ToListAsync();
        return perms.Select(p => new UserPermissionDto
        {
            Id = p.Id,
            UserId = p.UserId,
            UserEmail = p.User?.Email,
            WidgetId = p.WidgetId,
            WidgetName = p.Widget?.Name,
            CanView = p.CanView,
            CanExecute = p.CanExecute,
            CanEdit = p.CanEdit
        });
    }

    public async Task<IEnumerable<UserPermissionDto>> GetGroupPermissionsAsync(int groupId)
    {
        var perms = await _context.UserGroupPermissions
            .Include(p => p.User)
            .Include(p => p.Group)
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
        return perms.Select(p => new UserPermissionDto
        {
            Id = p.Id,
            UserId = p.UserId,
            UserEmail = p.User?.Email,
            GroupId = p.GroupId,
            GroupName = p.Group?.Name,
            CanView = p.CanView,
            CanExecute = p.CanExecute,
            CanEdit = p.CanEdit
        });
    }

    public async Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(string userId)
    {
        var widgetPerms = await _context.UserWidgetPermissions
            .Include(p => p.Widget)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var groupPerms = await _context.UserGroupPermissions
            .Include(p => p.Group)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var result = widgetPerms.Select(p => new UserPermissionDto
        {
            Id = p.Id,
            UserId = p.UserId,
            WidgetId = p.WidgetId,
            WidgetName = p.Widget?.Name,
            CanView = p.CanView,
            CanExecute = p.CanExecute,
            CanEdit = p.CanEdit
        }).ToList();

        result.AddRange(groupPerms.Select(p => new UserPermissionDto
        {
            Id = p.Id,
            UserId = p.UserId,
            GroupId = p.GroupId,
            GroupName = p.Group?.Name,
            CanView = p.CanView,
            CanExecute = p.CanExecute,
            CanEdit = p.CanEdit
        }));

        return result;
    }

    public async Task<UserPermissionDto> AssignWidgetPermissionAsync(AssignWidgetPermissionDto dto)
    {
        var existing = await _context.UserWidgetPermissions
            .FirstOrDefaultAsync(p => p.UserId == dto.UserId && p.WidgetId == dto.WidgetId);

        if (existing != null)
        {
            existing.CanView = dto.CanView;
            existing.CanExecute = dto.CanExecute;
            existing.CanEdit = dto.CanEdit;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var widget = await _context.Widgets.FindAsync(dto.WidgetId);
            return new UserPermissionDto
            {
                Id = existing.Id, UserId = existing.UserId, WidgetId = existing.WidgetId,
                WidgetName = widget?.Name, CanView = existing.CanView,
                CanExecute = existing.CanExecute, CanEdit = existing.CanEdit
            };
        }

        var perm = new UserWidgetPermission
        {
            UserId = dto.UserId,
            WidgetId = dto.WidgetId,
            CanView = dto.CanView,
            CanExecute = dto.CanExecute,
            CanEdit = dto.CanEdit
        };
        _context.UserWidgetPermissions.Add(perm);
        await _context.SaveChangesAsync();

        var w = await _context.Widgets.FindAsync(dto.WidgetId);
        return new UserPermissionDto
        {
            Id = perm.Id, UserId = perm.UserId, WidgetId = perm.WidgetId,
            WidgetName = w?.Name, CanView = perm.CanView,
            CanExecute = perm.CanExecute, CanEdit = perm.CanEdit
        };
    }

    public async Task<UserPermissionDto> AssignGroupPermissionAsync(AssignGroupPermissionDto dto)
    {
        var existing = await _context.UserGroupPermissions
            .FirstOrDefaultAsync(p => p.UserId == dto.UserId && p.GroupId == dto.GroupId);

        if (existing != null)
        {
            existing.CanView = dto.CanView;
            existing.CanExecute = dto.CanExecute;
            existing.CanEdit = dto.CanEdit;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var group = await _context.WidgetGroups.FindAsync(dto.GroupId);
            return new UserPermissionDto
            {
                Id = existing.Id, UserId = existing.UserId, GroupId = existing.GroupId,
                GroupName = group?.Name, CanView = existing.CanView,
                CanExecute = existing.CanExecute, CanEdit = existing.CanEdit
            };
        }

        var perm = new UserGroupPermission
        {
            UserId = dto.UserId,
            GroupId = dto.GroupId,
            CanView = dto.CanView,
            CanExecute = dto.CanExecute,
            CanEdit = dto.CanEdit
        };
        _context.UserGroupPermissions.Add(perm);
        await _context.SaveChangesAsync();

        var g = await _context.WidgetGroups.FindAsync(dto.GroupId);
        return new UserPermissionDto
        {
            Id = perm.Id, UserId = perm.UserId, GroupId = perm.GroupId,
            GroupName = g?.Name, CanView = perm.CanView,
            CanExecute = perm.CanExecute, CanEdit = perm.CanEdit
        };
    }

    public async Task<bool> RemoveWidgetPermissionAsync(int permissionId)
    {
        var perm = await _context.UserWidgetPermissions.FindAsync(permissionId);
        if (perm == null) return false;
        _context.UserWidgetPermissions.Remove(perm);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveGroupPermissionAsync(int permissionId)
    {
        var perm = await _context.UserGroupPermissions.FindAsync(permissionId);
        if (perm == null) return false;
        _context.UserGroupPermissions.Remove(perm);
        await _context.SaveChangesAsync();
        return true;
    }
}
