using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface ITenantRepository
{
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(int id);
    Task<Tenant?> GetBySlugAsync(string slug);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task DeleteAsync(int id);
}
