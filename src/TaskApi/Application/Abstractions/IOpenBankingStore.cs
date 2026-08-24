using TaskApi.Domain;

namespace TaskApi.Application.Abstractions;

public interface IOpenBankingStore
{
    IReadOnlyCollection<Account> ListAccounts();

    Account? FindAccount(Guid accountId);

    Balance? FindBalance(Guid accountId);

    IReadOnlyCollection<Transaction> ListTransactions(Guid accountId);

    Consent? FindConsent(Guid consentId);

    void SaveConsent(Consent consent);

    PaymentInstruction? FindPayment(Guid paymentId);

    void SavePayment(PaymentInstruction payment);
}
