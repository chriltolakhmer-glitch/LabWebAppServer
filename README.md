# LabWebAppServer

## Purpose

ASP.NET Core (.NET 10) Razor Pages application for Work-items and Operational Statuses. Login calls LabAuthServer and then LabAPIServer to verify identity and role before creating a server-side session. The browser receives a protected session cookie; API bearer tokens stay server-side. Web has no direct SQL access and does not own JWT signing or API authorization.

## Repository layout

- `src/LabWebAppServer.Web/`: Razor Pages, Auth/API clients and session store.
- `tests/LabWebAppServer.Web.Tests/`: isolated page, client and session tests.
- `docs/`: configuration examples, development rules, plans and operations records.

Git belongs at this source directory, as in LabAuthServer. Outer `Current`, `Releases`, `Backups` and IIS deployment contents are not repository source. Phase records document historical runs and environment names; they are not current defaults or acceptance of a new artifact.

## Build and local development

Install a .NET 10 SDK (preparation validated with 10.0.401) and trust the local HTTPS development certificate using `dotnet dev-certs https --trust`. From this repository root:

```powershell
dotnet restore LabWebAppServer.slnx
dotnet build LabWebAppServer.slnx -c Release --no-restore
dotnet test LabWebAppServer.slnx -c Release --no-build
Copy-Item src/LabWebAppServer.Web/appsettings.Development.example.json src/LabWebAppServer.Web/appsettings.Development.json
# Replace every placeholder in the copied, ignored file before starting.
dotnet run --project src/LabWebAppServer.Web --launch-profile https
```

Back up existing local settings before copying. The actual project profile uses HTTPS port 7153 (HTTP 5034). Root `Properties/launchSettings.json` and root `appsettings.json` are historical scaffolds; normal startup uses the files in `src/LabWebAppServer.Web/`.

## Configuration and authentication requirements

Use [development settings](src/LabWebAppServer.Web/appsettings.Development.example.json) or [external runtime settings](docs/configuration/labweb-runtime.example.json). Replace every host/URL placeholder; numeric timeouts are safe defaults. For the current local API profile use `https://localhost:7196`, overriding the older source fallback port. Configure a reachable Auth HTTPS endpoint and valid test accounts through the separate Auth environment; do not store accounts/passwords or tokens in settings, collections or Git. TLS validation must remain enabled.

`AuthClient:BaseUrl` and `ApiClient:BaseUrl` are fixed HTTPS destinations. Login requires both successful Auth authentication and API `/api/v1/session` verification. API owns authorization. Local cookies are Secure/HttpOnly and sessions are held in process; restart/recycle loses sessions and requires login again.

For runtime use, copy the runtime example outside the repository, restrict file permissions and set `LABWEB_CONFIG_PATH` to its absolute path. The existing loader adds external JSON **after** the standard providers, so it overrides duplicate environment/command-line values. Reload is disabled; restart after changes. Without external JSON, standard environment overrides such as `AuthClient__BaseUrl` apply normally.

Web needs no database connection string, SQL credentials, AD service credential, JWT trust export or Auth private key. SQL provisioning belongs to LabAPIServer. ASP.NET Core Data Protection protects Web cookies; provision protected persistence appropriate to the IIS identity using supported hosting configuration. The application currently has no custom `DataProtection` JSON binding, so do not assume a key-directory setting in a template would be honored.

## Deployment and release

Publish validated source with `dotnet publish src/LabWebAppServer.Web -c Release -o <EXTERNAL_PUBLISH_DIRECTORY>`. Local environment settings and example settings are excluded from publish output. Keep artifacts, IIS configuration and backups outside this source repository. Configure a dedicated IIS pool, .NET Hosting Bundle, HTTPS and normally trusted outbound Auth/API TLS. Deploy after compatible Auth/API/database provisioning. Retain the previous compatible artifact and runtime settings for rollback; expect users to log in again after recycle or rollback. See the [operations runbook](docs/operations/LabWebAppServer-Operations-Runbook.md).

API changes require Postman updates, including Web expectations for changed API contracts. Acceptance tests are required before release: login, all role outcomes, antiforgery, business journeys, cleanup and logout against the exact artifact. Follow [development and release rules](docs/Development-Rules.md). Existing external Postman collections have not been imported or rerun by repository preparation.

## CI and test instructions

The [build workflow](.github/workflows/build.yml) restores, builds and tests on pushes and pull requests targeting `main`, using Windows and .NET SDK 10.0.400. Branch names are not changed by this setup. Repositories still on `master` will not trigger this workflow until work targets `main`.

After the restore/build commands above, use this isolated local validation command:

```powershell
dotnet test LabWebAppServer.slnx -c Release --no-build --no-restore
```

Clear operational test configuration/opt-in variables in the test process first, as the workflow does. See [release process and CI limitations](docs/Release-Process.md) for the exact external-dependency exclusions and required acceptance. CI does not deploy or substitute for live acceptance.

## Architecture role

See [Architecture](docs/Architecture.md) for Browser -> Web -> Auth/AD and API/SQL responsibilities. Auth owns authentication, verified identity and role issuance; API owns business authorization and database access; Web owns UI, server-side sessions/cookies and API clients.

## API/Postman update requirement

New, modified or removed API endpoints require matching Postman updates before completion. Acceptance tests are required before release, covering success, authentication, role-specific authorization, validation/errors and verified cleanup of run-owned fixtures. Web expectations must follow changed API contracts. Keep credentials and tokens out of collections and repository files. Follow [Release Process](docs/Release-Process.md) for tagging, build verification, artifact checksums and rollback readiness.
