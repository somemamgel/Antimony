using Antimony.Core.Data;
using Antimony.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Services;

public class TagService(BlogDbContext db)
{
    public static string Normalize(string name) => name.Trim().ToUpperInvariant();

    public static string[] Parse(string? text) => (text ?? "").Split([',', '，'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .DistinctBy(Normalize).ToArray();

    public async Task<List<Tag>> ResolveAsync(string? text)
    {
        var result = new List<Tag>();
        foreach (var name in Parse(text))
        {
            var normalized = Normalize(name);
            var tag = await db.Tags.SingleOrDefaultAsync(t => t.NormalizedName == normalized);
            if (tag is null)
            {
                tag = new Tag { Name = name, NormalizedName = normalized };
                db.Tags.Add(tag);
            }
            result.Add(tag);
        }
        return result;
    }
}
