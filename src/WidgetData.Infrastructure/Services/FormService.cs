using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class FormService : IFormService
{
    private readonly ApplicationDbContext _context;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public FormService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FormSchemaDto?> GetSchemaAsync(int widgetId)
    {
        var widget = await _context.Widgets
            .Include(w => w.DataSource)
            .FirstOrDefaultAsync(w => w.Id == widgetId && w.IsActive);

        if (widget == null || widget.WidgetType != WidgetType.Form)
            return null;

        return ParseSchema(widget);
    }

    public async Task<FormSubmissionDto> SubmitAsync(CreateFormSubmissionDto dto, string? submittedBy, string? ipAddress)
    {
        var widget = await _context.Widgets.FirstOrDefaultAsync(w => w.Id == dto.WidgetId && w.IsActive)
            ?? throw new KeyNotFoundException($"Form widget {dto.WidgetId} not found or inactive.");

        if (widget.WidgetType != WidgetType.Form)
            throw new InvalidOperationException("Widget is not a Form widget.");

        // Validate required fields against schema
        var schema = ParseSchema(widget);
        if (schema != null)
        {
            foreach (var field in schema.Fields.Where(f => f.Required))
            {
                if (!dto.Data.TryGetValue(field.Name, out var val) || string.IsNullOrWhiteSpace(val))
                    throw new ArgumentException($"Trường '{field.Label}' là bắt buộc.");
            }
        }

        var submission = new FormSubmission
        {
            WidgetId = dto.WidgetId,
            Data = JsonSerializer.Serialize(dto.Data),
            SubmittedBy = submittedBy,
            IpAddress = ipAddress,
            SubmittedAt = DateTime.UtcNow
        };

        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        return MapToDto(submission);
    }

    public async Task<IEnumerable<FormSubmissionDto>> GetSubmissionsAsync(int widgetId)
    {
        var submissions = await _context.FormSubmissions
            .Where(s => s.WidgetId == widgetId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return submissions.Select(MapToDto);
    }

    public async Task<bool> DeleteSubmissionAsync(int id)
    {
        var sub = await _context.FormSubmissions.FindAsync(id);
        if (sub == null) return false;
        _context.FormSubmissions.Remove(sub);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static FormSchemaDto ParseSchema(Widget widget)
    {
        var schema = new FormSchemaDto
        {
            WidgetId = widget.Id,
            Title = widget.FriendlyLabel ?? widget.Name,
            Description = widget.Description
        };

        if (!string.IsNullOrWhiteSpace(widget.Configuration))
        {
            try
            {
                using var doc = JsonDocument.Parse(widget.Configuration);
                var root = doc.RootElement;

                if (root.TryGetProperty("fields", out var fieldsEl))
                {
                    schema.Fields = fieldsEl.Deserialize<List<FormFieldDto>>() ?? new List<FormFieldDto>();
                }
                if (root.TryGetProperty("submitLabel", out var labelEl))
                    schema.SubmitLabel = labelEl.GetString() ?? "Gửi";
                if (root.TryGetProperty("successMessage", out var msgEl))
                    schema.SuccessMessage = msgEl.GetString() ?? "Cảm ơn bạn đã gửi thông tin!";
            }
            catch
            {
                // Malformed config — return empty schema
            }
        }

        return schema;
    }

    private static FormSubmissionDto MapToDto(FormSubmission s) => new()
    {
        Id = s.Id,
        WidgetId = s.WidgetId,
        Data = s.Data,
        SubmittedBy = s.SubmittedBy,
        IpAddress = s.IpAddress,
        SubmittedAt = s.SubmittedAt
    };
}
