using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Antimony.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Antimony.Core.Pages.Admin;

public class LoginModel(AdminCredentials credentials) : PageModel
{
    [BindProperty, Required(ErrorMessage = "请输入用户名。")]
    public string Username { get; set; } = "";
    [BindProperty, Required(ErrorMessage = "请输入密码。")]
    public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public bool IsConfigured => credentials.IsConfigured;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (!credentials.Validate(Username, Password))
        {
            ModelState.AddModelError("", "用户名或密码不正确，或管理员尚未配置。");
            return Page();
        }
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, credentials.Username)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Admin/Posts/Index")!);
    }
}
