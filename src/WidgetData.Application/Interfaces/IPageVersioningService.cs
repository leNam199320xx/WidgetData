using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;

namespace WidgetData.Application.Interfaces;

public interface IPageVersioningService
{
    Task SaveSnapshotAsync(Page page, string createdBy, string action, string? note);
    Task<PageDto?> PublishAsync(int id, string userId, PublishPageDto? dto = null);
    Task<PageDto?> RollbackAsync(int id, int versionNumber, string userId, RollbackPageDto? dto = null);
    Task<IEnumerable<PageVersionDto>> GetVersionsAsync(int id);
}