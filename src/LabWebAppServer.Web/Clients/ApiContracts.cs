using System.Text.Json.Serialization;

namespace LabWebAppServer.Web.Clients;

public sealed record ApiHealthResponse(
    [property: JsonPropertyName("status")] string Status);

public sealed record ApiSessionResponse(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt);

public sealed record WorkItemResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTimeOffset UpdatedAtUtc,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("updatedBy")] string UpdatedBy);

public sealed record OperationalStatusResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("observedAtUtc")] DateTimeOffset ObservedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTimeOffset UpdatedAtUtc,
    [property: JsonPropertyName("updatedBy")] string UpdatedBy,
    [property: JsonPropertyName("rowVersion")] string RowVersion);

public sealed record OperationalStatusHistoryResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("statusId")] Guid StatusId,
    [property: JsonPropertyName("previousStatus")] string PreviousStatus,
    [property: JsonPropertyName("newStatus")] string NewStatus,
    [property: JsonPropertyName("changedBy")] string ChangedBy,
    [property: JsonPropertyName("changedAtUtc")] DateTimeOffset ChangedAtUtc);
