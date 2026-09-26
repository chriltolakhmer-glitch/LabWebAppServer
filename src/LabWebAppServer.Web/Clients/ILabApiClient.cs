namespace LabWebAppServer.Web.Clients;

public interface ILabApiClient
{
    Task<ApiHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<ApiSessionResponse?> GetSessionAsync(string bearerToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkItemResponse>?> GetWorkItemsAsync(string bearerToken, CancellationToken cancellationToken = default);

    Task<WorkItemResponse?> GetWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default);

    Task<WorkItemResponse?> CreateWorkItemAsync(string bearerToken, string name, string? description, string status, CancellationToken cancellationToken = default);

    Task<WorkItemResponse?> UpdateWorkItemAsync(string bearerToken, Guid id, string name, string? description, string status, CancellationToken cancellationToken = default);

    Task DeleteWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalStatusResponse>?> GetOperationalStatusesAsync(string bearerToken, CancellationToken cancellationToken = default);

    Task<OperationalStatusResponse?> GetOperationalStatusAsync(string bearerToken, string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalStatusHistoryResponse>?> GetOperationalStatusHistoryAsync(string bearerToken, string key, CancellationToken cancellationToken = default);

    Task<OperationalStatusResponse?> UpdateOperationalStatusAsync(string bearerToken, string key, string status, string value, string severity, string rowVersion, CancellationToken cancellationToken = default);
}
