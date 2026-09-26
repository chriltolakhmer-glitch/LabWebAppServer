# LabWebAppServer development plan

Date: 2026-09-21. Status: approved planning architecture; Phase 0 decisions pending; implementation NOT RUN. Operational root: `C:\Apps\LabWebAppServer`, sibling of LabAuthServer and LabAPIServer. Repository location: `C:\Apps\LabWebAppServer\Source\LabWebAppServer`. No Git initialization or application implementation exists yet.

Read [shared architecture, contracts, decisions and delivery](../../../../../LabAPIServer/Source/LabAPIServer/docs/plans/Shared-System-Architecture-and-Delivery-Plan.md) and the [API plan](../../../../../LabAPIServer/Source/LabAPIServer/docs/plans/LabAPIServer-Development-Plan.md). API-only JWT trust, exact Auth/session wire contracts, D1-D8 decisions, 11-phase roadmap, compatibility and the smoke matrix are owned by the shared document.

## 1. Technology choice

Current decisions and Phase 1 blockers: [Phase 0 record](../../../../../LabAPIServer/Source/LabAPIServer/docs/plans/Phase-0/Phase-0-Architecture-and-Decisions.md). Development URL defaults are retained; no business workflow or deployment host has been invented.

The specified requirements are login, authorized screens, API calls and user-facing errors. No offline mode, rich client state, realtime interaction, or complex component requirement is supplied. Recommend ASP.NET Core net10.0 Razor Pages with modest local CSS/JavaScript and server-side HTTP clients.

| Option | Fit and cost for known requirements | Decision |
| --- | --- | --- |
| Razor Pages | Page-oriented UI, familiar .NET tooling, server HTTP/session boundary; one build/deployment | Recommended default, subject to D1/D4 |
| MVC | Same hosting/security fit, extra controller/view organization useful for more complex navigation | Valid alternative if actual screen complexity warrants it |
| Blazor Server | Interactive components with connection/circuit state and operational implications | No interaction requirement yet justifies it |
| SPA/React or Blazor WebAssembly | Separate browser runtime/state/API integration; token handling or an additional backend boundary still required | Defer until rich/offline client requirements exist |

The choice is based on the current scope, not a requirement to copy Auth's stack. Razor Pages supplies a page model and routing suitable for this proposed UI ([framework reference](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/?view=aspnetcore-10.0)). Revisit only if D1/D4 changes the interaction model.

## 2. Project structure and responsibilities

One executable and one test project. No database package/folder, ORM, Identity user store, LDAP client, JWT validation/signing service, or shared source reference to either sibling. Outer folders now are only Source, Releases and Scripts; documentation lives under Source/project/docs. Releases and Scripts are empty; Git initialization and application scaffolding wait for Phase 1. Minimal future repository structure:

```text
LabWebAppServer/Source/LabWebAppServer/
  AGENTS.md, README.md, global.json, .gitignore
  LabWebAppServer.slnx
  src/LabWebAppServer.Web/
    Program.cs
    Pages/
      Account/Login, Logout, AccessDenied
      Shared/
      <agreed-business-pages>/
    Clients/                        # Auth client, API client, wire DTOs
    Authentication/                 # API-verified identity, local ticket store
    Configuration/
    wwwroot/                        # local static assets
  tests/LabWebAppServer.Tests/       # page/session/HTTP/browser tests
  docs/plans/
```

The Pages entries describe future `.cshtml`/PageModel pairs. Create business pages only from agreed D1 workflows. Thin page handlers validate user input, invoke clients, map outcomes and render. They do not duplicate API business rules or grant resource permissions. Browser-required hints such as required fields can mirror API constraints for usability; the API remains authoritative.

Initial proposed routes: anonymous `/Account/Login`, `/Account/AccessDenied`, error page and `/health`; protected home and scoped business pages; POST `/Account/Logout`. Public static assets contain no private data. Web health returns a small liveness response, not Auth/API probes. Anonymous root can redirect to Login. Do not build a generic forwarding proxy that accepts arbitrary destinations/routes.

## 3. Login and server session

Consume Auth `POST /api/v1/auth/login` with `{username,password}` and success `{accessToken,tokenType,expiresAt}`, then API `GET /api/v1/session` with that opaque Bearer token. There is no browser redirect to an Auth authorization endpoint. Never call AD or validate the password independently. Shared sections 3-4 define the complete flow and API-owned token validation rules.

Use a normal antiforgery-protected form on HTTPS. Validate username/password limits and downstream serialized JSON body budget before sending. Password is not trimmed or redisplayed, stored in ModelState responses, session, TempData, URL, telemetry, audit or logs. Do not retain credentials for renewal. A submitted end-user password is request data, not a configured AD credential.

