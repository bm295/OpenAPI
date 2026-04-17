# Task API — OpenAPI + C# Project

This repository now contains a runnable **C# 10** ASP.NET Core API project that exposes and documents a task service using OpenAPI.

> As of **April 17, 2026**, .NET 14 is not released. This project targets **.NET 10** while locking language features to **C# 10**.

## Project Layout

- `OpenAPI.sln` — solution file.
- `src/TaskApi` — ASP.NET Core minimal API implementation.
- `openapi/openapi.yaml` — canonical API contract.
- `examples/task.created.json` — sample response payload.
- `scripts/validate.sh` — OpenAPI lint helper.

## Run the API

```bash
dotnet restore OpenAPI.sln
dotnet run --project src/TaskApi/TaskApi.csproj
```

When running in development, the app serves OpenAPI JSON at:

- `http://localhost:5000/openapi/v1.json` (or your configured port)

## Endpoints

- `GET /health`
- `GET /tasks`
- `POST /tasks`
- `GET /tasks/{taskId}`
- `PATCH /tasks/{taskId}`
- `DELETE /tasks/{taskId}`

## OpenAPI Validation

```bash
./scripts/validate.sh
```

If `redocly` is not installed, the script prints the install command.
