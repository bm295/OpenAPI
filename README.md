# Open Banking API — Clean Architecture + C# Project

This repository contains a runnable **C# 10** ASP.NET Core API project that exposes an Open Banking-style service and documents it with OpenAPI.

> As of **April 17, 2026**, .NET 14 is not released. This project targets **.NET 10** while locking language features to **C# 10**.

## Project Layout

- `OpenAPI.sln` — solution file.
- `src/TaskApi` — ASP.NET Core minimal API implementation organized with Clean Architecture folders:
  - `Domain` — Open Banking domain records and enums.
  - `Application` — use-case contracts, service interface, and service implementation.
  - `Infrastructure` — in-memory data store seeded with demo banking data.
  - `Presentation` — HTTP endpoint mapping and request validation.
- `openapi/openapi.yaml` — canonical API contract.
- `examples/open-banking.payment.created.json` — sample payment initiation response payload.
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
- `GET /open-banking/v1/accounts`
- `GET /open-banking/v1/accounts/{accountId}`
- `GET /open-banking/v1/accounts/{accountId}/balances`
- `GET /open-banking/v1/accounts/{accountId}/transactions`
- `POST /open-banking/v1/consents`
- `GET /open-banking/v1/consents/{consentId}`
- `DELETE /open-banking/v1/consents/{consentId}`
- `POST /open-banking/v1/payments`
- `GET /open-banking/v1/payments/{paymentId}`

## OpenAPI Validation

```bash
./scripts/validate.sh
```

If `redocly` is not installed, the script prints the install command.
