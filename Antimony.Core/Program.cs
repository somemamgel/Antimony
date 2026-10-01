using Antimony.Core.Data;
using Antimony.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--hash-password"))
{
    AdminCredentials.GenerateHash();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.AccessDeniedPath = "/Admin/Login";
    options.Cookie.Name = "Antimony.Admin";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
var connection = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Blog") ?? "Data Source=App_Data/blog.db");
if (!string.IsNullOrEmpty(connection.DataSource) && connection.DataSource != ":memory:")
{
    connection.DataSource = Path.GetFullPath(connection.DataSource, builder.Environment.ContentRootPath);
    Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
}
builder.Services.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection.ToString()));
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddSingleton<AdminCredentials>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<BlogDbContext>().Database.MigrateAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();
