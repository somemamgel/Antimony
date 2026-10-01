using Antimony.Core.Data;
using Antimony.Core.Models;
using Antimony.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Posts;

public class DetailsModel(BlogDbContext db, MarkdownService markdown) : PageModel
{
    public Post Post { get; private set; } = null!;
    public string Html { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var post = await db.Posts.AsNoTracking().Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Id == id && p.IsPublished);
        if (post is null) return NotFound();
        Post = post;
        Html = markdown.Render(post.Markdown);
        return Page();
    }
}
