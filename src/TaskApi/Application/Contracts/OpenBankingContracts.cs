using TaskApi.Domain;

namespace TaskApi.Application.Contracts;

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);

public sealed record ErrorResponse(string Code, string Message, IReadOnlyCollection<ErrorDetail>? Details = null);

public sealed record ErrorDetail(string Field, string Issue);

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, string? NextCursor);

public sealed record CreateConsentRequest(
    string CustomerId,
    IReadOnlyCollection<string> Permissions,
    DateTimeOffset ExpiresAt);

public sealed record CreatePaymentRequest(
    Guid ConsentId,
    string DebtorAccountId,
    string CreditorAccountNumber,
    string CreditorName,
    Money Amount,
    string RemittanceInformation);
