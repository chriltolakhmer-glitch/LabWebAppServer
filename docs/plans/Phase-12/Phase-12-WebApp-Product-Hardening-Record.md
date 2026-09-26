# Phase 12 - WebApp product hardening record

Date: 2026-09-22 (Asia/Bangkok).

**PHASE 12 COMPLETE - SOURCE HARDENING VALIDATED.** The accepted Phase 11 Web/Auth/API/SQL architecture was preserved. No Auth, JWT, API contract, SQL schema/procedure, IIS, certificate, deployment, or infrastructure change was made.

## Implemented changes

- Added consistent authenticated navigation, a Work-items anchor, semantic main/table markup, captions, scopes, labels, validation summaries, and live error regions.
- Kept Reader read-only: Reader sees list/detail actions only. Operator and Administrator retain create/update/delete controls.
- Distinguished a successful empty Work-item list from a failed list load and preserved the list after failed mutations.
- Added safe presentation for API 400, 403, 404, and 502/503/504 outcomes, including not-found and temporary-unavailability messages.
- Added friendly status-code error presentation for 400/404/503 responses and a usable access-denied page.
- Kept Work-item detail read-only and aligned page titles, headings, actions, and navigation.

## Files changed

- `src/LabWebAppServer.Web/Program.cs`
- `src/LabWebAppServer.Web/Pages/Shared/_Layout.cshtml`
- `src/LabWebAppServer.Web/Pages/Index.cshtml`
- `src/LabWebAppServer.Web/Pages/Index.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/WorkItems/Details.cshtml`
- `src/LabWebAppServer.Web/Pages/WorkItems/Details.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Error.cshtml`
- `src/LabWebAppServer.Web/Pages/Error.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Account/AccessDenied.cshtml`
- This Phase 12 record.

## Validation

| Check | Result |
| --- | --- |
| Web Release solution build | PASS |
| Web tests, Release | PASS - 5 passed, 0 failed, 0 skipped |
| API non-SQL regression tests | PASS - 22 passed, 0 failed, 0 skipped |
| HTTPS source smoke | PASS - `https://localhost:17268/health` returned 200 and `/Error?statusCode=404` rendered the friendly error page |
| JWT/password/token leakage scan | PASS - no private-key, bearer-JWT, password-literal, localStorage, or sessionStorage matches |
| `git diff --check` | PASS |

The temporary HTTPS process was stopped after the smoke check. The test hosts emitted only the existing development warning that no HTTPS redirect port is configured.

## Known limitations

- Error messages are intentionally generic and do not expose upstream response bodies or correlation details.
- The existing Web test suite remains foundation/client-focused; it does not provide a browser test with a Reader and Operator fixture for every new conditional rendering branch.
- The in-memory session behavior and existing server-side opaque-token boundary remain unchanged.

## Explicit NOT RUN

- No deployment or IIS/Current artifact update was performed.
- No second credentialed deployed Web -> Auth -> API -> SQL acceptance run was performed after source changes; Phase 11 remains the accepted deployed baseline.
- Reader credentialed deployed acceptance, Administrator acceptance, and deployed post-change CRUD persistence were not run.
- No API SQL integration test was run; the API test command explicitly excluded `LocalDbIntegrationTests`.
- No Auth, JWT architecture, API contract, SQL, certificate, or infrastructure validation was needed or run for this source-only slice.

**PHASE 12 COMPLETE - SOURCE HARDENING VALIDATED; DEPLOYMENT INTENTIONALLY NOT RUN.**