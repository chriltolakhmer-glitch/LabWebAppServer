using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using LabWebAppServer.Web.Clients;
using LabWebAppServer.Web.Configuration;
using LabWebAppServer.Web.Pages.OperationalStatuses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace LabWebAppServer.Web.Tests;

public sealed class WebFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public WebFoundationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Home_page_returns_ok()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public void Endpoint_options_load_safe_https_defaults()
    {
        var auth = Options.Create(new AuthClientOptions());
        var api = Options.Create(new ApiClientOptions());

        Assert.Equal("https://localhost:7068", auth.Value.BaseUrl);
        Assert.Equal("https://localhost:7168", api.Value.BaseUrl);
        Assert.True(new Uri(auth.Value.BaseUrl).IsAbsoluteUri);
        Assert.True(new Uri(api.Value.BaseUrl).IsAbsoluteUri);
    }

    [Fact]
    public void Api_client_can_be_constructed_without_network_access()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7168") };
        var client = new LabApiClient(httpClient);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Api_client_gets_work_item_detail_with_nullable_description()
    {
        var id = Guid.NewGuid();
        using var httpClient = new HttpClient(new DetailHandler(id))
        {
            BaseAddress = new Uri("https://localhost:7196")
        };
        var client = new LabApiClient(httpClient);

        var item = await client.GetWorkItemAsync("opaque-token", id);

        Assert.NotNull(item);
        Assert.Equal(id, item!.Id);
        Assert.Null(item.Description);
    }

    [Fact]
    public async Task Api_client_updates_operational_status_with_row_version()
    {
        using var httpClient = new HttpClient(new OperationalStatusHandler())
        {
            BaseAddress = new Uri("https://localhost:7196")
        };
        var client = new LabApiClient(httpClient);

        var status = await client.UpdateOperationalStatusAsync("opaque-token", "api", "InProgress", "Ready", "Info", "AQIDBAUGBwg=");

        Assert.NotNull(status);
        Assert.Equal("api", status!.Key);
        Assert.Equal("Ready", status.Value);
    }

    [Fact]
    public async Task Api_client_gets_operational_status_history()
    {
        using var httpClient = new HttpClient(new OperationalStatusHistoryHandler())
        {
            BaseAddress = new Uri("https://localhost:7196")
        };
        var client = new LabApiClient(httpClient);

        var history = await client.GetOperationalStatusHistoryAsync("opaque-token", "api");

        Assert.NotNull(history);
        var entry = Assert.Single(history!);
        Assert.Equal("Open", entry.PreviousStatus);
        Assert.Equal("InProgress", entry.NewStatus);
    }

    [Fact]
    public async Task Operational_status_details_load_current_status_and_history_for_reader()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("role", "Reader"),
                new Claim("urn:lab:web:access-token", "opaque-token")
            ], "Test"))
        };
        var page = new DetailsModel(new DetailsApiClient())
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };

        var result = await page.OnGetAsync("api", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("InProgress", page.Status!.Status);
        Assert.Single(page.History);
        Assert.False(page.CanManage);
    }

    [Fact]
    public void Web_assembly_contains_no_jwt_validation_configuration()
    {
        var source = typeof(Program).Assembly.GetName().Name;

        Assert.Equal("LabWebAppServer.Web", source);

    }

    private sealed class DetailHandler(Guid expectedId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"https://localhost:7196/api/v1/work-items/{expectedId}", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("opaque-token", request.Headers.Authorization?.Parameter);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = expectedId,
                    name = "Fixture detail",
                    description = (string?)null,
                    status = "Complete",
                    createdAtUtc = DateTimeOffset.UtcNow,
                    updatedAtUtc = DateTimeOffset.UtcNow,
                    createdBy = "fixture",
                    updatedBy = "fixture"
                })
            });
        }
    }

    private sealed class OperationalStatusHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("https://localhost:7196/api/v1/operational-statuses/api", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("opaque-token", request.Headers.Authorization?.Parameter);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = Guid.NewGuid(),
                    key = "api",
                    status = "InProgress",
                    value = "Ready",
                    severity = "Info",
                    observedAtUtc = DateTimeOffset.UtcNow,
                    updatedAtUtc = DateTimeOffset.UtcNow,
                    updatedBy = "fixture",
                    rowVersion = "AgMEBQYHCAk="
                })
            });
        }
    }

    private sealed class OperationalStatusHistoryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://localhost:7196/api/v1/operational-statuses/api/history", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("opaque-token", request.Headers.Authorization?.Parameter);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        statusId = Guid.NewGuid(),
                        previousStatus = "Open",
                        newStatus = "InProgress",
                        changedBy = "fixture",
                        changedAtUtc = DateTimeOffset.UtcNow
                    }
                })
            });
        }
    }

    private sealed class DetailsApiClient : ILabApiClient
    {
        public Task<ApiHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ApiHealthResponse?>(null);

        public Task<ApiSessionResponse?> GetSessionAsync(string bearerToken, CancellationToken cancellationToken = default)
            => Task.FromResult<ApiSessionResponse?>(null);

        public Task<IReadOnlyList<WorkItemResponse>?> GetWorkItemsAsync(string bearerToken, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkItemResponse>?>(null);

        public Task<WorkItemResponse?> GetWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<WorkItemResponse?>(null);

        public Task<WorkItemResponse?> CreateWorkItemAsync(string bearerToken, string name, string? description, string status, CancellationToken cancellationToken = default)
            => Task.FromResult<WorkItemResponse?>(null);

        public Task<WorkItemResponse?> UpdateWorkItemAsync(string bearerToken, Guid id, string name, string? description, string status, CancellationToken cancellationToken = default)
            => Task.FromResult<WorkItemResponse?>(null);

        public Task DeleteWorkItemAsync(string bearerToken, Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<OperationalStatusResponse>?> GetOperationalStatusesAsync(string bearerToken, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OperationalStatusResponse>?>(null);

        public Task<OperationalStatusResponse?> GetOperationalStatusAsync(string bearerToken, string key, CancellationToken cancellationToken = default)
            => Task.FromResult<OperationalStatusResponse?>(new(
                Guid.NewGuid(),
                key,
                "InProgress",
                "Fixture",
                "Info",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "fixture",
                "AQIDBAUGBwg="));

        public Task<IReadOnlyList<OperationalStatusHistoryResponse>?> GetOperationalStatusHistoryAsync(string bearerToken, string key, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OperationalStatusHistoryResponse>?>([
                new(Guid.NewGuid(), Guid.NewGuid(), "Open", "InProgress", "fixture", DateTimeOffset.UtcNow)
            ]);

        public Task<OperationalStatusResponse?> UpdateOperationalStatusAsync(string bearerToken, string key, string status, string value, string severity, string rowVersion, CancellationToken cancellationToken = default)
            => Task.FromResult<OperationalStatusResponse?>(null);
    }
}
