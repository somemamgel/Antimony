using Antimony.Core.Data;
using Antimony.Core.Models;
using Antimony.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Antimony.Test.Integration.Services;

[Trait("Category", "Integration")]
public class TagServiceDatabaseTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DbContextOptions<BlogDbContext> options;
    private readonly BlogDbContext db;
    private readonly TagService service;

    public TagServiceDatabaseTests()
    {
        options = new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options;
        db = new BlogDbContext(options);
        service = new TagService(db);
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await db.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",，")]
    [InlineData(" , ， , ")]
    public async Task ResolveAsync_EmptyInput_DoesNotCreateTags(string? input)
    {
        Assert.Empty(await service.ResolveAsync(input));
        Assert.Empty(db.ChangeTracker.Entries<Tag>());
        Assert.Equal(0, await db.Tags.CountAsync());
    }

    [Theory]
    [InlineData(".NET")]
    [InlineData(".net")]
    [InlineData(" .Net ")]
    public async Task ResolveAsync_ExistingTag_ReusesPersistedTagWithoutChangingItsSpelling(string input)
    {
        var existing = new Tag { Name = ".NET", NormalizedName = ".NET" };
        db.Tags.Add(existing);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var tag = Assert.Single(await service.ResolveAsync(input));

        Assert.Equal(existing.Id, tag.Id);
        Assert.Equal(".NET", tag.Name);
        Assert.Equal(".NET", tag.NormalizedName);
        Assert.Equal(EntityState.Unchanged, db.Entry(tag).State);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(1, await db.Tags.CountAsync());
    }

    [Fact]
    public async Task ResolveAsync_NewDuplicateTags_TracksUniqueTagsWithoutSaving()
    {
        var tags = await service.ResolveAsync(" .NET, .net，编程， 编程 ");

        Assert.Equal(new[] { ".NET", "编程" }, tags.Select(tag => tag.Name));
        Assert.Equal(new[] { ".NET", "编程" }, tags.Select(tag => tag.NormalizedName));
        Assert.All(tags, tag => Assert.Equal(EntityState.Added, db.Entry(tag).State));
        Assert.Equal(2, db.ChangeTracker.Entries<Tag>().Count());

        await using var reader = new BlogDbContext(options);
        Assert.Equal(0, await reader.Tags.CountAsync());
    }

    [Fact]
    public async Task ResolveAsync_MixedTags_PreservesInputOrderAndReusesExistingTags()
    {
        var existing = new Tag { Name = ".NET", NormalizedName = ".NET" };
        db.Tags.Add(existing);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var tags = await service.ResolveAsync(" 读书, .net，ASP.NET Core,读书,.NET,asp.net core ");

        Assert.Equal(new[] { "读书", ".NET", "ASP.NET Core" }, tags.Select(tag => tag.Name));
        Assert.Equal(new[] { "读书", ".NET", "ASP.NET CORE" }, tags.Select(tag => tag.NormalizedName));
        Assert.Equal(existing.Id, tags[1].Id);
        Assert.Equal(EntityState.Unchanged, db.Entry(tags[1]).State);
        Assert.Equal(EntityState.Added, db.Entry(tags[0]).State);
        Assert.Equal(EntityState.Added, db.Entry(tags[2]).State);

        await db.SaveChangesAsync();
        await using var reader = new BlogDbContext(options);
        Assert.Equal(3, await reader.Tags.CountAsync());
        Assert.Equal(".NET", (await reader.Tags.SingleAsync(tag => tag.Id == existing.Id)).Name);
    }

    [Fact]
    public async Task ResolveAsync_AfterExplicitSave_PersistsTagsAndReusesThemOnLaterCalls()
    {
        var tags = await service.ResolveAsync(" ASP.NET Core,编程,asp.net core ");
        await db.SaveChangesAsync();

        await using var reader = new BlogDbContext(options);
        var saved = await reader.Tags.OrderBy(tag => tag.Id).ToListAsync();
        Assert.Equal(tags.Select(tag => tag.Id), saved.Select(tag => tag.Id));
        Assert.All(saved, tag => Assert.True(tag.Id > 0));
        Assert.Equal(new[] { "ASP.NET Core", "编程" }, saved.Select(tag => tag.Name));
        Assert.Equal(new[] { "ASP.NET CORE", "编程" }, saved.Select(tag => tag.NormalizedName));

        var resolved = await new TagService(reader).ResolveAsync("asp.net core，编程");
        Assert.Equal(saved.Select(tag => tag.Id), resolved.Select(tag => tag.Id));
        Assert.False(reader.ChangeTracker.HasChanges());
    }
}
