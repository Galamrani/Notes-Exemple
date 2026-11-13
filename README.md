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

`npm run test:ci` runs the frontend tests once, headless, and exits non-zero on failure. Run `npm install` in `frontend/` first if you have not already.
