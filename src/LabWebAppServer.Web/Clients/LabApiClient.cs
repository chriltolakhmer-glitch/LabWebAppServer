using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LabWebAppServer.Web.Clients;

public sealed class LabApiClient(HttpClient httpClient) : ILabApiClient
{
    public async Task<ApiHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<ApiHealthResponse>("/api/v1/health", cancellationToken);

    public async Task<ApiSessionResponse?> GetSessionAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, "/api/v1/session", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiSessionResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItemResponse>?> GetWorkItemsAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, "/api/v1/work-items", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<WorkItemResponse>>(cancellationToken);
    }

    public async Task<WorkItemResponse?> GetWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, $"/api/v1/work-items/{id}", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkItemResponse>(cancellationToken);
    }

    public async Task<WorkItemResponse?> CreateWorkItemAsync(string bearerToken, string name, string? description, string status, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Post, "/api/v1/work-items", bearerToken);
        request.Content = JsonContent.Create(new { name, description, status });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkItemResponse>(cancellationToken);
    }

    public async Task<WorkItemResponse?> UpdateWorkItemAsync(string bearerToken, Guid id, string name, string? description, string status, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Put, $"/api/v1/work-items/{id}", bearerToken);
        request.Content = JsonContent.Create(new { name, description, status });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkItemResponse>(cancellationToken);
    }

    public async Task DeleteWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Delete, $"/api/v1/work-items/{id}", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<OperationalStatusResponse>?> GetOperationalStatusesAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, "/api/v1/operational-statuses", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalStatusResponse>>(cancellationToken);
    }

    public async Task<OperationalStatusResponse?> GetOperationalStatusAsync(string bearerToken, string key, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, $"/api/v1/operational-statuses/{Uri.EscapeDataString(key)}", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalStatusResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalStatusHistoryResponse>?> GetOperationalStatusHistoryAsync(string bearerToken, string key, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, $"/api/v1/operational-statuses/{Uri.EscapeDataString(key)}/history", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<OperationalStatusHistoryResponse>>(cancellationToken);
    }

    public async Task<OperationalStatusResponse?> UpdateOperationalStatusAsync(string bearerToken, string key, string status, string value, string severity, string rowVersion, CancellationToken cancellationToken = default)
    {
        using var request = CreateBearerRequest(HttpMethod.Put, $"/api/v1/operational-statuses/{Uri.EscapeDataString(key)}", bearerToken);
        request.Content = JsonContent.Create(new { status, value, severity, rowVersion });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperationalStatusResponse>(cancellationToken);
    }

    private static HttpRequestMessage CreateBearerRequest(HttpMethod method, string path, string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new ArgumentException("A bearer token is required for this API request.", nameof(bearerToken));
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return request;
    }
}
