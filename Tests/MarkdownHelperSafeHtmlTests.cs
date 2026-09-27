using Compass.Helpers;
using Xunit;

namespace Compass.Tests;

public class MarkdownHelperSafeHtmlTests
{
    [Fact]
    public void ToSafeHtml_RendersBoldAndList()
    {
        var markdown = "Use **bold** text.\n\n- First item\n- Second item";

        var html = MarkdownHelper.ToSafeHtml(markdown);

        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>First item</li>", html);
        Assert.Contains("<li>Second item</li>", html);
        Assert.DoesNotContain("**bold**", html);
        Assert.DoesNotContain("- First item", html);
    }

    [Fact]
    public void ToSafeHtml_DoesNotEmitRawScriptOrHtmlTags()
    {
        var markdown = "Safe text <script>alert('xss')</script> and <img src=x onerror=alert(1)>";

        var html = MarkdownHelper.ToSafeHtml(markdown);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Safe text", html);
        // Raw HTML is treated as text (encoded), not executable markup.
        Assert.Contains("&lt;script&gt;", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToSafeHtml_StripsJavascriptLinks()
    {
        var markdown = "[Click](javascript:alert(1))";

        var html = MarkdownHelper.ToSafeHtml(markdown);

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Click", html);
    }

    [Fact]
    public void ToSafeHtml_EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MarkdownHelper.ToSafeHtml(null));
        Assert.Equal(string.Empty, MarkdownHelper.ToSafeHtml(""));
        Assert.Equal(string.Empty, MarkdownHelper.ToSafeHtml("   "));
    }

    [Fact]
    public void ToSafeHtml_PlainText_StillLooksCorrect()
    {
        var html = MarkdownHelper.ToSafeHtml("Plain guidance with no markup.");

        Assert.Contains("Plain guidance with no markup.", html);
        Assert.Contains("<p>", html);
    }
}
