namespace WidgetData.Application.DTOs;

public class WidgetGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<int> WidgetIds { get; set; } = new List<int>();
}

public class CreateWidgetGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IList<int> WidgetIds { get; set; } = new List<int>();
}

public class UpdateWidgetGroupDto : CreateWidgetGroupDto
{
    public bool IsActive { get; set; } = true;
}
