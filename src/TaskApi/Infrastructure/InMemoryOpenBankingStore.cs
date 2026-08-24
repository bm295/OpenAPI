using System.Collections.Concurrent;
using TaskApi.Application.Abstractions;
using TaskApi.Domain;

namespace TaskApi.Infrastructure;

public sealed class InMemoryOpenBankingStore : IOpenBankingStore
{
    private static readonly Guid PrimaryAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SavingsAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public InMemoryOpenBankingStore(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        _accounts = Array.AsReadOnly(new[]
        {
            new Account(PrimaryAccountId, "bank-demo", "****1234", "Everyday Current", AccountType.Current, AccountStatus.Enabled, "USD", now.AddMonths(-18)),
            new Account(SavingsAccountId, "bank-demo", "****9876", "Rainy Day Savings", AccountType.Savings, AccountStatus.Enabled, "USD", now.AddMonths(-10))
        });

        _balances = Array.AsReadOnly(new[]
        {
            new Balance(PrimaryAccountId, new Money(2575.42m, "USD"), new Money(2631.10m, "USD"), now),
            new Balance(SavingsAccountId, new Money(12880.00m, "USD"), new Money(12880.00m, "USD"), now)
        });

        _transactions = Array.AsReadOnly(new[]
        {
            new Transaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), PrimaryAccountId, now.AddDays(-1), "Grocery purchase", "Neighborhood Market", new Money(-84.16m, "USD"), "Groceries", "POS-10001"),
            new Transaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), PrimaryAccountId, now.AddDays(-3), "Payroll deposit", "Contoso Payroll", new Money(2250.00m, "USD"), "Income", "ACH-90001"),
            new Transaction(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), SavingsAccountId, now.AddDays(-7), "Monthly savings transfer", "Internal Transfer", new Money(500.00m, "USD"), "Transfer", "TRF-70001")
        });
    }

    private readonly IReadOnlyCollection<Account> _accounts;
    private readonly IReadOnlyCollection<Balance> _balances;
    private readonly IReadOnlyCollection<Transaction> _transactions;
    private readonly ConcurrentDictionary<Guid, Consent> _consents = new();
    private readonly ConcurrentDictionary<Guid, PaymentInstruction> _payments = new();

    public IReadOnlyCollection<Account> ListAccounts() => _accounts;

    public Account? FindAccount(Guid accountId) => _accounts.FirstOrDefault(account => account.Id == accountId);

    public Balance? FindBalance(Guid accountId) => _balances.FirstOrDefault(balance => balance.AccountId == accountId);

    public IReadOnlyCollection<Transaction> ListTransactions(Guid accountId) =>
        _transactions.Where(transaction => transaction.AccountId == accountId).ToArray();

    public Consent? FindConsent(Guid consentId) => _consents.GetValueOrDefault(consentId);

    public void SaveConsent(Consent consent) => _consents[consent.Id] = consent;

    public PaymentInstruction? FindPayment(Guid paymentId) => _payments.GetValueOrDefault(paymentId);

    public void SavePayment(PaymentInstruction payment) => _payments[payment.Id] = payment;
}
