# Notes

A small notes website, kept as an example project:

- `backend/` — an ASP.NET Core Web API (`backend/Notes.sln` with `Notes.Api` and the xUnit project `Notes.Api.Tests`). It serves JSON under `/api`.
- `frontend/` — an Angular app created with the Angular CLI. It is the UI and calls the API.

## Prerequisites

- .NET SDK 10 (the projects target `net10.0`)
- Node.js and npm (developed with Node 26 and npm 11)
- Angular CLI 22 — installed locally by `npm install` in `frontend/`, so no global install is needed

All commands below are run from the repository root.

## Running the API

```sh
dotnet run --project backend/Notes.Api
```

The `http` launch profile (`backend/Notes.Api/Properties/launchSettings.json`) makes the API listen on http://localhost:5180 in the Development environment. Check it with http://localhost:5180/api/health, which returns `{"status":"ok"}`.

### Notes API

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/notes` | `200` with all notes, newest `createdAt` first |
| `GET` | `/api/notes/{id}` | `200` with the note, or `404` |
| `POST` | `/api/notes` | `201` with the created note and a `Location` header |
| `PUT` | `/api/notes/{id}` | `200` with the updated note, or `404` |
| `DELETE` | `/api/notes/{id}` | `204`, or `404` |

`POST` and `PUT` take `{"title": "...", "body": "..."}`. The title is required and must be 1–100 characters after trimming (it is stored trimmed); the body is optional, up to 2000 characters. Invalid input returns `400` with a problem-details body. Notes have `id`, `title`, `body`, `createdAt` and `updatedAt` (UTC, ending in `Z`).

Notes are stored in SQLite. The connection string is `ConnectionStrings:Notes` in `backend/Notes.Api/appsettings.json` (`Data Source=notes.db`), so with the command above the database file is `backend/Notes.Api/notes.db`. The API creates it automatically on first start; it is git-ignored. Delete it to start with an empty database. The tests use an in-memory SQLite database and never create this file.

## Running the frontend

Start the API first, then in a second terminal:

```sh
cd frontend && npm install && npm start
```

`npm start` runs `ng serve`, which serves the app on http://localhost:4200. The dev server proxies every request under `/api` to the API at http://localhost:5180 (`frontend/proxy.conf.json`), so the app shows API data only while the API is running.

## Building

```sh
dotnet build backend
```

```sh
cd frontend && npm run build
```

The frontend build output goes to `frontend/dist/`.

## Running all tests

```sh
dotnet test backend
```

```sh
cd frontend && npm run test:ci
```

`npm run test:ci` runs the frontend tests once, headless, and exits non-zero on failure. If `frontend/node_modules` is missing, it first runs `npm ci` to install the locked dependencies.
