namespace WidgetData.Application.DTOs;

public class UserPermissionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public int? WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool CanView { get; set; }
    public bool CanExecute { get; set; }
    public bool CanEdit { get; set; }
}

public class AssignWidgetPermissionDto
{
    public string UserId { get; set; } = string.Empty;
    public int WidgetId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanExecute { get; set; } = false;
    public bool CanEdit { get; set; } = false;
}

public class AssignGroupPermissionDto
{
    public string UserId { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanExecute { get; set; } = false;
    public bool CanEdit { get; set; } = false;
}
