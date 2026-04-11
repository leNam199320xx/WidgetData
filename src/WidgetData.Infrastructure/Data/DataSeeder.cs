using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        string[] roles = { "Admin", "Manager", "Developer", "Viewer" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await _userManager.Users.AnyAsync())
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@widgetdata.com",
                Email = "admin@widgetdata.com",
                DisplayName = "Admin User",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await _userManager.CreateAsync(admin, "Admin@123!");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await _context.DataSources.AnyAsync())
        {
            var ds = new DataSource
            {
                Name = "Sample SQLite",
                SourceType = DataSourceType.SQLite,
                Description = "Sample SQLite data source",
                ConnectionString = "Data Source=sample.db",
                IsActive = true,
                CreatedBy = "system"
            };
            _context.DataSources.Add(ds);
            await _context.SaveChangesAsync();

            var widget = new Widget
            {
                Name = "Sample Widget",
                WidgetType = WidgetType.Table,
                Description = "A sample widget",
                DataSourceId = ds.Id,
                Configuration = "{}",
                IsActive = true,
                CreatedBy = "system"
            };
            _context.Widgets.Add(widget);
            await _context.SaveChangesAsync();
        }
    }
}
