using System.Text.Json.Serialization;

namespace LabWebAppServer.Web.Clients;

public sealed record AuthLoginResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("tokenType")] string TokenType,
    [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt);

public sealed record AuthLoginResult(string AccessToken, DateTimeOffset ExpiresAt);
