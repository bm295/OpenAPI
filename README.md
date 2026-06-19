# OpenAPI Sample Service

A runnable ASP.NET Core minimal API that demonstrates how to pair a Clean Architecture-style C# service with a checked-in OpenAPI contract.

The sample domain is Open Banking: account information, consent management, and payment initiation. The root README focuses on getting the project running; the domain-specific behavior is documented in [docs/open-banking.md](docs/open-banking.md).

## What is included

- `OpenAPI.sln` — solution entry point.
- `global.json` — pins the .NET SDK to `10.0.100` with latest-feature roll-forward.
- `src/TaskApi` — ASP.NET Core minimal API implementation.
- `src/TaskApi/appsettings.json` — local configuration, including the demo Open Banking API key.
- `openapi/openapi.yaml` — canonical OpenAPI 3.1 contract.
- `examples/open-banking.payment.created.json` — sample payment initiation response.
- `scripts/validate.sh` — Redocly-based OpenAPI validation helper.
- `docs/open-banking.md` — Open Banking endpoint and behavior guide.

## Architecture

The API code is organized by responsibility under `src/TaskApi`:

| Area | Purpose |
| --- | --- |
| `Domain` | Core Open Banking records and enums. |
| `Application` | Service interfaces, contracts, and application logic. |
| `Infrastructure` | In-memory data store seeded with demo banking data. |
| `Presentation` | HTTP endpoint mapping and request validation. |

The service uses an in-memory store so the API can be restored and run without a database or external dependencies.

## Prerequisites

- .NET SDK `10.0.100` or compatible later feature band.
- Optional: Redocly CLI for OpenAPI linting. If it is not installed, `scripts/validate.sh` prints the install command.

## Quick start

Restore dependencies:

```bash
dotnet restore OpenAPI.sln
```

Run the API:

```bash
dotnet run --project src/TaskApi/TaskApi.csproj
```

Open the app:

- `https://localhost:65347/`
- `http://localhost:65348/`

The root URL redirects to the local Swagger UI at `/swagger/index.html`. If your local ports differ, use the addresses printed by `dotnet run`.

## Authorization

Open Banking resource endpoints require a demo API key. Supply the configured key from `OpenBanking:ApiKey` using either header form:

```bash
curl -H "X-API-Key: dev-open-banking-key" http://localhost:65348/open-banking/v1/accounts
```

```bash
curl -H "Authorization: Bearer dev-open-banking-key" http://localhost:65348/open-banking/v1/accounts
```

The `/health`, `/swagger/index.html`, and development OpenAPI document endpoints remain public for local diagnostics and exploration.

## API documents

- Swagger UI: `/swagger/index.html`
- Runtime OpenAPI JSON: `/openapi/v1.json` in development
- Source OpenAPI contract: `openapi/openapi.yaml`
- Open Banking guide: [docs/open-banking.md](docs/open-banking.md)

## Validate the OpenAPI contract

```bash
./scripts/validate.sh
```

## Example payload

A sample payment-created response is available at:

```text
examples/open-banking.payment.created.json
```

Use it as a quick reference for response shape while working with payment initiation.
