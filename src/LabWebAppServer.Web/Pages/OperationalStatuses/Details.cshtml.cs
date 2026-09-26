using System.ComponentModel.DataAnnotations;
using LabWebAppServer.Web.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabWebAppServer.Web.Pages.OperationalStatuses;

[Authorize]
public sealed class DetailsModel(ILabApiClient apiClient) : PageModel
{
    public OperationalStatusResponse? Status { get; private set; }

    public IReadOnlyList<OperationalStatusHistoryResponse> History { get; private set; } = [];

    public string Role => User.FindFirst("role")?.Value ?? "Unknown";

    public bool CanManage => string.Equals(Role, "Operator", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);

    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public OperationalStatusInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string key, CancellationToken cancellationToken)
    {
        await LoadAsync(key, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(string key, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(key, cancellationToken, preserveInput: true);
            return Page();
        }

        var token = User.FindFirst("urn:lab:web:access-token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/Login");
        }

        try
        {
            Status = await apiClient.UpdateOperationalStatusAsync(token, key, Input.Status, Input.Value, Input.Severity, Input.RowVersion, cancellationToken);
            return RedirectToPage(new { key });
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/Login");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ErrorMessage = "The API denied this update for the current role.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            ErrorMessage = "This status changed while you were editing it. Reload the latest value before saving.";
            await LoadAsync(key, cancellationToken);
        }
        catch (HttpRequestException exception) when ((int?)exception.StatusCode == 422)
        {
            ErrorMessage = "That status transition is not allowed by the operational lifecycle.";
            await LoadAsync(key, cancellationToken, preserveInput: true);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ErrorMessage = "The requested operational status was not found.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode is System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout)
        {
            ErrorMessage = "The operational status service is temporarily unavailable. Try again shortly.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "The API is temporarily unavailable.";
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = "The API returned an invalid response.";
        }

        return Page();
    }

    private async Task LoadAsync(string key, CancellationToken cancellationToken, bool preserveInput = false)
    {
        var token = User.FindFirst("urn:lab:web:access-token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        try
        {
            Status = await apiClient.GetOperationalStatusAsync(token, key, cancellationToken);
            if (Status is null)
            {
                ErrorMessage = "The requested operational status was not found.";
            }
            else if (!preserveInput)
            {
                Input = new OperationalStatusInput
                {
                    Status = Status.Status,
                    Value = Status.Value,
                    Severity = Status.Severity,
                    RowVersion = Status.RowVersion
                };
            }

            History = await apiClient.GetOperationalStatusHistoryAsync(token, key, cancellationToken) ?? [];
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ErrorMessage = "The requested operational status was not found.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "The operational status service is temporarily unavailable.";
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = "The API returned an invalid response.";
        }
    }
}

public sealed class OperationalStatusInput
{
    [Required, StringLength(32)]
    public string Status { get; set; } = "Open";

    [Required, StringLength(200, MinimumLength = 1)]
    public string Value { get; set; } = string.Empty;

    [Required, StringLength(16)]
    public string Severity { get; set; } = "Info";

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
