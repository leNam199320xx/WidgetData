namespace WidgetData.Application.DTOs;

// ─── Schema / Field definitions ──────────────────────────────────────────────

/// <summary>
/// Định nghĩa một field trong form widget.
/// Lưu trong Widget.Configuration dưới dạng JSON array "fields".
/// </summary>
public class FormFieldDto
{
    /// <summary>Tên kỹ thuật, dùng làm key khi submit (VD: "full_name").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Nhãn hiển thị cho người dùng.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Loại input: text | email | tel | number | textarea | select | checkbox | date.</summary>
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    /// <summary>Placeholder text.</summary>
    public string? Placeholder { get; set; }
    /// <summary>Dành cho type=select: danh sách options phân cách bởi ";".</summary>
    public string? Options { get; set; }
}

/// <summary>
/// Schema đầy đủ của form, parse từ Widget.Configuration khi WidgetType = Form.
/// </summary>
public class FormSchemaDto
{
    public int WidgetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IList<FormFieldDto> Fields { get; set; } = new List<FormFieldDto>();
    public string SubmitLabel { get; set; } = "Gửi";
    public string SuccessMessage { get; set; } = "Cảm ơn bạn đã gửi thông tin!";
}

// ─── Submission ───────────────────────────────────────────────────────────────

public class FormSubmissionDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    /// <summary>JSON object chứa field values.</summary>
    public string Data { get; set; } = "{}";
    public string? SubmittedBy { get; set; }
    public string? IpAddress { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class CreateFormSubmissionDto
{
    public int WidgetId { get; set; }
    /// <summary>Key-value của các field trong form.</summary>
    public Dictionary<string, string?> Data { get; set; } = new();
}
