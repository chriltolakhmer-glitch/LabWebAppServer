namespace LabWebAppServer.Web.Clients;

public interface IAuthClient
{
    Task<AuthLoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
