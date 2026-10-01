using Microsoft.AspNetCore.Identity;

namespace Antimony.Core.Services;

public class AdminCredentials(IConfiguration configuration)
{
    public string Username => configuration["Admin:Username"] ?? "";
    private string PasswordHash => configuration["Admin:PasswordHash"] ?? "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(PasswordHash);

    public bool Validate(string username, string password)
    {
        if (!IsConfigured || username != Username) return false;
        try
        {
            return new PasswordHasher<string>().VerifyHashedPassword(Username, PasswordHash, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }

    public static void GenerateHash()
    {
        Console.Write("请输入管理员密码（输入不回显）：");
        var password = new System.Text.StringBuilder();
        if (Console.IsInputRedirected)
            password.Append(Console.ReadLine());
        else
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
                else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
            }
        Console.WriteLine();
        if (password.Length == 0) throw new ArgumentException("密码不能为空。");
        Console.WriteLine(new PasswordHasher<string>().HashPassword("admin", password.ToString()));
    }
}
