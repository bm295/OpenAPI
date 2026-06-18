namespace TaskApi.Domain;

public enum AccountType
{
    Current,
    Savings,
    CreditCard
}

public enum AccountStatus
{
    Enabled,
    Disabled
}

public enum ConsentStatus
{
    AwaitingAuthorization,
    Authorized,
    Revoked,
    Expired
}

public enum PaymentStatus
{
    Pending,
    Accepted,
    Rejected
}

public sealed record Money(decimal Amount, string Currency);

public sealed record Account(
    Guid Id,
    string BankId,
    string AccountNumberMasked,
    string DisplayName,
    AccountType Type,
    AccountStatus Status,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record Balance(
    Guid AccountId,
    Money Available,
    Money Current,
    DateTimeOffset AsOf);

public sealed record Transaction(
    Guid Id,
    Guid AccountId,
    DateTimeOffset BookedAt,
    string Description,
    string MerchantName,
    Money Amount,
    string Category,
    string Reference);

public sealed record Consent(
    Guid Id,
    string CustomerId,
    IReadOnlyCollection<string> Permissions,
    ConsentStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record PaymentInstruction(
    Guid Id,
    Guid ConsentId,
    string DebtorAccountId,
    string CreditorAccountNumber,
    string CreditorName,
    Money Amount,
    string RemittanceInformation,
    PaymentStatus Status,
    DateTimeOffset CreatedAt);
