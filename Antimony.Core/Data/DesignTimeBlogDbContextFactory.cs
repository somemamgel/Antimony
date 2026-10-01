using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Antimony.Core.Data;

public class DesignTimeBlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder().SetBasePath(root)
            .AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables().Build();
        var connection = new SqliteConnectionStringBuilder(config.GetConnectionString("Blog") ?? "Data Source=App_Data/blog.db");
        if (!string.IsNullOrEmpty(connection.DataSource) && connection.DataSource != ":memory:")
            connection.DataSource = Path.GetFullPath(connection.DataSource, root);
        return new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection.ToString()).Options);
    }
}
