# HTTP Examples

These examples assume the API is running locally on `http://localhost:65348` and that the default development API key is configured as `dev-open-banking-key`.

Set a base URL and API key in your shell if you want to copy and paste the examples as written:

```bash
BASE_URL=http://localhost:65348
API_KEY=dev-open-banking-key
```

## GET /open-banking/v1/accounts

```bash
curl -H "X-API-Key: ${API_KEY}" \
  "${BASE_URL}/open-banking/v1/accounts"
```

## GET /open-banking/v1/accounts/{accountId}

```bash
curl -H "X-API-Key: ${API_KEY}" \
  "${BASE_URL}/open-banking/v1/accounts/11111111-1111-1111-1111-111111111111"
```

## GET /open-banking/v1/accounts/{accountId}/balances

```bash
curl -H "X-API-Key: ${API_KEY}" \
  "${BASE_URL}/open-banking/v1/accounts/11111111-1111-1111-1111-111111111111/balances"
```

## GET /open-banking/v1/accounts/{accountId}/transactions

```bash
curl -H "X-API-Key: ${API_KEY}" \
  "${BASE_URL}/open-banking/v1/accounts/11111111-1111-1111-1111-111111111111/transactions?pageSize=10"
```

## POST /open-banking/v1/consents

```bash
curl -X POST \
  -H "X-API-Key: ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-demo",
    "permissions": [
      "ReadAccountsBasic",
      "ReadBalances",
      "ReadTransactions",
      "InitiatePayments"
    ],
    "expiresAt": "2030-01-01T00:00:00Z"
  }' \
  "${BASE_URL}/open-banking/v1/consents"
```

Save the returned `id` value as `CONSENT_ID` before using the consent-specific examples:

```bash
CONSENT_ID=<created-consent-id>
```

## DELETE /open-banking/v1/consents/{consentId}

```bash
curl -X DELETE \
  -H "X-API-Key: ${API_KEY}" \
  "${BASE_URL}/open-banking/v1/consents/${CONSENT_ID}"
```

## POST /open-banking/v1/payments

Create an active consent first, then use its `id` as the `consentId` value in the payment request.

```bash
curl -X POST \
  -H "X-API-Key: ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d "{
    \"consentId\": \"${CONSENT_ID}\",
    \"debtorAccountId\": \"11111111-1111-1111-1111-111111111111\",
    \"creditorAccountNumber\": \"US1234567890\",
    \"creditorName\": \"Example Supplier\",
    \"amount\": {
      \"amount\": 42.50,
      \"currency\": \"USD\"
    },
    \"remittanceInformation\": \"Invoice INV-1001\"
  }" \
  "${BASE_URL}/open-banking/v1/payments"
```
