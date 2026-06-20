# OpenAPI Sample Service

A runnable ASP.NET Core minimal API that demonstrates a Clean Architecture-style C# service with runtime OpenAPI documentation.

The sample domain is Open Banking: account information, consent management, and payment initiation. The root README focuses on getting the project running; the domain-specific behavior is documented in [docs/open-banking.md](docs/open-banking.md), with product positioning in [docs/product-overview.md](docs/product-overview.md) and terminology in [docs/glossary.md](docs/glossary.md).

> **Production-readiness disclaimer:** This service is a local development and reference sample. It is not production-ready and should not be used with real customer, financial, or regulated banking data without substantial security, compliance, persistence, observability, and operational hardening.


## Who this is for

- Developers who want a runnable ASP.NET Core minimal API with OpenAPI documentation.
- Product and architecture teams exploring Open Banking account, consent, and payment flows.
- Client integrators who need a stable local API for contract exploration and demo automation.

## Who this is not for

- Teams looking for a certified production Open Banking platform.
- Applications that need durable storage, real bank connectivity, OAuth2/OIDC consent journeys, or regulatory compliance out of the box.
- Workloads that process live customer financial data.

## Roadmap

- Replace the in-memory store with durable repository implementations.
- Add production-grade OAuth2/OIDC authorization and scope validation.
- Expand audit logging, error contracts, examples, and automated test coverage.
- Document security, operational readiness, and deployment guidance.

## What is included

- `OpenAPI.sln` — solution entry point.
- `global.json` — pins the .NET SDK to `10.0.100` with latest-feature roll-forward.
- `src/TaskApi` — ASP.NET Core minimal API implementation.
- `src/TaskApi/appsettings.json` — local configuration, including the demo Open Banking API key.
- `examples/open-banking.payment.created.json` — sample payment initiation response.
- `docs/open-banking.md` — Open Banking endpoint and behavior guide.
- `docs/product-overview.md` — Product positioning, target customers, limitations, and readiness notes.
- `docs/glossary.md` — Definitions for core Open Banking and API terms.

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
- Open Banking guide: [docs/open-banking.md](docs/open-banking.md)
- Product overview: [docs/product-overview.md](docs/product-overview.md)
- Glossary: [docs/glossary.md](docs/glossary.md)

## Example payload

A sample payment-created response is available at:

```text
examples/open-banking.payment.created.json
```

Use it as a quick reference for response shape while working with payment initiation.
