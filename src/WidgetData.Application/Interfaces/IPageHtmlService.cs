namespace WidgetData.Application.Interfaces;

/// <summary>
/// Builds a complete or partial HTML page by combining a WidgetGroup (page) with
/// live widget data, ready to be served directly as a landing page, product page,
/// or any public-facing HTML page – without requiring the WidgetEngine JS client.
/// </summary>
public interface IPageHtmlService
{
    /// <summary>
    /// Renders all widgets belonging to <paramref name="pageId"/> into HTML.
    /// </summary>
    /// <param name="pageId">ID of the WidgetGroup / page.</param>
    /// <param name="standalone">
    ///   <c>true</c>  – returns a full <c>&lt;!DOCTYPE html&gt;</c> document with embedded CSS. <br/>
    ///   <c>false</c> – returns only the inner grid fragment (useful for embedding in an existing page).
    /// </param>
    /// <param name="cssUrl">
    ///   Optional URL to an external widget-engine.css stylesheet.
    ///   When supplied, a <c>&lt;link&gt;</c> tag is used instead of the embedded CSS.
    /// </param>
    /// <returns>Rendered HTML string.</returns>
    Task<string> BuildAsync(int pageId, bool standalone = true, string? cssUrl = null);
}
