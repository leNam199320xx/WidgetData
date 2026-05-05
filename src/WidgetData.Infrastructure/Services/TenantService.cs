using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _repo;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TenantService(ITenantRepository repo, ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _repo = repo;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        var tenants = await _repo.GetAllAsync();
        var result = new List<TenantDto>();
        foreach (var t in tenants)
            result.Add(await MapWithCountsAsync(t));
        return result;
    }

    public async Task<TenantDto?> GetByIdAsync(int id)
    {
        var t = await _repo.GetByIdAsync(id);
        return t == null ? null : await MapWithCountsAsync(t);
    }

    public async Task<TenantDto?> GetBySlugAsync(string slug)
    {
        var t = await _repo.GetBySlugAsync(slug);
        return t == null ? null : await MapWithCountsAsync(t);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        var tenant = new Tenant
        {
            Name = dto.Name,
            Slug = dto.Slug.ToLowerInvariant(),
            Plan = dto.Plan,
            ContactEmail = dto.ContactEmail,
            IsActive = true
        };
        var created = await _repo.CreateAsync(tenant);
        return await MapWithCountsAsync(created);
    }

    public async Task<TenantDto?> UpdateAsync(int id, UpdateTenantDto dto)
    {
        var tenant = await _repo.GetByIdAsync(id);
        if (tenant == null) return null;

        tenant.Name = dto.Name;
        tenant.Plan = dto.Plan;
        tenant.ContactEmail = dto.ContactEmail;
        tenant.IsActive = dto.IsActive;
        var updated = await _repo.UpdateAsync(tenant);
        return await MapWithCountsAsync(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tenant = await _repo.GetByIdAsync(id);
        if (tenant == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var tenants = await _repo.GetAllAsync();
        var tenantList = tenants.ToList();

        var totalPages = await _context.Pages.IgnoreQueryFilters().CountAsync();
        var activePages = await _context.Pages.IgnoreQueryFilters().CountAsync(p => p.IsActive);
        var totalApiCalls = await _context.WidgetApiActivities.CountAsync();
        var totalForms = await _context.FormSubmissions.IgnoreQueryFilters().CountAsync();

        var tenantDtos = new List<TenantDto>();
        foreach (var t in tenantList)
            tenantDtos.Add(await MapWithCountsAsync(t));

        return new AdminStatsDto
        {
            TotalTenants = tenantList.Count,
            ActiveTenants = tenantList.Count(t => t.IsActive),
            TotalPages = totalPages,
            ActivePages = activePages,
            TotalApiCalls = totalApiCalls,
            TotalFormSubmissions = totalForms,
            Tenants = tenantDtos
        };
    }

    private async Task<TenantDto> MapWithCountsAsync(Tenant t)
    {
        var userCount = await _userManager.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == t.Id);
        var pageCount = await _context.Pages
            .IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == t.Id);
        var widgetCount = await _context.Widgets
            .IgnoreQueryFilters()
            .CountAsync(w => w.TenantId == t.Id);

        return new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            IsActive = t.IsActive,
            Plan = t.Plan,
            ContactEmail = t.ContactEmail,
            CreatedAt = t.CreatedAt,
            UserCount = userCount,
            PageCount = pageCount,
            WidgetCount = widgetCount
        };
    }
}
