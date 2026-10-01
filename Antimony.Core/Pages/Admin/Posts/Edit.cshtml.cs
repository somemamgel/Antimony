using System.ComponentModel.DataAnnotations;
using Antimony.Core.Data;
using Antimony.Core.Models;
using Antimony.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Admin.Posts;

public class EditModel(BlogDbContext db, TagService tags, MarkdownService markdown) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool IsPublished { get; private set; }
    public List<string> AvailableTags { get; private set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "请输入标题。"), StringLength(200, ErrorMessage = "标题最多 200 个字符。")]
        public string Title { get; set; } = "";
        [Required(ErrorMessage = "请输入 Markdown 正文。")]
        public string Markdown { get; set; } = "";
        public string? Tags { get; set; }
    }

    private async Task LoadTagsAsync() => AvailableTags = await db.Tags.AsNoTracking().OrderBy(t => t.Name).Select(t => t.Name).ToListAsync();

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is not null)
        {
            var post = await db.Posts.AsNoTracking().Include(p => p.Tags).SingleOrDefaultAsync(p => p.Id == Id);
            if (post is null) return NotFound();
            IsPublished = post.IsPublished;
            Input = new InputModel { Title = post.Title, Markdown = post.Markdown, Tags = string.Join(", ", post.Tags.OrderBy(t => t.Name).Select(t => t.Name)) };
        }
        await LoadTagsAsync();
        return Page();
    }

    public IActionResult OnPostPreview() => new JsonResult(new { html = markdown.Render(Input.Markdown ?? "") });

    public async Task<IActionResult> OnPostAsync(string action = "save")
    {
        if (action is not ("save" or "publish" or "withdraw")) return BadRequest();
        var post = Id is null ? new Post() : await db.Posts.Include(p => p.Tags).SingleOrDefaultAsync(p => p.Id == Id);
        if (post is null) return NotFound();
        IsPublished = post.IsPublished;
        if (TagService.Parse(Input.Tags).Any(t => t.Length > 80))
            ModelState.AddModelError("Input.Tags", "每个标签最多 80 个字符。");
        if (!ModelState.IsValid)
        {
            await LoadTagsAsync();
            return Page();
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        post.Title = Input.Title.Trim();
        post.Markdown = Input.Markdown;
        post.Tags.Clear();
        post.Tags.AddRange(await tags.ResolveAsync(Input.Tags));
        post.UpdatedAt = DateTime.UtcNow;
        if (action == "publish") { post.IsPublished = true; post.PublishedAt ??= DateTime.UtcNow; }
        if (action == "withdraw") post.IsPublished = false;
        if (Id is null) db.Posts.Add(post);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Message"] = post.IsPublished ? "文章已保存并公开。" : "草稿已保存。";
        return RedirectToPage("Index");
    }
}
