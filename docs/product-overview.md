# Product Overview

OpenAPI Sample Service is a self-contained Open Banking reference API for teams that need a runnable, documented example of account information, consent management, and payment initiation workflows.

## Target customers

- API product teams evaluating Open Banking interaction patterns.
- Backend engineers learning ASP.NET Core minimal APIs with generated OpenAPI documentation.
- Solution architects prototyping account, consent, and payment journeys before integrating with real banking systems.
- Developer relations and enablement teams that need a compact demo service for workshops or documentation.

## Primary use cases

- Explore Open Banking account, balance, transaction, consent, and payment endpoints locally.
- Demonstrate API-key-protected resource access and standard HTTP error handling.
- Use the runtime OpenAPI document and Swagger UI as a contract exploration tool.
- Prototype client integrations against stable sample payloads without external dependencies.

## Current limitations

- Data is stored in memory and resets when the application restarts.
- Authentication uses a demo API key rather than production OAuth2/OIDC flows.
- Consent and payment workflows are simplified and do not represent a certified banking implementation.
- The service does not include persistence, audit logging, rate limiting, reconciliation, or operational monitoring.

## Production readiness

This project is intended for local development, education, and product discovery. It should not be used in production without replacing the demo security model, adding durable storage and observability, completing regulatory and compliance controls, and validating behavior against real Open Banking requirements.
