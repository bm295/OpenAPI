# Open Banking API Guide

This project implements a compact Open Banking-style API for account information, account access consent, and payment initiation. The endpoints are mounted under `/open-banking/v1` in the running ASP.NET Core application.

## Scope

The API demonstrates these Open Banking capabilities:

- Account discovery and account detail lookup.
- Balance retrieval for a known account.
- Transaction search with optional date filtering and pagination.
- Account access consent creation, lookup, and revocation.
- Payment initiation and payment status lookup.
- Standard error responses for invalid requests and missing resources.

The implementation is intentionally self-contained: data is stored in memory and seeded at startup for local development and contract exploration. Open Banking resource endpoints are protected by a demo API key configured in `OpenBanking:ApiKey`.

## Base path

```text
/open-banking/v1
```

The standalone health check is exposed outside the versioned Open Banking path:

```text
GET /health
```

## Authorization

All `/open-banking/v1` endpoints require an API key. The sample accepts either of these headers:

```http
X-API-Key: dev-open-banking-key
```

```http
Authorization: Bearer dev-open-banking-key
```

Requests without a key return `401 Unauthorized`. Requests with an incorrect key return `403 Forbidden`. The `GET /health` endpoint is intentionally unauthenticated.

## Endpoints

### System

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/health` | Returns service health and current server timestamp. |

### Accounts

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/open-banking/v1/accounts` | Lists available accounts. Supports `cursor` and `pageSize`. |
| `GET` | `/open-banking/v1/accounts/{accountId}` | Returns one account by UUID. |
| `GET` | `/open-banking/v1/accounts/{accountId}/balances` | Returns the account balance. |
| `GET` | `/open-banking/v1/accounts/{accountId}/transactions` | Lists account transactions. Supports `from`, `to`, `cursor`, and `pageSize`. |

### Consents

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/open-banking/v1/consents` | Creates account access consent. |
| `GET` | `/open-banking/v1/consents/{consentId}` | Returns one consent by UUID. |
| `DELETE` | `/open-banking/v1/consents/{consentId}` | Revokes an active consent. |

### Payments

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/open-banking/v1/payments` | Initiates a payment using a valid consent. |
| `GET` | `/open-banking/v1/payments/{paymentId}` | Returns one payment instruction by UUID. |

## Pagination

List endpoints accept these query parameters:

| Parameter | Type | Default | Rule |
| --- | --- | --- | --- |
| `cursor` | string | none | Opaque cursor returned by a previous page. |
| `pageSize` | integer | `25` | Must be between `1` and `100`. |

Pagination is available for account listing and transaction listing.

## Transaction filtering

`GET /open-banking/v1/accounts/{accountId}/transactions` accepts optional date-time filters:

| Parameter | Type | Description |
| --- | --- | --- |
| `from` | date-time | Includes transactions at or after this timestamp. |
| `to` | date-time | Includes transactions at or before this timestamp. |

When both values are supplied, `from` must be earlier than or equal to `to`.

## Consent lifecycle

A consent request must include:

- A non-empty `customerId`.
- At least one requested permission.
- An `expiresAt` value in the future.

Created consents can be retrieved by ID. Revoking a consent changes its state and prevents it from being used for new payment initiation.

## Payment initiation

Payment creation validates that:

- Debtor account, creditor account, creditor name, and remittance information are present.
- Amount is positive and includes a currency.
- The referenced consent exists and is active.

A successful request returns `201 Created` and a payment instruction. See `examples/open-banking.payment.created.json` for a sample response body.

## Error handling

The API returns structured error objects for common failures:

- `400 Bad Request` for validation errors such as invalid pagination, empty required fields, invalid date ranges, or invalid consent for payment creation.
- `401 Unauthorized` when an Open Banking request does not include an API key.
- `403 Forbidden` when an Open Banking request includes an invalid API key.
- `404 Not Found` when an account, consent, balance, transaction collection, or payment cannot be found.

## OpenAPI contract

The canonical OpenAPI 3.1 contract is stored at `openapi/openapi.yaml`. Validate it with:

```bash
./scripts/validate.sh
```
