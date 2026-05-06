using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface ITenantService
{
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto?> GetByIdAsync(int id);
    Task<TenantDto?> GetBySlugAsync(string slug);
    Task<TenantDto> CreateAsync(CreateTenantDto dto);
    Task<TenantDto?> UpdateAsync(int id, UpdateTenantDto dto);
    Task<bool> DeleteAsync(int id);
    Task<AdminStatsDto> GetAdminStatsAsync();

    // User management per tenant
    Task<IEnumerable<UserDto>> GetUsersAsync(int tenantId);
    Task<UserDto?> AssignUserAsync(int tenantId, AssignUserToTenantDto dto);
    Task<bool> RemoveUserAsync(int tenantId, string userId);
}
