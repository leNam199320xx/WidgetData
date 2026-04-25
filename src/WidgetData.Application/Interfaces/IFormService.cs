using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IFormService
{
    /// <summary>Lấy schema (field definitions) của form từ Widget.Configuration.</summary>
    Task<FormSchemaDto?> GetSchemaAsync(int widgetId);

    /// <summary>Submit dữ liệu form, lưu vào FormSubmissions.</summary>
    Task<FormSubmissionDto> SubmitAsync(CreateFormSubmissionDto dto, string? submittedBy, string? ipAddress);

    /// <summary>Lấy danh sách submissions của một widget (admin).</summary>
    Task<IEnumerable<FormSubmissionDto>> GetSubmissionsAsync(int widgetId);

    /// <summary>Xóa một submission.</summary>
    Task<bool> DeleteSubmissionAsync(int id);
}
