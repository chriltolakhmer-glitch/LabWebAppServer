using System.Security.Claims;
using LabWebAppServer.Web.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabWebAppServer.Web.Pages.WorkItems;

[Authorize]
public sealed class DetailsModel(ILabApiClient apiClient) : PageModel
{
    public WorkItemResponse? Item { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var token = User.FindFirst("urn:lab:web:access-token")?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/Login");
        }

        try
        {
            Item = await apiClient.GetWorkItemAsync(token, id, cancellationToken);
            if (Item is null)
            {
                ErrorMessage = "The requested Work-item was not found.";
            }
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/Login");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ErrorMessage = "The API denied access to this Work-item.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ErrorMessage = "The requested Work-item was not found.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode is System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout)
        {
            ErrorMessage = "The work-item service is temporarily unavailable. Try again shortly.";
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
}
