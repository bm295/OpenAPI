using TaskApi.Application.Abstractions;
using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

internal static class PaymentEndpoints
{
    internal static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder group)
    {
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

        return group;
    }
}
