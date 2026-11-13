# Team guidelines: Notes (example project)

A small notes website. A .NET Web API in `backend/` serves JSON; an Angular
app in `frontend/` is the UI. Every agent reads this file. The reviewer
enforces it.

## Layout
- `backend/Notes.sln` with two projects: `backend/Notes.Api` (ASP.NET Core
  Web API) and `backend/Notes.Api.Tests` (xUnit).
- `frontend/` is one Angular application created with the Angular CLI.
- Nothing else at the repository root except `README.md`, `.gitignore` and `.team/`.

## Versions and tools
- Use the newest .NET SDK and Angular CLI already installed on this machine.
  Do not install SDKs or global tools. If something is missing, stop and
  report BLOCKED.
- Do not add a NuGet or npm package without naming it and the reason in your
  handoff note. Prefer what the framework already provides.

## Backend rules
- Controllers stay thin: validate, call a service, return a result. Logic
  lives in services under `Notes.Api/Services`.
- Data access goes through EF Core with SQLite. No raw SQL.
- Request and response bodies are DTO records, never EF entities.
- Invalid input returns 400 with a problem-details body; a missing note
  returns 404. Never return 500 for bad input.
- All routes are under `/api`. JSON uses camelCase.
- Nullable reference types stay enabled. No compiler warnings.

## Frontend rules
- Standalone components, strict TypeScript, no `any`.
- Only services under `src/app/services` talk to the API (`HttpClient`).
  Components never call `HttpClient` directly.
- The API base URL comes from one place (environment file or a dev proxy
  config), never hard-coded in a component or service method.
- Show a visible message when a request fails or a list is empty. No silent failures.
- Plain CSS in component styles. No UI library.

## Tests
- Backend: xUnit. API behaviour is tested through
  `WebApplicationFactory` with an in-memory SQLite database, not by starting
  a real server.
- Frontend: the test runner the Angular CLI scaffolded. `frontend/package.json`
  must keep a `test:ci` script that runs all tests once, headless, and exits
  non-zero on failure.
- Every endpoint and every component with logic has tests. A bug fix comes
  with a test that fails without the fix.
- The full check is:
  `dotnet test backend` and `(cd frontend && npm run test:ci)`.

## Running things (several agents work at the same time)
- Do not leave servers running. If you start `dotnet run` or `ng serve` to
  try something, stop it before you finish your turn.
- Never use the default ports 5000, 5001 or 4200 for a manual check: another
  agent may be using them. Pick a random free port
  (`--urls http://127.0.0.1:<port>`, `ng serve --port <port>`).

## Definition of done
- Both test commands pass and both projects build without warnings.
- `README.md` says how to run and test anything that changed for a user.
- No commented-out code, no debug output, no TODO without an explanation.

## Out of bounds
- Never commit `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/`, `*.db`
  files, or secrets.
- No authentication, no deployment config, no Docker. This is an example
  project; keep it small.
