using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabWebAppServer.Web.Pages.Account;

[AllowAnonymous]
public sealed class AccessDeniedModel : PageModel
{
}
