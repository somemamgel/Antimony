using Antimony.Core.Data;
using Antimony.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages;

public class IndexModel(BlogDbContext db) : PageModel
{
    public List<Post> Posts { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalPages { get; private set; }

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        var query = db.Posts.AsNoTracking().Where(p => p.IsPublished);
        TotalPages = (int)Math.Ceiling(await query.CountAsync() / 10.0);
        PageNumber = Math.Min(PageNumber, Math.Max(1, TotalPages));
        Posts = await query.Include(p => p.Tags).OrderByDescending(p => p.PublishedAt).ThenByDescending(p => p.Id)
            .Skip((PageNumber - 1) * 10).Take(10).ToListAsync();
    }
}
