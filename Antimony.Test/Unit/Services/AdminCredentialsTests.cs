using Antimony.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Antimony.Test.Unit.Services;

[Trait("Category", "Unit")]
public class AdminCredentialsTests
{
    private const string Password = "test-password-for-unit-tests";

    private static AdminCredentials CreateCredentials(string? username, string? hash)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Admin:Username"] = username,
                ["Admin:PasswordHash"] = hash
            }).Build();
        return new AdminCredentials(configuration);
    }

    private static string HashPassword() => new PasswordHasher<string>().HashPassword("admin", Password);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("blog-owner", "blog-owner")]
    public void Username_ReturnsConfiguredValueOrEmptyDefault(string? username, string expected)
    {
        Assert.Equal(expected, CreateCredentials(username, null).Username);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("admin", null)]
    [InlineData(null, "hash")]
    [InlineData("   ", "hash")]
    [InlineData("admin", "   ")]
    public void Validate_IncompleteConfiguration_DisablesLogin(string? username, string? hash)
    {
        var credentials = CreateCredentials(username, hash);

        Assert.False(credentials.IsConfigured);
        Assert.False(credentials.Validate("admin", Password));
    }

    [Fact]
    public void Validate_ConfiguredCredentials_AcceptsCorrectPassword()
    {
        var credentials = CreateCredentials("admin", HashPassword());

        Assert.True(credentials.IsConfigured);
        Assert.True(credentials.Validate("admin", Password));
    }

    [Fact]
    public void Validate_CustomUsername_AcceptsHashProducedBySetupCommand()
    {
        var credentials = CreateCredentials("blog-owner", HashPassword());

        Assert.True(credentials.Validate("blog-owner", Password));
    }

    [Theory]
    [InlineData("unknown", Password)]
    [InlineData("ADMIN", Password)]
    [InlineData("admin", "wrong-password")]
    [InlineData("admin", "")]
    public void Validate_IncorrectCredentials_RejectsLogin(string username, string password)
    {
        var credentials = CreateCredentials("admin", HashPassword());

        Assert.False(credentials.Validate(username, password));
    }

    [Theory]
    [InlineData("not-a-base64-password-hash")]
    [InlineData("AA==")]
    [InlineData("AQ==")]
    [InlineData("Ag==")]
    public void Validate_MalformedHash_RejectsLoginWithoutThrowing(string hash)
    {
        var credentials = CreateCredentials("admin", hash);

        Assert.False(credentials.Validate("admin", Password));
    }
}
