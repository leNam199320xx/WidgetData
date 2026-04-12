using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasWidgetAccessAsync(string userId, int widgetId, string action);
    Task<IEnumerable<int>> GetAccessibleWidgetIdsAsync(string userId);
    Task<IEnumerable<UserPermissionDto>> GetWidgetPermissionsAsync(int widgetId);
    Task<IEnumerable<UserPermissionDto>> GetGroupPermissionsAsync(int groupId);
    Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(string userId);
    Task<UserPermissionDto> AssignWidgetPermissionAsync(AssignWidgetPermissionDto dto);
    Task<UserPermissionDto> AssignGroupPermissionAsync(AssignGroupPermissionDto dto);
    Task<bool> RemoveWidgetPermissionAsync(int permissionId);
    Task<bool> RemoveGroupPermissionAsync(int permissionId);
}
