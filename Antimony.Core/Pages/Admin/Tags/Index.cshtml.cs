using System.ComponentModel.DataAnnotations;
using Antimony.Core.Data;
using Antimony.Core.Models;
using Antimony.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Pages.Admin.Tags;

public class IndexModel(BlogDbContext db) : PageModel
{
    public record TagItem(int Id, string Name, int Count);
    public List<TagItem> Tags { get; private set; } = [];
    [BindProperty, Required(ErrorMessage = "请输入标签名称。"), StringLength(80, ErrorMessage = "标签最多 80 个字符。")]
    public string Name { get; set; } = "";
    [TempData] public string? Message { get; set; }
    public int? EditingId { get; private set; }

    public async Task OnGetAsync() => await LoadAsync();
    private async Task LoadAsync() => Tags = await db.Tags.AsNoTracking().OrderBy(t => t.Name)
        .Select(t => new TagItem(t.Id, t.Name, t.Posts.Count)).ToListAsync();

    public async Task<IActionResult> OnPostSaveAsync(int? id)
    {
        EditingId = id;
        var normalized = TagService.Normalize(Name ?? "");
        if (normalized.Length == 0) ModelState.AddModelError("Name", "请输入标签名称。");
        if ((Name ?? "").Contains(',') || (Name ?? "").Contains('，')) ModelState.AddModelError("Name", "标签名称不能包含逗号。");
        if (await db.Tags.AnyAsync(t => t.NormalizedName == normalized && t.Id != (id ?? 0)))
            ModelState.AddModelError("Name", "同名标签已经存在。");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        var tag = id is null ? new Tag() : await db.Tags.FindAsync(id.Value);
        if (tag is null) return NotFound();
        tag.Name = Name!.Trim();
        tag.NormalizedName = normalized;
        if (id is null) db.Tags.Add(tag);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Name", "标签保存失败，请检查是否存在同名标签。");
            await LoadAsync();
            return Page();
        }
        Message = id is null ? "标签已创建。" : "标签已重命名。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return NotFound();
        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        Message = "标签及关联已删除，文章已保留。";
        return RedirectToPage();
    }
}
