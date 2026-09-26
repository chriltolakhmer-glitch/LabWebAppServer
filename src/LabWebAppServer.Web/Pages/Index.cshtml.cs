using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabWebAppServer.Web.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabWebAppServer.Web.Pages;

[Authorize]
public sealed class IndexModel(ILabApiClient apiClient) : PageModel
{
    public IReadOnlyList<WorkItemResponse> WorkItems { get; private set; } = [];

    public string Subject => User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    public string Role => User.FindFirst("role")?.Value ?? "Unknown";

    public string Expiry => User.FindFirst("urn:lab:web:expires-at")?.Value ?? "Unknown";

    public string? ErrorMessage { get; private set; }

    public bool CanManageWorkItems => string.Equals(Role, "Operator", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);

    public bool ListLoadFailed { get; private set; }

    [BindProperty]
    public WorkItemInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await ExecuteApiAsync(token => apiClient.CreateWorkItemAsync(token, Input.Name, Input.Description, Input.Status, cancellationToken));
        if (result is null)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await ExecuteApiAsync(token => apiClient.UpdateWorkItemAsync(token, id, Input.Name, Input.Description, Input.Status, cancellationToken));
        if (result is null)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await ExecuteApiAsync(async token =>
        {
            await apiClient.DeleteWorkItemAsync(token, id, cancellationToken);
            return (object?)new object();
        });
        if (result is null)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task<IActionResult> LoadAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteApiAsync(token => apiClient.GetWorkItemsAsync(token, cancellationToken));
        if (result is not null)
        {
            WorkItems = result;
        }
        else if (string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ErrorMessage = "Work-items could not be loaded.";
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
            ErrorMessage = "The API denied this operation for the current role.";
            return default;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            ErrorMessage = "The submitted work-item data was not accepted.";
            ModelState.AddModelError(string.Empty, ErrorMessage);
            return default;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ErrorMessage = "That work-item could not be found. It may have been removed.";
            return default;
        }
        catch (HttpRequestException exception) when (exception.StatusCode is System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout)
        {
            ErrorMessage = "The work-item service is temporarily unavailable. Try again shortly.";
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

public sealed class WorkItemInput
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Open";
}
