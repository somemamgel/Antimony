using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Antimony.Core.Services;

namespace Antimony.Test.Unit.Services;

[Trait("Category", "Unit")]
public class MarkdownServiceTests
{
    private readonly MarkdownService service = new();

    private IDocument RenderDocument(string markdown) =>
        new HtmlParser().ParseDocument(service.Render(markdown));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_EmptyMarkdown_ReturnsNoContent(string markdown)
    {
        using var document = RenderDocument(markdown);

        Assert.Empty(document.Body!.Children);
        Assert.True(string.IsNullOrWhiteSpace(document.Body.TextContent));
    }

    [Fact]
    public void Render_ArticleFormatting_PreservesHeadingsEmphasisAndLists()
    {
        using var document = RenderDocument("# 标题\n\n**重点**与*强调*\n\n- 第一项\n- 第二项");

        Assert.Equal("标题", document.QuerySelector("h1")?.TextContent);
        Assert.Equal("重点", document.QuerySelector("strong")?.TextContent);
        Assert.Equal("强调", document.QuerySelector("em")?.TextContent);
        Assert.Equal(new[] { "第一项", "第二项" },
            document.QuerySelectorAll("ul li").Select(item => item.TextContent));
    }

    [Fact]
    public void Render_Table_PreservesColumnHeadingsAndData()
    {
        using var document = RenderDocument("| 标签 | 数量 |\n| --- | --- |\n| 编程 | 2 |");

        Assert.Equal(new[] { "标签", "数量" },
            document.QuerySelectorAll("th").Select(cell => cell.TextContent));
        Assert.Equal(new[] { "编程", "2" },
            document.QuerySelectorAll("td").Select(cell => cell.TextContent));
    }

    [Theory]
    [InlineData("https://example.com/posts/1")]
    [InlineData("http://example.com")]
    [InlineData("/posts/1")]
    public void Render_NormalLinks_PreservesDestinationAndText(string url)
    {
        using var document = RenderDocument($"[文章]({url})");

        var link = Assert.IsAssignableFrom<IElement>(document.QuerySelector("a"));
        Assert.Equal(url, link.GetAttribute("href"));
        Assert.Equal("文章", link.TextContent);
    }

    [Theory]
    [InlineData("javascript:alert%281%29")]
    [InlineData("JaVaScRiPt:alert%281%29")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    public void Render_DangerousLinkProtocols_RemovesDestination(string url)
    {
        using var document = RenderDocument($"[文章]({url})");

        Assert.Empty(document.QuerySelectorAll("a[href]"));
        Assert.Contains("文章", document.Body!.TextContent);
    }

    [Fact]
    public void Render_NormalImage_PreservesSourceAndDescription()
    {
        using var document = RenderDocument("![示例图片](https://example.com/image.png)");

        var image = Assert.IsAssignableFrom<IElement>(document.QuerySelector("img"));
        Assert.Equal("https://example.com/image.png", image.GetAttribute("src"));
        Assert.Equal("示例图片", image.GetAttribute("alt"));
    }

    [Theory]
    [InlineData("javascript:alert%281%29")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    public void Render_DangerousImageProtocols_RemovesSource(string url)
    {
        using var document = RenderDocument($"![示例图片]({url})");

        Assert.Empty(document.QuerySelectorAll("img[src]"));
    }

    [Fact]
    public void Render_RawHtml_DoesNotCreateExecutableElements()
    {
        using var document = RenderDocument("<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>");

        Assert.Empty(document.QuerySelectorAll("script, img, [onerror]"));
    }

    [Fact]
    public void Render_CodeBlock_DisplaysHtmlAsText()
    {
        using var document = RenderDocument("```html\n<script>alert(1)</script>\n```");

        Assert.Empty(document.QuerySelectorAll("script"));
        Assert.Contains("<script>alert(1)</script>", document.QuerySelector("pre code")!.TextContent);
    }
}
