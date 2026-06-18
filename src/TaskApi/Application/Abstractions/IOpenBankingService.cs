using TaskApi.Application.Contracts;
using TaskApi.Domain;

namespace TaskApi.Application.Abstractions;

public interface IOpenBankingService
{
    PagedResponse<Account> ListAccounts(int pageSize, string? cursor);

    Account? GetAccount(Guid accountId);

    Balance? GetBalance(Guid accountId);

    PagedResponse<Transaction>? ListTransactions(Guid accountId, DateTimeOffset? from, DateTimeOffset? to, int pageSize, string? cursor);

    Consent CreateConsent(CreateConsentRequest request);

    Consent? GetConsent(Guid consentId);

    Consent? RevokeConsent(Guid consentId);

    PaymentInstruction? CreatePayment(CreatePaymentRequest request);

    PaymentInstruction? GetPayment(Guid paymentId);
}
