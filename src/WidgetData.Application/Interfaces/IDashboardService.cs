using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}
