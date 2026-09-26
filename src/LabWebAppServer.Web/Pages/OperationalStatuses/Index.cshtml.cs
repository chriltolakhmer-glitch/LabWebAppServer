using System.Security.Claims;
using LabWebAppServer.Web.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabWebAppServer.Web.Pages.OperationalStatuses;

[Authorize]
public sealed class IndexModel(ILabApiClient apiClient) : PageModel
{
    public IReadOnlyList<OperationalStatusResponse> Statuses { get; private set; } = [];

    public string Role => User.FindFirst("role")?.Value ?? "Unknown";

    public bool CanManage => string.Equals(Role, "Operator", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);

    public string? ErrorMessage { get; private set; }

    public bool ListLoadFailed { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteApiAsync(token => apiClient.GetOperationalStatusesAsync(token, cancellationToken));
        if (result is not null)
        {
            Statuses = result;
        }
        else if (string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ErrorMessage = "Operational statuses could not be loaded.";
            ListLoadFailed = true;
        }

        return Page();
    }

    private async Task<T?> ExecuteApiAsync<T>(Func<string, Task<T?>> action)
    {
        var token = User.FindFirst("urn:lab:web:access-token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return default;
        }

        try
        {
            return await action(token);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return default;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ErrorMessage = "The API denied access to operational statuses.";
            return default;
        }
        catch (HttpRequestException exception) when (exception.StatusCode is System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout)
        {
            ErrorMessage = "The operational status service is temporarily unavailable. Try again shortly.";
            return default;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "The API is temporarily unavailable.";
            return default;
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = "The API returned an invalid response.";
            return default;
        }
    }
}
