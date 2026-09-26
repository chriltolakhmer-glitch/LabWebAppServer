# LabWebAppServer operations and recovery

Verified 2026-09-22 (Asia/Bangkok). See the [Phase 10 record](../plans/Phase-10/Phase-10-Operations-Acceptance-Record.md). Same-host representative service startup from the retained recovery set is TESTED; production rollback and host-loss recovery are NOT RUN.

## Active deployment and health

- IIS site `LabWebAppServer`, site ID 4; pool `LabWebAppServerAppPool`; both Started.
- Active physical path: `C:\Apps\LabWebAppServer\Current`.
- Pool: ApplicationPoolIdentity, one worker, `loadUserProfile=false`.
- Release: `C:\Apps\LabWebAppServer\Releases\v1.0.0\LabWebAppServer-1.0.0.zip`; 197 files; adjacent `SHA256SUMS.txt` is authoritative.
- HTTPS: `https://localhost:7153`, existing trusted localhost development certificate. Only the local route/client was tested.

```powershell
Invoke-WebRequest -Uri https://localhost:7153/ -UseBasicParsing -TimeoutSec 20
Invoke-WebRequest -Uri https://localhost:7153/health -UseBasicParsing -TimeoutSec 20
Invoke-WebRequest -Uri https://localhost:7153/Account/Login -UseBasicParsing -TimeoutSec 20
& "$env:windir\System32\inetsrv\appcmd.exe" list site LabWebAppServer
& "$env:windir\System32\inetsrv\appcmd.exe" list apppool LabWebAppServerAppPool
```

All return 200 after normal redirects; anonymous `/` finishes at `/Account/Login?ReturnUrl=%2F`. `/health` is independent liveness, not proof of Auth/API/SQL availability or a successful login.

## Configuration, sessions and diagnosis

Pool variables select `AuthClient__BaseUrl=https://DC01.lab.local` and `ApiClient__BaseUrl=https://localhost:7196`. No `LABWEB_CONFIG_PATH` is configured and no `C:\ProgramData\LabWebAppServer` directory exists. Default configuration plus pool environment variables supply the current settings. If external JSON is later configured, the existing loader adds it last, overriding duplicate default-provider/environment values, with reload disabled.

Web obtains identity/expiry from API `/api/v1/session`; it treats JWTs as opaque. The token/ticket stays in the in-process `ServerTicketStore`; the Secure, HttpOnly, SameSite=Lax `__Host-LabWebSession` cookie carries the protected handle. Source contains no JWT signature validator, SQL client or browser local/session storage use. An actual authenticated browser storage/session journey was NOT RUN in this phase.

Restart/recycle loses in-memory tickets and forces sign-in. Do not back up tokens/tickets or promise session survival. An explicit external Data Protection key directory/application discriminator is not configured in the inspected source/pool settings; effective key persistence and its recovery are unverified. Do not assume a ProgramData key directory exists. This remains a recovery limitation.

IIS W3C logs are enabled, daily, at `C:\inetpub\logs\LogFiles\W3SVC4`; endpoint records were observed in `u_ex260921.log`. Windows Application log contains historical IIS/ANCM startup/error events. ANCM stdout logging is disabled; `.\logs\stdout` is not an active sink. No fresh application failure was induced, and application diagnostic capture/retention was not proved. Production code uses `/Error` and HSTS. Do not log passwords, bearer headers, cookies or upstream bodies.

## Scoped restart and recovery checklist

These are operator instructions, **not actions performed in Phase 10**. Notify users that sessions will be lost before planned maintenance.

```powershell
& "$env:windir\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:LabWebAppServerAppPool
& "$env:windir\System32\inetsrv\appcmd.exe" start apppool /apppool.name:LabWebAppServerAppPool
```

Repeat the three health/page checks afterward. Validate a real sign-in only with an authorized test account. Never restart Auth/API or use `iisreset` as part of this scoped action.

1. Identify prior immutable ZIP/checksum, matching Web pool/settings, Data Protection requirements and API contract compatibility before rollback. The current-version same-host set is `C:\ProgramData\LabAPIServer\Recovery\Phase10-20260922`; verify its SHA256SUMS.txt. It does not provide an older working release or host-loss recovery.
2. The protected set retains scoped settings/IIS metadata and the Web ZIP/checksum. Never archive live user tickets/JWTs. No private key was copied; this same-host recovery depends on existing certificate/key infrastructure and does not promise session survival or off-host key recovery.
3. Restore the ZIP into isolation and compare every file. The current 197-file package was recovered and started with the recovered API URL on trusted `https://localhost:17153`; home/health/login page returned 200. Both services stopped gracefully afterward. This was Kestrel under the inspection identity, not IIS identity or real credentialed login acceptance.
4. An older Web must be compatible with the current API. Only current v1.0.0 file identity was verified; no older-pair runtime compatibility is claimed.
5. A first-install fallback disables only this new Web site/pool and preserves evidence. It is an outage fallback, not a tested prior working service. No rollback was executed.

Weekly: trusted health/version/path, failures and backup completion. Monthly: certificate/runtime/access and backup readability/retention review; representative isolated restore once its target/recovery set is established. No scheduler or monitoring platform was added.

Owner-confirmed objectives are RPO 24 hours/RTO one working day. The representative combined drill finished availability/parity verification in 4 minutes 49.791 seconds; the isolated SQL backup recovered all six synthetic rows. Recovery deliberately loses volatile Web sessions and requires re-login; no user ticket or private key is copied. The existing host certificate/key infrastructure remains a prerequisite. Production service rollback, real Auth login and host-loss recovery are NOT RUN. IIS API SQL remains unconfigured, so Work-item persistence cannot be accepted from Web page health.
