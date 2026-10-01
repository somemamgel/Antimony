using Antimony.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Core.Data;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>().Property(p => p.Title).HasMaxLength(200);
        modelBuilder.Entity<Tag>().Property(t => t.Name).HasMaxLength(80);
        modelBuilder.Entity<Tag>().HasIndex(t => t.NormalizedName).IsUnique();
        modelBuilder.Entity<Post>().HasMany(p => p.Tags).WithMany(t => t.Posts);
    }
}
