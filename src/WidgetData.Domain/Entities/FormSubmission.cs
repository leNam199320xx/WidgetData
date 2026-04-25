namespace WidgetData.Domain.Entities;

/// <summary>
/// Lưu trữ dữ liệu được submit từ Form widget (WidgetType.Form với schema tùy chỉnh).
/// </summary>
public class FormSubmission
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    /// <summary>JSON blob chứa các field value do người dùng điền.</summary>
    public string Data { get; set; } = "{}";
    public string? SubmittedBy { get; set; }
    public string? IpAddress { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Widget Widget { get; set; } = null!;
}
