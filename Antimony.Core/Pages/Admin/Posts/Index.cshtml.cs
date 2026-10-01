using Antimony.Core.Data;
using Antimony.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Admin.Posts;

public class IndexModel(BlogDbContext db) : PageModel
{
    public List<Post> Posts { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalPages { get; private set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        TotalPages = (int)Math.Ceiling(await db.Posts.CountAsync() / 20.0);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, TotalPages));
        Posts = await db.Posts.AsNoTracking().OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id)
            .Skip((PageNumber - 1) * 20).Take(20).ToListAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(int id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return NotFound();
        post.IsPublished = true;
        post.PublishedAt ??= DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Message = "文章已发布。";
        return RedirectToPage(new { PageNumber });
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return NotFound();
        post.IsPublished = false;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Message = "文章已撤回，保留为草稿。";
        return RedirectToPage(new { PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return NotFound();
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        Message = "文章已删除。";
        return RedirectToPage(new { PageNumber });
    }
}