The Auth HttpClient has a fixed allowlisted HTTPS base address, no cookie container, redirects disabled and no automatic retries. Proposed timeout 40 seconds accommodates the reference's 30-second default decision budget plus response overhead, but cannot guarantee native directory cancellation; confirm against effective Auth settings. Bound response reading (proposed 64 KiB) and verify successful JSON/type/expiry. Caller cancellation propagates; show no successful session after an incomplete response.

Treat the returned JWT as opaque, bounded server-side data. Do not parse claims, validate signatures, load JWT public keys or configure a JwtTrust section in Web. Never accept a token supplied in a browser form/query as an alternate login path. Keep the pending token in request-local memory while calling API `GET /api/v1/session`; no authenticated ticket or cookie exists yet.

That API call is mandatory for login. A successful response is `{subject, role, expiresAt}`, all derived by API from its validated JWT. Over the fixed, normally validated HTTPS connection, require a well-formed nonempty identity, supported role and future UTC expiry in the response. Use that response as the sole identity and absolute-expiry source. Auth's expiresAt is only response metadata, not authority to create the local session. API rejection, timeout/unavailability, malformed/oversized response or elapsed expiry means no session: discard the pending token and show a safe sign-in failure. Do not fall back to decoded claims or Auth metadata. API still validates the token independently on every subsequent protected call.

