using Microsoft.Extensions.Options;

namespace LabWebAppServer.Web.Configuration;

public sealed class AuthClientOptions
{
    public const string SectionName = "AuthClient";
    public string BaseUrl { get; set; } = "https://localhost:7068";
    public int TimeoutSeconds { get; set; } = 40;
}

public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";
    public string BaseUrl { get; set; } = "https://localhost:7168";
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class EndpointOptionsValidator<TOptions>(string sectionName) : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var baseUrl = options switch
        {
            AuthClientOptions auth => auth.BaseUrl,
            ApiClientOptions api => api.BaseUrl,
            _ => string.Empty
        };

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail($"{sectionName}:BaseUrl must be an absolute HTTPS URL.");
        }

        var timeout = options switch
        {
            AuthClientOptions auth => auth.TimeoutSeconds,
            ApiClientOptions api => api.TimeoutSeconds,
            _ => 0
        };

        if (timeout is < 1 or > 60)
        {
            return ValidateOptionsResult.Fail($"{sectionName}:TimeoutSeconds must be between 1 and 60.");
        }

        return ValidateOptionsResult.Success;
    }
}
