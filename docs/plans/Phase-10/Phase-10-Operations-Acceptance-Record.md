# Phase 10 - Operations and final acceptance

Date: 2026-09-22 (Asia/Bangkok).

**PHASE 10 COMPLETE — IIS SQL RUNTIME VALIDATION NOT RUN.** The authoritative shared [Phase 10 evidence, commands and limitations](../../../../../../LabAPIServer/Source/LabAPIServer/docs/plans/Phase-10/Phase-10-Operations-Acceptance-Record.md) is maintained in the API repository; the shared roadmap defines this phase as Operations.

Web HTTPS checks passed, existing Release tests passed 5/5, and the 197-file package matched its checksum, Current and isolated restored files. See the [Web operations runbook/recovery checklist](../../operations/LabWebAppServer-Operations-Runbook.md).

The owner confirmed RPO 24 hours/RTO one working day and approved `DC01 / LabAPIServer_Phase10_Recovery_20260922`. A real synthetic database backup/restore/integrity/CRUD/authorization exercise passed, with zero fixture loss. API tests passed 23/23; Web passed 5/5. The disposable database was removed without changing the existing test database.

Exact API/Web artifacts were recovered into an isolated directory; both started with recovered configuration and existing trusted HTTPS on ports 17196/17153. Health/page checks passed, API anonymous session returned 401, and representative recovery took 4 minutes 49.791 seconds. The processes stopped gracefully and the temporary directory was removed. A checksummed recovery set remains at `C:\ProgramData\LabAPIServer\Recovery\Phase10-20260922` with restricted access.

This is same-host representative recovery under the inspection identity. Real credentialed Auth login, production IIS rollback and host-loss recovery remain NOT RUN. IIS runtime SQL configuration is still unavailable; deployed persistence was not live-verified. Current deployment, configuration, existing source, release ZIPs and Auth were preserved. No later phase started.
