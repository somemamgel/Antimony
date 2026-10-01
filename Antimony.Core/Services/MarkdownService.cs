using Ganss.Xss;
using Markdig;

namespace Antimony.Core.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions().DisableHtml().Build();

    public string Render(string markdown) => new HtmlSanitizer().Sanitize(Markdown.ToHtml(markdown, pipeline));
}
