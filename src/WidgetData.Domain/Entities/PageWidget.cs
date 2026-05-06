namespace WidgetData.Domain.Entities;

/// <summary>
/// Liên kết giữa Page và Widget, xác định vị trí và chiều rộng hiển thị.
/// </summary>
public class PageWidget
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int WidgetId { get; set; }
    /// <summary>Thứ tự hiển thị trong trang (0-based).</summary>
    public int Position { get; set; } = 0;
    /// <summary>Số cột bootstrap chiếm (1-12).</summary>
    public int Width { get; set; } = 6;

    public Page Page { get; set; } = null!;
    public Widget Widget { get; set; } = null!;
}
