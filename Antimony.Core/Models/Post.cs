namespace Antimony.Core.Models;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Markdown { get; set; } = "";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public List<Tag> Tags { get; set; } = [];
}
