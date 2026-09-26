# Phase 7 - End-to-End Validation

Validation date: 2026-09-21

Status: COMPLETE WITH EXPLICIT NOT RUN ITEMS

## Runtime endpoints

- Auth: `https://DC01.lab.local`
- API: `https://localhost:7196`
- Web: `https://localhost:7153`
- Database: `DC01`, database `LabAPIServer_Test`, Integrated authentication

The API and Web used process-only test configuration overrides for the current launch-profile ports. No repository configuration was changed. The API trusted the public key corresponding to the active Auth signing certificate; no private key was exported or copied.

## Results

### Auth and session

- Auth health: HTTP 200.
- Auth login through the Web: HTTP 200.
- Web login: PASS.
- Authenticated identity: `test.itd@lab.local`.
- Role: `Operator`.
- API session verification through Web: HTTP 200.
- Authenticated Web home: HTTP 200.
- Web logout: PASS; authenticated access redirected to `/Account/Login`.
- JWT browser exposure: PASS. `localStorage` and `sessionStorage` were empty, `document.cookie` exposed no cookie, and rendered body text contained no JWT-shaped value. The browser receives only the Web session mechanism.

The password was entered interactively in the browser. It was not printed, saved, logged, or documented. The JWT was not printed, saved, or documented.

### Web to API and SQL

- API health: HTTP 200.
- Unauthenticated `/api/v1/session`: HTTP 401.
- Unauthenticated Work-item list: HTTP 401.
- Baseline list: six records, with 2 Open, 2 InProgress, and 2 Complete.
- Create: PASS through Web -> API -> stored procedure -> SQL. Synthetic name: `[PHASE7 TEST] Create`.
- Generated ID: `F8ACF3B8-EC6F-4C94-B99B-734B15C76A9C`.
- Update: PASS through the Web path. Name, description, and status changed to `[PHASE7 TEST] Updated`, `Phase 7 updated nullable description`, and `InProgress`.
- Detail/Get: PASS through the Web detail route, API `GET /api/v1/work-items/{id}`, and `app.WorkItems_Get`. Fixture `11111111-1111-1111-1111-111111111101` rendered its description correctly; fixture `11111111-1111-1111-1111-111111111106` rendered nullable description as `(none)`.
- SQL verification: the updated record was present with the expected values.
- Delete: PASS through the Web path. The synthetic record disappeared from the Web list and SQL.
- Final SQL state: six total rows, zero Phase 7 temporary rows, and status distribution Open 2, InProgress 2, Complete 2.

### Authorization and errors

- Reader live: NOT RUN; no authorized Reader identity was supplied.
- Reader API: PASS; read allowed and write denied in the opt-in API LocalDB integration test.
- Operator live: PASS; list, create, update, and delete passed through the Web/API path.
- Operator API: PASS in the existing API policy/CRUD tests.
- Administrator live: NOT RUN; no authorized Administrator identity was supplied.
- Administrator API: PASS; CRUD is covered by the existing explicit-policy API test.
- 400, 401, 403, and 404 behavior: covered by existing API automated tests and the live unauthenticated 401 checks. No response exposed credentials, tokens, private keys, SQL connection strings, stack traces, or internal paths.

No Auth compatibility defect was found. An initial temporary API test configuration used the wrong public certificate and correctly rejected the Auth token; the process was restarted with the matching public certificate. LabAuthServer source and configuration were not modified.

## SQL boundary and security scan

- API Work-item CRUD C# contains no inline `SELECT`, `INSERT`, `UPDATE`, or `DELETE` statements.
- `SqlWorkItemStore` calls `app.WorkItems_List`, `app.WorkItems_Get`, `app.WorkItems_Create`, `app.WorkItems_Update`, and `app.WorkItems_Delete` with `CommandType.StoredProcedure`.
- Web SQL implementation: none.
- Web JWT validation: none.
- Web browser JWT storage: none.
- Private keys, raw JWTs, passwords, and credential-bearing source values: none found by source scan.
- TLS bypasses: none used.

## Automated validation

API:

- Restore: PASS.
- Release build: PASS.
- Default test command: the SQL integration test is explicitly gated and reports a dynamic skip/failure unless `LABAPI_RUN_SQL_TESTS=1` is set.
- SQL-enabled test command with `LABAPI_RUN_SQL_TESTS=1`: PASS, 23 passed, 0 failed.

Web:

- Restore: PASS.
- Release build: PASS.
- Tests: PASS, 5 passed, 0 failed, including typed detail GET and nullable-description coverage.

Security scan: PASS.
SQL boundary scan: PASS.
Git diff check: PASS before documentation; final check is recorded below.

## Cleanup and boundaries

- Temporary Phase 7 Work-item deleted; permanent six-row fixture retained.
- Temporary API/Web/Auth processes stopped after validation.
- No JWT, password, response file, public-key export, or credential material was written to the repositories or documented.
- Production database touched: NO.
- Deployment: NOT RUN.
- Release packaging: NOT RUN.
- Phase 8+: NOT RUN.
- LabAuthServer changed by Phase 7: NO.

## Explicit NOT RUN items

- Live Reader browser login, because no authorized Reader identity was supplied; API Reader denial was tested by the isolated integration suite.
- Live Administrator browser login, because no authorized Administrator identity was supplied.
- Production migration, deployment, packaging, release, rollback, and Phase 8+ work.

## Final integrity

The final repository status and `git diff --check` were run for LabAPIServer, LabWebAppServer, and LabAuthServer. LabAuthServer remained unmodified by Phase 7. Phase 7 is complete; Phase 8 and all later work remain explicitly out of scope.