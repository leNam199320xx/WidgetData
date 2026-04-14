using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Tests.Services;

public class PermissionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _service = new PermissionService(_context, _userManagerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ApplicationUser CreateUser(string id, string email = "test@example.com") =>
        new() { Id = id, UserName = email, Email = email };

    private void SetupUser(string userId, ApplicationUser? user, IList<string>? roles = null)
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        if (user != null)
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles ?? new List<string>());
    }

    // ─── HasWidgetAccessAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task HasWidgetAccessAsync_UserNotFound_ReturnsFalse()
    {
        SetupUser("ghost", null);

        var result = await _service.HasWidgetAccessAsync("ghost", 1, "view");

        Assert.False(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_AdminUser_ReturnsTrue()
    {
        var user = CreateUser("admin1");
        SetupUser("admin1", user, new[] { "Admin" });

        var result = await _service.HasWidgetAccessAsync("admin1", 1, "view");

        Assert.True(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_DirectWidgetPermission_CanView_ReturnsTrue()
    {
        var user = CreateUser("u1");
        SetupUser("u1", user);
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "u1", WidgetId = 1, CanView = true, CanExecute = false, CanEdit = false
        });
        await _context.SaveChangesAsync();

        var result = await _service.HasWidgetAccessAsync("u1", 1, "view");

        Assert.True(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_DirectWidgetPermission_CannotEdit_ReturnsFalse()
    {
        var user = CreateUser("u1");
        SetupUser("u1", user);
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "u1", WidgetId = 1, CanView = true, CanExecute = false, CanEdit = false
        });
        await _context.SaveChangesAsync();

        var result = await _service.HasWidgetAccessAsync("u1", 1, "edit");

        Assert.False(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_GroupPermission_CanExecute_ReturnsTrue()
    {
        var user = CreateUser("u2");
        SetupUser("u2", user);

        // Create a group membership for widget 5
        var group = new WidgetGroup { Name = "Ops" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = group.Id, WidgetId = 5 });
        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "u2", GroupId = group.Id, CanView = true, CanExecute = true, CanEdit = false
        });
        await _context.SaveChangesAsync();

        var result = await _service.HasWidgetAccessAsync("u2", 5, "execute");

        Assert.True(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_NoPermissionAtAll_ReturnsFalse()
    {
        var user = CreateUser("u3");
        SetupUser("u3", user);

        var result = await _service.HasWidgetAccessAsync("u3", 99, "view");

        Assert.False(result);
    }

    [Fact]
    public async Task HasWidgetAccessAsync_UnknownAction_ReturnsFalse()
    {
        var user = CreateUser("u4");
        SetupUser("u4", user);
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "u4", WidgetId = 2, CanView = true, CanExecute = true, CanEdit = true
        });
        await _context.SaveChangesAsync();

        var result = await _service.HasWidgetAccessAsync("u4", 2, "destroy");

        Assert.False(result);
    }

    // ─── GetAccessibleWidgetIdsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAccessibleWidgetIdsAsync_UserNotFound_ReturnsEmpty()
    {
        SetupUser("ghost", null);

        var result = await _service.GetAccessibleWidgetIdsAsync("ghost");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccessibleWidgetIdsAsync_AdminUser_ReturnsAllWidgetIds()
    {
        var user = CreateUser("admin2");
        SetupUser("admin2", user, new[] { "Admin" });

        // Seed some widgets directly into the DbContext
        _context.DataSources.Add(new Domain.Entities.DataSource
        {
            Id = 1, Name = "DS", SourceType = Domain.Enums.DataSourceType.SQLite
        });
        await _context.SaveChangesAsync();
        _context.Widgets.AddRange(
            new Domain.Entities.Widget { Id = 1, Name = "W1", DataSourceId = 1, WidgetType = Domain.Enums.WidgetType.Chart },
            new Domain.Entities.Widget { Id = 2, Name = "W2", DataSourceId = 1, WidgetType = Domain.Enums.WidgetType.Table }
        );
        await _context.SaveChangesAsync();

        var result = (await _service.GetAccessibleWidgetIdsAsync("admin2")).ToList();

        Assert.Contains(1, result);
        Assert.Contains(2, result);
    }

    [Fact]
    public async Task GetAccessibleWidgetIdsAsync_RegularUser_ReturnsDirectAndGroupWidgets()
    {
        var user = CreateUser("u5");
        SetupUser("u5", user);

        // Direct permission for widget 10
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "u5", WidgetId = 10, CanView = true
        });

        // Group permission for widget 20
        var group = new WidgetGroup { Name = "G1" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();
        _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = group.Id, WidgetId = 20 });
        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "u5", GroupId = group.Id, CanView = true
        });
        await _context.SaveChangesAsync();

        var result = (await _service.GetAccessibleWidgetIdsAsync("u5")).ToList();

        Assert.Contains(10, result);
        Assert.Contains(20, result);
    }

    // ─── AssignWidgetPermissionAsync ──────────────────────────────────────────

    [Fact]
    public async Task AssignWidgetPermissionAsync_NewPermission_CreatesRecord()
    {
        var dto = new AssignWidgetPermissionDto
        {
            UserId = "u6", WidgetId = 1, CanView = true, CanExecute = false, CanEdit = false
        };

        var result = await _service.AssignWidgetPermissionAsync(dto);

        Assert.Equal("u6", result.UserId);
        Assert.Equal(1, result.WidgetId);
        Assert.True(result.CanView);
        Assert.Equal(1, await _context.UserWidgetPermissions.CountAsync());
    }

    [Fact]
    public async Task AssignWidgetPermissionAsync_ExistingPermission_UpdatesInsteadOfCreating()
    {
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "u7", WidgetId = 3, CanView = false, CanExecute = false, CanEdit = false
        });
        await _context.SaveChangesAsync();

        var dto = new AssignWidgetPermissionDto
        {
            UserId = "u7", WidgetId = 3, CanView = true, CanExecute = true, CanEdit = true
        };

        var result = await _service.AssignWidgetPermissionAsync(dto);

        Assert.True(result.CanView);
        Assert.True(result.CanExecute);
        Assert.True(result.CanEdit);
        // Should still be only one record
        Assert.Equal(1, await _context.UserWidgetPermissions.CountAsync());
    }

    // ─── AssignGroupPermissionAsync ───────────────────────────────────────────

    [Fact]
    public async Task AssignGroupPermissionAsync_NewPermission_CreatesRecord()
    {
        var group = new WidgetGroup { Name = "G2" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        var dto = new AssignGroupPermissionDto
        {
            UserId = "u8", GroupId = group.Id, CanView = true, CanExecute = false, CanEdit = false
        };

        var result = await _service.AssignGroupPermissionAsync(dto);

        Assert.Equal("u8", result.UserId);
        Assert.Equal(group.Id, result.GroupId);
        Assert.True(result.CanView);
        Assert.Equal(1, await _context.UserGroupPermissions.CountAsync());
    }

    [Fact]
    public async Task AssignGroupPermissionAsync_ExistingPermission_UpdatesInsteadOfCreating()
    {
        var group = new WidgetGroup { Name = "G3" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "u9", GroupId = group.Id, CanView = false, CanExecute = false, CanEdit = false
        });
        await _context.SaveChangesAsync();

        var dto = new AssignGroupPermissionDto
        {
            UserId = "u9", GroupId = group.Id, CanView = true, CanExecute = true, CanEdit = false
        };

        var result = await _service.AssignGroupPermissionAsync(dto);

        Assert.True(result.CanView);
        Assert.True(result.CanExecute);
        Assert.Equal(1, await _context.UserGroupPermissions.CountAsync());
    }

    // ─── RemoveWidgetPermissionAsync ──────────────────────────────────────────

    [Fact]
    public async Task RemoveWidgetPermissionAsync_ExistingPermission_RemovesAndReturnsTrue()
    {
        var perm = new UserWidgetPermission { UserId = "u10", WidgetId = 5, CanView = true };
        _context.UserWidgetPermissions.Add(perm);
        await _context.SaveChangesAsync();

        var result = await _service.RemoveWidgetPermissionAsync(perm.Id);

        Assert.True(result);
        Assert.Equal(0, await _context.UserWidgetPermissions.CountAsync());
    }

    [Fact]
    public async Task RemoveWidgetPermissionAsync_NonExistentPermission_ReturnsFalse()
    {
        var result = await _service.RemoveWidgetPermissionAsync(999);

        Assert.False(result);
    }

    // ─── RemoveGroupPermissionAsync ───────────────────────────────────────────

    [Fact]
    public async Task RemoveGroupPermissionAsync_ExistingPermission_RemovesAndReturnsTrue()
    {
        var group = new WidgetGroup { Name = "G4" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        var perm = new UserGroupPermission { UserId = "u11", GroupId = group.Id, CanView = true };
        _context.UserGroupPermissions.Add(perm);
        await _context.SaveChangesAsync();

        var result = await _service.RemoveGroupPermissionAsync(perm.Id);

        Assert.True(result);
        Assert.Equal(0, await _context.UserGroupPermissions.CountAsync());
    }

    [Fact]
    public async Task RemoveGroupPermissionAsync_NonExistentPermission_ReturnsFalse()
    {
        var result = await _service.RemoveGroupPermissionAsync(999);

        Assert.False(result);
    }

    // ─── GetWidgetPermissionsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetWidgetPermissionsAsync_ReturnsPermissionsForWidget()
    {
        // Add users and widgets so Include() can load navigation properties
        _context.Users.AddRange(CreateUser("uA"), CreateUser("uB"), CreateUser("uC"));
        _context.DataSources.Add(new Domain.Entities.DataSource
        {
            Id = 100, Name = "DS", SourceType = Domain.Enums.DataSourceType.SQLite
        });
        await _context.SaveChangesAsync();
        _context.Widgets.AddRange(
            new Domain.Entities.Widget { Id = 7, Name = "W7", DataSourceId = 100, WidgetType = Domain.Enums.WidgetType.Chart },
            new Domain.Entities.Widget { Id = 8, Name = "W8", DataSourceId = 100, WidgetType = Domain.Enums.WidgetType.Chart }
        );
        await _context.SaveChangesAsync();

        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "uA", WidgetId = 7, CanView = true, CanExecute = true, CanEdit = false
        });
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "uB", WidgetId = 7, CanView = true, CanExecute = false, CanEdit = false
        });
        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "uC", WidgetId = 8, CanView = true
        });
        await _context.SaveChangesAsync();

        var result = (await _service.GetWidgetPermissionsAsync(7)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(7, p.WidgetId));
    }

    // ─── GetGroupPermissionsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetGroupPermissionsAsync_ReturnsPermissionsForGroup()
    {
        // Add users so Include(p => p.User) can load navigation properties
        _context.Users.AddRange(CreateUser("uD"), CreateUser("uE"));
        var group = new WidgetGroup { Name = "G5" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "uD", GroupId = group.Id, CanView = true
        });
        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "uE", GroupId = group.Id, CanView = true, CanExecute = true
        });
        await _context.SaveChangesAsync();

        var result = (await _service.GetGroupPermissionsAsync(group.Id)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(group.Id, p.GroupId));
    }

    // ─── GetUserPermissionsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsCombinedWidgetAndGroupPermissions()
    {
        // Add user and widget so Include() can load navigation properties
        _context.Users.Add(CreateUser("uF"));
        _context.DataSources.Add(new Domain.Entities.DataSource
        {
            Id = 200, Name = "DS2", SourceType = Domain.Enums.DataSourceType.SQLite
        });
        await _context.SaveChangesAsync();
        _context.Widgets.Add(new Domain.Entities.Widget
        {
            Id = 1, Name = "W1", DataSourceId = 200, WidgetType = Domain.Enums.WidgetType.Chart
        });
        var group = new WidgetGroup { Name = "G6" };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        _context.UserWidgetPermissions.Add(new UserWidgetPermission
        {
            UserId = "uF", WidgetId = 1, CanView = true
        });
        _context.UserGroupPermissions.Add(new UserGroupPermission
        {
            UserId = "uF", GroupId = group.Id, CanView = true
        });
        await _context.SaveChangesAsync();

        var result = (await _service.GetUserPermissionsAsync("uF")).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("uF", p.UserId));
    }
}
