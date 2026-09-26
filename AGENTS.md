# LabWebAppServer Agent Instructions

## Required before implementation

- Read the project plan and the current Phase 0 decision record before making changes.
- Preserve the Auth/API/Web boundaries defined by the shared architecture.
- Do not modify LabAuthServer without explicit instruction.
- Do not add unnecessary architecture or framework projects.
- Keep secrets and credentials out of source control.
- Run the relevant tests after changes.
- Update the phase record when implementation work changes status.

## Phase 1 constraints

- Keep the project minimal and buildable.
- Do not implement real login, JWT handling, API integration, SQL access, or business pages in Phase 1.
- Keep configuration placeholders and local-only development values out of source.
- Validate startup and page rendering only; do not claim later-phase functionality.

## Current development and release rules

The Phase 1 constraints above are historical and apply only to that phase. For current work, follow docs/Development-Rules.md. New, changed, or removed API endpoints require Postman updates; acceptance tests are required before release. See README.md for current implemented scope and configuration.

