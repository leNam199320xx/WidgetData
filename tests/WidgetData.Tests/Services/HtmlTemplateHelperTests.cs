using System.Text.Json;
using WidgetData.Application.Helpers;

namespace WidgetData.Tests.Services;

public class HtmlTemplateHelperTests
{
    // ─── Render – scalar substitution ────────────────────────────────────────

    [Fact]
    public void Render_EmptyTemplate_ReturnsEmpty()
    {
        var result = HtmlTemplateHelper.Render("", new[] { "name" },
            new[] { new Dictionary<string, string> { ["name"] = "Alice" } });

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_WhitespaceTemplate_ReturnsEmpty()
    {
        var result = HtmlTemplateHelper.Render("   ", new[] { "name" },
            new[] { new Dictionary<string, string> { ["name"] = "Alice" } });

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_ScalarTemplate_SubstitutesFirstRowValues()
    {
        var template = "<p>{{name}} - {{amount}}</p>";
        var columns = new[] { "name", "amount" };
        var rows = new[]
        {
            new Dictionary<string, string> { ["name"] = "Alice", ["amount"] = "100" },
            new Dictionary<string, string> { ["name"] = "Bob",   ["amount"] = "200" }
        };

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.Equal("<p>Alice - 100</p>", result);
    }

    [Fact]
    public void Render_ScalarTemplate_HtmlEncodesValues()
    {
        var template = "<p>{{name}}</p>";
        var columns = new[] { "name" };
        var rows = new[] { new Dictionary<string, string> { ["name"] = "<script>alert('xss')</script>" } };

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Render_ScalarTemplate_NoRows_LeavesPlaceholdersUnreplaced()
    {
        var template = "<p>{{name}}</p>";

        var result = HtmlTemplateHelper.Render(template, new[] { "name" },
            new List<Dictionary<string, string>>());

        Assert.Equal("<p>{{name}}</p>", result);
    }

    // ─── Render – #each iteration ────────────────────────────────────────────

    [Fact]
    public void Render_EachTemplate_IteratesAllRows()
    {
        var template = "{{#each rows}}<li>{{name}}</li>{{/each}}";
        var columns = new[] { "name" };
        var rows = new[]
        {
            new Dictionary<string, string> { ["name"] = "Alice" },
            new Dictionary<string, string> { ["name"] = "Bob" },
            new Dictionary<string, string> { ["name"] = "Carol" }
        };

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.Equal("<li>Alice</li><li>Bob</li><li>Carol</li>", result);
    }

    [Fact]
    public void Render_EachTemplate_EmptyRows_ProducesNoItems()
    {
        var template = "<ul>{{#each rows}}<li>{{name}}</li>{{/each}}</ul>";

        var result = HtmlTemplateHelper.Render(template, new[] { "name" },
            new List<Dictionary<string, string>>());

        Assert.Equal("<ul></ul>", result);
    }

    [Fact]
    public void Render_EachTemplate_ExceedsMaxRows_CapsAtMaxPreviewRows()
    {
        var template = "{{#each rows}}<li>{{n}}</li>{{/each}}";
        var columns = new[] { "n" };
        var rows = Enumerable.Range(1, HtmlTemplateHelper.MaxPreviewRows + 50)
            .Select(i => new Dictionary<string, string> { ["n"] = i.ToString() })
            .ToList();

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.Equal(HtmlTemplateHelper.MaxPreviewRows,
            result.Split("<li>", StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Render_EachTemplate_HtmlEncodesRowValues()
    {
        var template = "{{#each rows}}<td>{{val}}</td>{{/each}}";
        var columns = new[] { "val" };
        var rows = new[] { new Dictionary<string, string> { ["val"] = "<b>bold</b>" } };

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.DoesNotContain("<b>", result);
        Assert.Contains("&lt;b&gt;", result);
    }

    [Fact]
    public void Render_EachTemplate_PreservesTextOutsideBlock()
    {
        var template = "<h1>Header</h1>{{#each rows}}<p>{{x}}</p>{{/each}}<footer>Footer</footer>";
        var columns = new[] { "x" };
        var rows = new[] { new Dictionary<string, string> { ["x"] = "A" } };

        var result = HtmlTemplateHelper.Render(template, columns, rows);

        Assert.StartsWith("<h1>Header</h1>", result);
        Assert.EndsWith("<footer>Footer</footer>", result);
        Assert.Contains("<p>A</p>", result);
    }

    // ─── ToStringRows ─────────────────────────────────────────────────────────

    [Fact]
    public void ToStringRows_NullInput_ReturnsEmptyList()
    {
        var result = HtmlTemplateHelper.ToStringRows(null);

        Assert.Empty(result);
    }

    [Fact]
    public void ToStringRows_EmptyList_ReturnsEmptyList()
    {
        var result = HtmlTemplateHelper.ToStringRows(new List<Dictionary<string, object?>>());

        Assert.Empty(result);
    }

    [Fact]
    public void ToStringRows_StringValues_ConvertedCorrectly()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = (object?)"Alice", ["count"] = (object?)"42" }
        };

        var result = HtmlTemplateHelper.ToStringRows(rows);

        Assert.Single(result);
        Assert.Equal("Alice", result[0]["name"]);
        Assert.Equal("42", result[0]["count"]);
    }

    [Fact]
    public void ToStringRows_NullValues_ConvertedToEmptyString()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["val"] = null }
        };

        var result = HtmlTemplateHelper.ToStringRows(rows);

        Assert.Equal(string.Empty, result[0]["val"]);
    }

    [Fact]
    public void ToStringRows_JsonElementValues_ConvertedCorrectly()
    {
        var json = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            "{\"name\":\"Bob\",\"score\":99}");
        var rows = new List<Dictionary<string, object?>> { json! };

        var result = HtmlTemplateHelper.ToStringRows(rows);

        Assert.Equal("Bob", result[0]["name"]);
        Assert.Equal("99", result[0]["score"]);
    }

    [Fact]
    public void ToStringRows_MultipleRows_PreservesRowCount()
    {
        var rows = Enumerable.Range(1, 5)
            .Select(i => new Dictionary<string, object?> { ["id"] = (object?)i.ToString() })
            .ToList();

        var result = HtmlTemplateHelper.ToStringRows(rows);

        Assert.Equal(5, result.Count);
    }
}
