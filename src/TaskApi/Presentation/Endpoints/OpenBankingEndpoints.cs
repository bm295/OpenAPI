using TaskApi.Application.Abstractions;
using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

public static class OpenBankingEndpoints
{
    public static IEndpointRouteBuilder MapOpenBankingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthResponse("ok", DateTimeOffset.UtcNow)))
            .WithName("GetHealth");

        var group = app.MapGroup("/open-banking/v1")
            .WithTags("Open Banking");

        group.MapGet("/accounts", (int? pageSize, string? cursor, IOpenBankingService service) =>
        {
            var validation = ValidatePageSize(pageSize);
            if (validation is not null)
            {
                return validation;
            }

            return Results.Ok(service.ListAccounts(pageSize.GetValueOrDefault(25), cursor));
        })
        .WithName("ListAccounts");

        group.MapGet("/accounts/{accountId:guid}", (Guid accountId, IOpenBankingService service) =>
        {
            var account = service.GetAccount(accountId);
            return account is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Account was not found"))
                : Results.Ok(account);
        })
        .WithName("GetAccount");

        group.MapGet("/accounts/{accountId:guid}/balances", (Guid accountId, IOpenBankingService service) =>
        {
            var balance = service.GetBalance(accountId);
            return balance is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Account balance was not found"))
                : Results.Ok(balance);
        })
        .WithName("GetAccountBalance");

        group.MapGet("/accounts/{accountId:guid}/transactions", (Guid accountId, DateTimeOffset? from, DateTimeOffset? to, int? pageSize, string? cursor, IOpenBankingService service) =>
        {
            var validation = ValidatePageSize(pageSize);
            if (validation is not null)
            {
                return validation;
            }

            if (from is not null && to is not null && from > to)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "from must be earlier than or equal to to",
                    new[] { new ErrorDetail("from", "Invalid date range") }));
            }

            var transactions = service.ListTransactions(accountId, from, to, pageSize.GetValueOrDefault(25), cursor);
            return transactions is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Account was not found"))
                : Results.Ok(transactions);
        })
        .WithName("ListAccountTransactions");

        group.MapPost("/consents", (CreateConsentRequest request, IOpenBankingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "customerId is required",
                    new[] { new ErrorDetail("customerId", "Cannot be empty") }));
            }

            if (request.Permissions.Count == 0)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "At least one permission is required",
                    new[] { new ErrorDetail("permissions", "Cannot be empty") }));
            }

            if (request.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "expiresAt must be in the future",
                    new[] { new ErrorDetail("expiresAt", "Must be future dated") }));
            }

            var consent = service.CreateConsent(request);
            return Results.Created($"/open-banking/v1/consents/{consent.Id}", consent);
        })
        .WithName("CreateConsent");

        group.MapGet("/consents/{consentId:guid}", (Guid consentId, IOpenBankingService service) =>
        {
            var consent = service.GetConsent(consentId);
            return consent is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Consent was not found"))
                : Results.Ok(consent);
        })
        .WithName("GetConsent");

        group.MapDelete("/consents/{consentId:guid}", (Guid consentId, IOpenBankingService service) =>
        {
            var consent = service.RevokeConsent(consentId);
            return consent is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Consent was not found"))
                : Results.Ok(consent);
        })
        .WithName("RevokeConsent");

        group.MapPost("/payments", (CreatePaymentRequest request, IOpenBankingService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.DebtorAccountId) ||
                string.IsNullOrWhiteSpace(request.CreditorAccountNumber) ||
                string.IsNullOrWhiteSpace(request.CreditorName) ||
                string.IsNullOrWhiteSpace(request.RemittanceInformation))
            {
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "Payment debtor, creditor, and remittance fields are required"));
            }

            if (request.Amount.Amount <= 0 || string.IsNullOrWhiteSpace(request.Amount.Currency))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "Payment amount must be positive and include a currency",
                    new[] { new ErrorDetail("amount", "Invalid payment amount") }));
            }

            var payment = service.CreatePayment(request);
            return payment is null
                ? Results.BadRequest(new ErrorResponse("INVALID_CONSENT", "Consent is missing, revoked, or expired"))
                : Results.Created($"/open-banking/v1/payments/{payment.Id}", payment);
        })
        .WithName("CreatePayment");

        group.MapGet("/payments/{paymentId:guid}", (Guid paymentId, IOpenBankingService service) =>
        {
            var payment = service.GetPayment(paymentId);
            return payment is null
                ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Payment was not found"))
                : Results.Ok(payment);
        })
        .WithName("GetPayment");

        return app;
    }

    private static IResult? ValidatePageSize(int? pageSize)
    {
        var effectivePageSize = pageSize.GetValueOrDefault(25);
        return effectivePageSize is < 1 or > 100
            ? Results.BadRequest(new ErrorResponse(
                "INVALID_REQUEST",
                "pageSize must be between 1 and 100",
                new[] { new ErrorDetail("pageSize", "Out of range") }))
            : null;
    }
}
