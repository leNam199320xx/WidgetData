namespace WidgetData.Domain.Entities;

public class UserGroupPermission
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanExecute { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public WidgetGroup Group { get; set; } = null!;
}
