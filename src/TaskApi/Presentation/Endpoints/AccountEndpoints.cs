using TaskApi.Application.Abstractions;
using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

internal static class AccountEndpoints
{
    internal static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/accounts", (int? pageSize, string? cursor, IOpenBankingService service) =>
        {
            var validation = EndpointValidation.ValidatePageSize(pageSize);
            return validation ?? Results.Ok(service.ListAccounts(pageSize.GetValueOrDefault(25), cursor));
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
            var validation = EndpointValidation.ValidatePageSize(pageSize);
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

        return group;
    }
}
