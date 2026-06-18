using TaskApi.Domain;

namespace TaskApi.Application;

public interface IOpenBankingStore
{
    IReadOnlyCollection<Account> Accounts { get; }

    IReadOnlyCollection<Balance> Balances { get; }

    IReadOnlyCollection<Transaction> Transactions { get; }

    IDictionary<Guid, Consent> Consents { get; }

    IDictionary<Guid, PaymentInstruction> Payments { get; }
}
