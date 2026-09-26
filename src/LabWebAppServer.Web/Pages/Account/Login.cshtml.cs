using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LabWebAppServer.Web.Clients;

namespace LabWebAppServer.Web.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    IAuthClient authClient,
    ILabApiClient apiClient) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var login = await authClient.LoginAsync(Input.Username, Input.Password, cancellationToken);
            var session = await apiClient.GetSessionAsync(login.AccessToken, cancellationToken)
                ?? throw new InvalidOperationException("API returned an empty session response.");

            if (session.ExpiresAt <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(session.Subject) || string.IsNullOrWhiteSpace(session.Role))
            {
                throw new InvalidOperationException("API returned an invalid session response.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, session.Subject),
                new("role", session.Role),
                new("urn:lab:web:access-token", login.AccessToken),
                new("urn:lab:web:expires-at", session.ExpiresAt.ToUniversalTime().ToString("O"))
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var properties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = session.ExpiresAt,
                AllowRefresh = false
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
            Input.Password = string.Empty;
            return LocalRedirect(ReturnUrl ?? "/");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Sign-in is temporarily unavailable.");
            Input.Password = string.Empty;
            return Page();
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, "Sign-in could not be completed.");
            Input.Password = string.Empty;
            return Page();
        }
    }

    private string? NormalizeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}

public sealed class LoginInput
{
    [Required, StringLength(1024, MinimumLength = 1)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 1)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}
