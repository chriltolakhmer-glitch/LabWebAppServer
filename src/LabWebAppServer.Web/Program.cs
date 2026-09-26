using LabWebAppServer.Web.Clients;
using LabWebAppServer.Web.Configuration;
using LabWebAppServer.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var externalConfigurationPath = builder.Configuration["LABWEB_CONFIG_PATH"];
if (!string.IsNullOrWhiteSpace(externalConfigurationPath))
{
    builder.Configuration.AddJsonFile(externalConfigurationPath, optional: false, reloadOnChange: false);
}

builder.Services.AddSingleton<ServerTicketStore>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-LabWebSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ServerTicketStore>((options, store) => options.SessionStore = store);

builder.Services
    .AddOptions<AuthClientOptions>()
    .Bind(builder.Configuration.GetSection(AuthClientOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AuthClientOptions>>(_ => new EndpointOptionsValidator<AuthClientOptions>(AuthClientOptions.SectionName));

builder.Services
    .AddOptions<ApiClientOptions>()
    .Bind(builder.Configuration.GetSection(ApiClientOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ApiClientOptions>>(_ => new EndpointOptionsValidator<ApiClientOptions>(ApiClientOptions.SectionName));

builder.Services.AddHttpClient<ILabApiClient, LabApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LabWebAppServer/Phase5");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = false
});

builder.Services.AddHttpClient<IAuthClient, LabAuthClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AuthClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = false
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
