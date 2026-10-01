using Antimony.Core.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Tags;

public class IndexModel(BlogDbContext db) : PageModel
{
    public record TagItem(int Id, string Name, int Count);
    public List<TagItem> Tags { get; private set; } = [];
    public async Task OnGetAsync() => Tags = await db.Tags.AsNoTracking()
        .Where(t => t.Posts.Any(p => p.IsPublished)).OrderBy(t => t.Name)
        .Select(t => new TagItem(t.Id, t.Name, t.Posts.Count(p => p.IsPublished))).ToListAsync();
}
