using WidgetData.Application.DTOs;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IPageCrudService
{
    Task<IEnumerable<PageDto>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null, bool includeWidgetContent = true);
    Task<PageDto?> GetByIdAsync(int id);
    Task<PageDto?> GetBySlugAsync(string slug, int? tenantId = null);
    Task<PageDto> CreateAsync(CreatePageDto dto, int tenantId, string createdBy);
    Task<PageDto?> UpdateAsync(int id, UpdatePageDto dto);
    Task<bool> DeleteAsync(int id);
}