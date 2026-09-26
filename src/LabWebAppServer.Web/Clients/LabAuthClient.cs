using System.Net.Http.Json;

namespace LabWebAppServer.Web.Clients;

public sealed class LabAuthClient(HttpClient httpClient) : IAuthClient
{
    public async Task<AuthLoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username, password },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthLoginResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Auth returned an empty login response.");

        if (!string.Equals(payload.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(payload.AccessToken) ||
            payload.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Auth returned an invalid login response.");
        }

        return new AuthLoginResult(payload.AccessToken, payload.ExpiresAt);
    }
}
