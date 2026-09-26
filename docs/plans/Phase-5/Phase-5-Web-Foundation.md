# Phase 5 - Web Foundation

## Scope

Phase 5 establishes the Razor Pages shell, safe Auth/API endpoint configuration, and a typed API-client boundary for later integration. It does not implement login, JWT validation, token storage, SQL, business rules, or Work-item pages.

## Architecture

The Web application remains a separate server-rendered Razor Pages process:

```text
Browser -> LabWebAppServer -> typed LabAPIServer client
```

Auth and API are represented as configured HTTPS destinations. The Web project owns no JWT keys, JWT validation parameters, SQL connection, AD client, or business authorization logic. The API remains authoritative for identity and permissions.

## Configuration

- `AuthClient:BaseUrl`: safe Development default `https://localhost:7068`.
- `ApiClient:BaseUrl`: safe Development default `https://localhost:7168`.
- Both options require absolute HTTPS URLs and are validated at startup.
- No passwords, JWTs, private keys, SQL connection strings, or AD credentials are configured.

## Implemented foundation

- Razor Pages layout with Home, health/status, and explicit unauthenticated placeholder state.
- Typed `ILabApiClient` boundary with health, session, and future Work-item response models.
- API client methods forward an opaque bearer value only when a later caller supplies one; the Web application does not create, parse, validate, or persist tokens.
- No outbound Auth/API call occurs during Phase 5 startup or home-page rendering.

## Tests and evidence

Required automated checks cover application startup, home-page HTTP 200, option validation, API-client construction, and the absence of JWT/SQL implementation in the Web project. The existing Web launch profile uses `https://localhost:7153` (not the planning default `7268`); the current source returned HTTP 200 for `/` and `/health` on that port.

Release build and test evidence on 2026-09-21:

- `dotnet restore`: PASS.
- `dotnet build -c Release`: PASS.
- `dotnet test -c Release --no-restore`: PASS, 4 passed, 0 failed, 0 skipped.
- Live current-source `https://localhost:7153/`: HTTP 200.
- Live current-source `https://localhost:7153/health`: HTTP 200.
- Narrow Web source security scan: PASS; no private keys, raw JWTs, bearer literals, JWT validation, SQL connection strings, or passwords found.

Files added or changed:

- `src/LabWebAppServer.Web/Configuration/EndpointOptions.cs`
- `src/LabWebAppServer.Web/Clients/ApiContracts.cs`
- `src/LabWebAppServer.Web/Clients/ILabApiClient.cs`
- `src/LabWebAppServer.Web/Clients/LabApiClient.cs`
- `src/LabWebAppServer.Web/Program.cs`
- `src/LabWebAppServer.Web/appsettings.json`
- `src/LabWebAppServer.Web/Pages/Index.cshtml`
- `src/LabWebAppServer.Web/Pages/Index.cshtml.cs`
- `tests/LabWebAppServer.Web.Tests/LabWebAppServer.Web.Tests.csproj`
- `tests/LabWebAppServer.Web.Tests/UnitTest1.cs`

## Explicit NOT RUN

- Real Auth login and password handling.
- Real JWT acquisition, storage, decoding, or signature validation.
- Web session cookies, ticket store, logout, antiforgery login flow, or expiry handling.
- Real Auth/API calls from the running Web application.
- Work-item UI and business workflow.
- SQL access, migration execution, deployment, packaging, and release.

## Definition of Done

Phase 5 is complete: the Web shell starts, renders home successfully, endpoint options validate, the typed API boundary is constructible, focused tests pass, no prohibited security/database implementation exists, and the local HTTPS startup result is recorded. Phase 6 remains responsible for real login and Auth/API session integration.

**PHASE 5 WEB FOUNDATION COMPLETE - READY FOR PHASE 6**
