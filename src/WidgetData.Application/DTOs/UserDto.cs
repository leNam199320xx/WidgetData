using System.ComponentModel.DataAnnotations;

namespace WidgetData.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public int? TenantId { get; set; }
}

public class UpdateUserDto
{
    [StringLength(100)]
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ChangeUserPasswordDto
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class SetUserRolesDto
{
    /// <summary>Danh sách các role sẽ được gán cho user (thay thế hoàn toàn các role hiện tại).</summary>
    public IList<string> Roles { get; set; } = new List<string>();
}

public class AssignUserToTenantDto
{
    /// <summary>ID của user cần gán vào tenant.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Role trong tenant: TenantAdmin hoặc TenantUser (mặc định).</summary>
    [StringLength(50)]
    public string Role { get; set; } = "TenantUser";
}
