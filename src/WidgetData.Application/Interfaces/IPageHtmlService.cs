using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

/// <summary>
/// Builds a complete or partial HTML page by combining a WidgetGroup (page) with
/// live widget data, ready to be served directly as a landing page, product page,
/// or any public-facing HTML page – without requiring the WidgetEngine JS client.
/// </summary>
public interface IPageHtmlService
{
    /// <summary>
    /// Renders all widgets belonging to <paramref name="pageId"/> (WidgetGroup) into HTML.
    /// </summary>
    Task<string> BuildAsync(int pageId, bool standalone = true, string? cssUrl = null);

    /// <summary>
    /// Renders a <see cref="PageDto"/> (Site Page) to HTML using the widget list
    /// already attached to the DTO.
    /// </summary>
    Task<string> BuildFromPageAsync(PageDto page, bool standalone = true, string? cssUrl = null);

    /// <summary>
    /// Renders each page to a standalone HTML file and returns a ZIP archive as bytes.
    /// Each file inside the ZIP is named <c>{slug}.html</c>.
    /// </summary>
    Task<byte[]> BuildMultiPageZipAsync(IList<PageDto> pages);

    /// <summary>
    /// Renders all pages into a single SPA <c>index.html</c> with hash-based navigation.
    /// </summary>
    Task<string> BuildSpaHtmlAsync(IList<PageDto> pages);
}
