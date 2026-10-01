using Antimony.Core.Data;
using Antimony.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Tags;

public class DetailsModel(BlogDbContext db) : PageModel
{
    public Tag Tag { get; private set; } = null!;
    public List<Post> Posts { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var tag = await db.Tags.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id && t.Posts.Any(p => p.IsPublished));
        if (tag is null) return NotFound();
        Tag = tag;
        var query = db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Tags.Any(t => t.Id == id));
        TotalPages = (int)Math.Ceiling(await query.CountAsync() / 10.0);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));
        Posts = await query.OrderByDescending(p => p.PublishedAt).ThenByDescending(p => p.Id)
            .Skip((PageNumber - 1) * 10).Take(10).ToListAsync();
        return Page();
    }
}