Use ASP.NET Core cookie authentication with an `ITicketStore` backed by a bounded, concurrent in-process cache. Ticket contains verified subject/role and token server-side. Browser cookie carries only a cryptographically protected random session handle. The framework provides the ticket-store integration ([cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [ITicketStore](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.iticketstore?view=aspnetcore-10.0)); the limits and lifecycle below are project decisions.

Cookie proposal: `__Host-LabWebSession`, Secure, HttpOnly, SameSite=Lax, Path=/, no Domain, no persistent remember-me. Rotate the handle on successful login and invalidate the prior session. Store token/ticket with absolute expiry at the API session response's `expiresAt` (API derives it from signed exp); never extend beyond it. Disable framework sliding cookie lifetime. Proposed 20-minute server-side idle expiry, capped by absolute expiry, with per-request activity updates; test with a controlled clock. A missing/evicted/expired ticket is anonymous and clears the cookie. Do not depend on browser cookie expiration as the only check.

Proposed cache limit: 1000 sessions with one bounded ticket per entry and a maximum accepted JWT of 12288 bytes; confirm D6 capacity. Expired sessions are removed; eviction results in safe re-login. Do not place JWT in localStorage, sessionStorage, page markup, JavaScript, hidden fields, URLs or browser cookies. Token and cookie logs are disabled. Data Protection keys are Web's own cookie-protection material, not Auth signing keys; keep them external, ACL restricted and distinct per app/environment.

Single IIS worker is the starting deployment assumption. Restart, recycle and deployment lose in-memory tickets and force login. Overlapping recycling can also invalidate sessions during transition; document this behavior and use a scoped stop/start for planned cutovers. Multiple workers/nodes or surviving restarts requires D4 to choose shared server storage; do not introduce SQL access into Web to solve it. No distributed cache is planned without that need.

Logout is POST with antiforgery: remove ticket, sign out cookie, return to a safe public page. A replayed removed cookie must fail. Check expiration before API calls; API 401 also removes the ticket and prompts login. Do not extend/refresh tokens, silently retry credentials or invent an Auth logout route. Already-running requests may finish; cookie logout does not revoke an issued JWT at the API. Role changes/account disable are not visible until a new token or expiry under current Auth capabilities; D4 must accept this or request separately scoped Auth work.

## 4. API communication, UI and errors

Typed HttpClient per external service, fixed trusted destinations, server-side only. Each outgoing API HttpRequestMessage receives the current user's Bearer token; never share mutable DefaultRequestHeaders or per-user state in a singleton handler. Disable automatic redirects and cookie handling. Pass cancellation and safe correlation IDs. Proposed API timeout 30 seconds, explicit bounded response size (initially 1 MiB for paged DTOs, adjusted for the agreed contract). Avoid retries in v1; never automatically replay business writes. A timeout may have occurred after a write committed, so tell the user to check the result before resubmitting.

Keep small wire DTOs matching actual API JSON in Clients; contract tests detect drift. No shared binary DTO package or generated SDK needed initially. Map data to page models only where presentation differs. API supplies data and authoritative authorization; Web may hide disallowed navigation/actions from the verified role matrix for usability. API 403 still wins, including when a user crafts a direct form request.

| Failure | User-facing handling |
| --- | --- |
| Login 400 / local validation | Safe field message; never repeat submitted password |
| Login 401 | Generic credentials-not-accepted message, no account enumeration |
| Login 429 | Ask user to wait; honor a valid Retry-After if present; do not assume the Auth response includes one |
| Login 413 / invalid Auth JSON / API session rejection or malformed response | Safe sign-in failure; no session; sanitized contract/size diagnostic |
| Login 500 / 503 / 504 / timeout | Sign-in temporarily unavailable with correlation; distinguish from bad credentials |
| API 401 | Remove ticket and require login, preserve only a validated local GET return URL |
| API 403 | Access denied page; retain an existing session; a rejection during initial session verification creates no session; no login loop |
| API 400 / 404 / 409 | Render safe validation, not-found or conflict message according to implemented endpoint contract |
| API unavailable / malformed or oversized response | Safe temporary-error page and correlation; no raw upstream body/stack trace |

Protected responses use no-store, including errors and redirects as appropriate. Encode API/user text by default; no raw HTML from DTOs. Validate return URLs as local and allow only known safe destinations; never automatically replay an earlier POST after login. Keep layout keyboard accessible, labels and validation associated, and error states understandable. Do not introduce product details about certificates, JWT claims or SQL internals into normal user flows.

## 5. Practical Web security and configuration

Require antiforgery on login, logout and every state-changing form; GET requests must not mutate data. Keep framework output encoding; prohibit arbitrary redirect and upstream URLs. Set nosniff, frame denial via CSP `frame-ancestors 'none'`, a restrictive self-hosted asset CSP, and a privacy-preserving Referrer-Policy. Start with local scripts/styles to avoid third-party runtime dependencies; adjust CSP deliberately for actual assets without blanket unsafe allowances. Production HTTPS/HSTS and host/proxy trust follow the shared deployment plan.

Proposed typed settings:

| Setting | Requirement |
| --- | --- |
| `AuthClient:BaseUrl`, `TimeoutSeconds` | Required absolute HTTPS allowlisted URL; proposed 40-second timeout |
| `ApiClient:BaseUrl`, `TimeoutSeconds` | Required absolute HTTPS allowlisted URL; proposed 30-second timeout |
| `Session:IdleMinutes`, `MaximumEntries` | Proposed 20 and 1000; positive bounded values; absolute ceiling comes only from API-verified expiresAt |
| `DataProtection:KeyDirectory`, `ApplicationName` | Explicit external protected directory and stable app/environment discriminator |
| `AllowedHosts`, `Logging` | Exact production hosts; safe levels and verified diagnostic sink |

Implement safe source defaults, development file/user secrets in Development only, required external production JSON selected by `LABWEB_CONFIG_PATH`, then process environment overrides last. Restrict/disable arbitrary production command-line overrides and document the actual order. Validate options at startup; missing destination configuration or Data Protection directory access fails safely without secret output. Outgoing HTTPS certificate errors fail the affected request; startup need not probe upstreams. Restart for changes. Source/published files contain no operational credentials or trust exports. Production settings are deployment-managed, runtime-read-only, and absent from the immutable ZIP.

There is no SQL connection setting, AD service credential, JWT trust configuration or Auth public/private signing key in Web. End-user passwords exist only during a login submission. HTTPS trust for outgoing Auth/API clients is required and distinct from JWT signing-key trust, which belongs only to API. Web's cookie proves a local session only; API does not accept that cookie or claims supplied by Web headers.

## 6. Tests and local development

Use a real Web test host with controlled Auth/API handlers, opaque token placeholders, verified-session DTO fixtures, deterministic time, and an isolated ticket store/Data Protection setup. JWT cryptographic tests belong to API; Web tests prove that session creation depends on the API response and that the exact opaque token is forwarded safely. Deny accidental operational HTTP calls by default. Unit tests cover small client/error/session rules; host tests cover actual page/authentication/antiforgery behavior. Add a small browser smoke suite using Playwright if needed for cookie/CSRF/rendering behavior; keep it to the real user journey.

| Area | Required evidence when implemented |
| --- | --- |
| Startup | Valid configuration starts; missing/unsafe upstream settings fail; `/health` is anonymous and independent of upstream availability |
| Login | Form GET, antiforgery failure, exact Auth JSON exchange then mandatory API session call; no cookie before verification; failed credentials/429, invalid/malformed session response or Auth/API outage creates no session; no password retention |
| Session verification | Verified subject/role/expiry come only from API DTO; Web never decodes the opaque token; past/missing expiry fails; valid Auth metadata cannot bypass a failed API verification; API 401/403 removes pending token without a cookie |
| Session | Cookie flags and handle-only content; tampering/replay denied; handle rotation; bounded store eviction; idle/absolute expiry; recycle re-login |
| API call | Correct per-user token sent only to configured API, no redirect forwarding or cross-user leakage; real DTO renders correctly |
| Authorization | Protected-page challenge; allowed UI state and denied API 403; crafted action still governed by API |
| Logout/errors | POST logout removes store entry; GET cannot log out; 401 clears session, 403 preserves it; timeout and non-JSON/oversize upstream response handled safely |
| Browser | HTTPS cookie behavior, HTML encoding, safe return URL, antiforgery, no JWT in DOM/storage/browser response |
| System | Real Web -> Auth -> JWT -> API -> SQL -> rendered agreed business data, plus correlated audit/logout/expiry |

Future commands after scaffolding, from `C:\Apps\LabWebAppServer\Source\LabWebAppServer`:

```powershell
dotnet restore LabWebAppServer.slnx
dotnet build LabWebAppServer.slnx -c Release --no-restore
dotnet test LabWebAppServer.slnx -c Release --no-build --filter "Category!=SystemAcceptance"
dotnet run --project src/LabWebAppServer.Web --launch-profile https
```

Proposed local URL `https://localhost:7268`; API `https://localhost:7168`; test Auth `https://localhost:7068` or explicitly authorized test service. Configure trusted HTTPS later; never bypass validation. Shared section 6 defines startup order and Auth prerequisites. Real-system tests require explicit opt-in plus named endpoints and approved test accounts supplied through secure interactive/test-secret handling. No stored real passwords/tokens, no implicit production target, and no directory membership mutations as routine test setup.

## 7. Delivery, operations and acceptance

Use the shared 11-phase roadmap: Phase 0 resolves the required decisions; Phase 1 creates both buildable skeletons. Phases 2-4 build API/database foundation, JWT/session integration and one actual API feature. Phase 5 adds the Razor Pages layout, clients, login/logout and server session to the existing skeleton; it does not scaffold a second Web project. Phase 6 connects the real browser feature; Phase 7 validates successful/failed journeys; Phase 8 packages; Phase 9 deploys; Phase 10 establishes operations. Each phase has prerequisites, work, tests, deliverables, acceptance, DoD and explicit NOT RUN items. Phase 5 client fixtures can be prepared independently of Phase 4, but real Phase 6 acceptance needs both.

Publish one immutable Web artifact from the tested source with safe static assets and no database content, local/production settings, tokens or Data Protection keys. Keep ZIP, SHA256 and release record in outer `Releases\<version>`; extracted `app` and previous versions stay under Releases. Outer Scripts holds versioned deployed operational script copies; their source belongs inside the repository. No outer Backups/Deployments or database folder. Record Web/API contract compatibility against exact tested packages. Deploy after database, compatible Auth confirmation, and API. Use its own IIS application/pool/identity, trusted HTTPS host and outbound Auth/API HTTPS trust; no SQL or AD credentials. Verify TLS and API session behavior under the actual Web identity, then the remote-browser journey.

Rollback switches to the previous Web artifact and matching settings/API-compatible contract; volatile sessions intentionally disappear. Preserve external Data Protection storage securely, but do not back up raw user tokens/tickets; recovery means users log in again. Avoid reverting to a Web version unsupported by the currently active API. First-install rollback disables only the new Web site/pool. No Auth/SQL rollback is implied by a Web deployment failure.

The later runbook records actual `/health`, scoped pool restart, configuration/HTTPS trust/Data Protection key paths, observed Windows/IIS logs, external backup/checksum set, rollback and session-loss behavior. Web audit evidence is its safe session/request diagnostics plus links to Auth/API correlations; no database is added for Web logging. Weekly health/backup checks and monthly TLS/runtime/access reviews follow the shared maintenance baseline. Mark log capture or host-loss recovery unverified until exercised.

Web implementation is done when the agreed page works through the real API with allowed and denied users, login/session/logout/expiry are correct, no JWT/password leaks into browser persistence or logs, required tests pass, and the compatible immutable package and applicable operational evidence exist. A mock page and a green health check do not complete the business journey. Shared D1-D8 separates required Phase 0 decisions from later prerequisites; JWT audience/public-key acceptance is API's responsibility.

Planning validation: document boundaries and cross-references reviewed; no Web source, credentials, configuration, certificates or deployment created. Builds, browser tests and end-to-end execution are NOT RUN for this documentation-only task.
