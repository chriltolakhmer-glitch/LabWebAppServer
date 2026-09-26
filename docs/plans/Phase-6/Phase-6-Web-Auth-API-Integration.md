# Phase 6 - Web + Auth + API Integration

## Architecture

The implemented local flow is:

```text
Browser -> LabWebAppServer -> LabAuthServer -> opaque JWT
                                      |
                                      v
                         LabWebAppServer server ticket
                                      |
                                      v
                         LabAPIServer /api/v1/session
                                      |
                                      v
                         LabAPIServer /api/v1/work-items
```

LabAuthServer remains the authentication/JWT authority. LabAPIServer remains the JWT validation, authorization, business, and SQL boundary. LabWebAppServer owns the Razor Pages UI, Auth login interaction, and bounded in-memory server-side ticket store. Web does not validate JWT signatures, load signing keys, connect to SQL, call AD, or duplicate API authorization.

## Implemented flow

- `POST /Account/Login` sends the existing `{username,password}` contract to Auth.
- Auth's `accessToken`, `tokenType`, and `expiresAt` are checked in memory.
- The opaque token is immediately sent to API `GET /api/v1/session`.
- Only the API-verified subject, role, and expiry are used to create the Web cookie session.
- The token is stored in the server-side `ITicketStore`; the browser receives only the protected `__Host-LabWebSession` cookie handle.
- The authenticated dashboard sends the server-side token to API `GET /api/v1/work-items` and exposes create/delete operations through API calls. API authorization remains authoritative.
- `POST /Account/Logout` removes the cookie session.

## Runtime and TLS

The current Web launch profile uses `https://localhost:7153`; the current API test process used `https://localhost:7196`. Auth used the existing controlled endpoint `https://DC01.lab.local`.

The live test used default .NET `HttpClientHandler` certificate chain and hostname validation. No `DangerousAcceptAnyServerCertificateValidator`, `-k`, or certificate callback was used. The local .NET `CN=localhost` development certificate was trusted before testing. The Auth hostname resolved to the controlled lab host and used normal certificate validation.

## Live evidence

Tested on 2026-09-21 with existing account `test.itd@lab.local`. The password was entered through `Read-Host -AsSecureString`; it was not printed, stored, logged, or documented. The real JWT was retained only in memory by the Web process and was not printed or saved.

- Web login page antiforgery token: present.
- Auth login through Web: HTTP 200, observed in Web's sanitized HTTP client status logs.
- Web -> API `/api/v1/session`: HTTP 200, observed in Web's sanitized HTTP client status logs.
- Authenticated Web home: HTTP 200.
- Dashboard contained the verified subject `test.itd@lab.local` and role `Operator`.
- Web logout: HTTP 302.
- Request after logout: HTTP 302 to `/Account/Login`.
- Anonymous root before login: HTTP 302 to `/Account/Login`.

## Work-item and SQL limitation

The Web reached API `GET /api/v1/work-items` with the server-side JWT. The API returned HTTP 500 because no Database connection target is configured and the Phase 4 migration has not been executed against a safe development/test database. No SQL target was invented, no production data was used, and no migration was run. The corrected API data boundary is stored-procedure-only: `SqlWorkItemStore` calls the five procedures defined in `0001_WorkItems.sql`; the API contains no Work-item CRUD SQL.

Therefore the authenticated Web -> API session boundary is PASS, but Work-item data persistence and create/update/delete acceptance are **NOT RUN / BLOCKED by unavailable safe SQL test data**. No API change was made to conceal or bypass this limitation.

No authorized Reader account was tested. Reader write authorization is **NOT RUN**; no AD changes or account creation were performed.

## Tests

Web command from `C:\Apps\LabWebAppServer\Source\LabWebAppServer`:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore
```

Result: build passed; 4 tests passed, 0 failed, 0 skipped.

API command from `C:\Apps\LabAPIServer\Source\LabAPIServer`:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore
```

Result: build passed; 16 tests passed, 0 failed, 0 skipped.

Web automated tests cover anonymous protection, endpoint options, API-client construction, and foundation behavior. Real live evidence covers Auth login, API session verification, server-side Web session rendering, and logout.

## Security results

- Browser did not receive or manage the JWT.
- JWT was not placed in HTML, JavaScript, URL, localStorage, sessionStorage, or a browser cookie.
- Web contains no signing key, JWT signature validation, `JwtBearer`, `TokenValidationParameters`, SQL client, connection string, AD client, or password store.
- Auth/API clients use fixed configured HTTPS base URLs, disabled redirects, disabled cookie handling, and bounded timeouts.
- Login and logout use antiforgery-protected POST forms.
- Login return URLs are restricted to local URLs.
- API 401 signs out the local Web session; API 403 is shown as a safe operation-denied message.
- No raw password, JWT, cookie, Authorization header, private key, or credential-bearing connection string is recorded in source or documentation.

## Exact Web files changed

- `src/LabWebAppServer.Web/Authentication/ServerTicketStore.cs`
- `src/LabWebAppServer.Web/Clients/AuthContracts.cs`
- `src/LabWebAppServer.Web/Clients/IAuthClient.cs`
- `src/LabWebAppServer.Web/Clients/LabAuthClient.cs`
- `src/LabWebAppServer.Web/Clients/ILabApiClient.cs`
- `src/LabWebAppServer.Web/Clients/LabApiClient.cs`
- `src/LabWebAppServer.Web/Configuration/EndpointOptions.cs`
- `src/LabWebAppServer.Web/Pages/Account/Login.cshtml`
- `src/LabWebAppServer.Web/Pages/Account/Login.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Account/Logout.cshtml`
- `src/LabWebAppServer.Web/Pages/Account/Logout.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Account/AccessDenied.cshtml`
- `src/LabWebAppServer.Web/Pages/Account/AccessDenied.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Index.cshtml`
- `src/LabWebAppServer.Web/Pages/Index.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Shared/_Layout.cshtml`
- `src/LabWebAppServer.Web/Program.cs`
- `src/LabWebAppServer.Web/appsettings.json`
- `tests/LabWebAppServer.Web.Tests/UnitTest1.cs`
- `docs/plans/Phase-6/Phase-6-Web-Auth-API-Integration.md`

## Explicit NOT RUN items

- Safe SQL migration execution and deterministic Work-item test data.
- Real Work-item list/create/update/delete success through SQL.
- Reader account login and Reader write-denial live test.
- Expired Web session test against a real expired token.
- Web integration through LabWebAppServer's original planning port `7268`; the project launch profile uses `7153`.
- Phase 7 end-to-end validation, production SQL migration, deployment, packaging, release, and rollback testing.

## Definition of Done

Phase 6 core Web/Auth/API integration is complete with documented limitations: real Auth login, API session verification, server-side Web session, authenticated dashboard, and logout passed using default TLS validation. Work-item persistence remains pending a safe SQL target. Phase 7 was not started.

**PHASE 6 INTEGRATION COMPLETE WITH DOCUMENTED LIMITATIONS - READY FOR PHASE 7**
