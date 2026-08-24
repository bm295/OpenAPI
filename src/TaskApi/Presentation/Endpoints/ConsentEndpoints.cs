using TaskApi.Application.Abstractions;
using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

internal static class ConsentEndpoints
{
    internal static RouteGroupBuilder MapConsentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/consents", (CreateConsentRequest request, IOpenBankingService service, TimeProvider timeProvider) =>
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

            if (request.ExpiresAt <= timeProvider.GetUtcNow())
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

        return group;
    }
}
