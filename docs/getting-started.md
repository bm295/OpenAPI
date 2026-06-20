# Getting Started

Use this guide to restore, run, and verify the OpenAPI Sample Service locally. The service is an ASP.NET Core minimal API with Open Banking sample endpoints.

## .NET SDK prerequisite

Install the .NET SDK version required by `global.json` before restoring or running the project. This repository pins the SDK to `10.0.100` and allows compatible later feature bands through roll-forward.

Verify the SDK is available:

```bash
dotnet --version
```

If the command is not found, install the .NET SDK and open a new terminal before continuing.

## Restore dependencies

From the repository root, restore the solution:

```bash
dotnet restore OpenAPI.sln
```

## Run the API

Start the Task API project:

```bash
dotnet run --project src/TaskApi/TaskApi.csproj
```

The application prints the HTTP and HTTPS addresses it is listening on. The local launch profile typically uses:

- `http://localhost:65348`
- `https://localhost:65347`

Use the printed address if your environment selects a different port.

## Verify health

The health endpoint is public and does not require an API key. In a second terminal, call `GET /health`:

```bash
curl http://localhost:65348/health
```

A healthy response includes a JSON body with `status` set to `ok` and a server timestamp.

## Verify authorization

Open Banking resource endpoints require an API key. Verify authorized access by sending the configured development key in the `X-API-Key` header:

```bash
curl -H "X-API-Key: dev-open-banking-key" \
  http://localhost:65348/open-banking/v1/accounts
```

A successful response returns the seeded account list. Requests without a key return `401 Unauthorized`; requests with an incorrect key return `403 Forbidden`.

## Troubleshooting missing .NET SDK

If `dotnet --version`, `dotnet restore OpenAPI.sln`, or `dotnet run --project src/TaskApi/TaskApi.csproj` fails because the SDK is missing or incompatible:

1. Install the .NET SDK version requested by `global.json`, or a compatible later feature band.
2. Restart your terminal so the updated `PATH` is loaded.
3. Run `dotnet --list-sdks` to confirm the SDK is installed.
4. Retry `dotnet restore OpenAPI.sln` from the repository root.

## Troubleshooting port conflicts

If the API fails to start because a port is already in use, another process is listening on the configured HTTP or HTTPS port.

Options:

- Stop the other process and rerun `dotnet run --project src/TaskApi/TaskApi.csproj`.
- Run the API on a different address for the current session:

```bash
dotnet run --project src/TaskApi/TaskApi.csproj --urls http://localhost:5050
```

Then use the replacement base URL for verification, for example `http://localhost:5050/health`.

## Troubleshooting 401 and 403 responses

Open Banking endpoints under `/open-banking/v1` require API key authorization.

- `401 Unauthorized` means no API key was supplied. Add `X-API-Key: dev-open-banking-key` or an `Authorization: Bearer dev-open-banking-key` header.
- `403 Forbidden` means a key was supplied but it does not match the configured value. Check `OpenBanking:ApiKey` in `src/TaskApi/appsettings.json` and retry with the configured key.
- `GET /health` is unauthenticated, so use it to confirm the service is running before debugging protected endpoint authorization.
