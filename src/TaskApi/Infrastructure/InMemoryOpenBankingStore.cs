using TaskApi.Application.Abstractions;
using TaskApi.Domain;

namespace TaskApi.Infrastructure;

public sealed class InMemoryOpenBankingStore : IOpenBankingStore
{
    private static readonly Guid PrimaryAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SavingsAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public InMemoryOpenBankingStore()
    {
        var now = DateTimeOffset.UtcNow;

        Accounts = new[]
        {
            new Account(PrimaryAccountId, "bank-demo", "****1234", "Everyday Current", AccountType.Current, AccountStatus.Enabled, "USD", now.AddMonths(-18)),
            new Account(SavingsAccountId, "bank-demo", "****9876", "Rainy Day Savings", AccountType.Savings, AccountStatus.Enabled, "USD", now.AddMonths(-10))
        };

        Balances = new[]
        {
            new Balance(PrimaryAccountId, new Money(2575.42m, "USD"), new Money(2631.10m, "USD"), now),
            new Balance(SavingsAccountId, new Money(12880.00m, "USD"), new Money(12880.00m, "USD"), now)
        };

        Transactions = new[]
        {
            new Transaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), PrimaryAccountId, now.AddDays(-1), "Grocery purchase", "Neighborhood Market", new Money(-84.16m, "USD"), "Groceries", "POS-10001"),
            new Transaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), PrimaryAccountId, now.AddDays(-3), "Payroll deposit", "Contoso Payroll", new Money(2250.00m, "USD"), "Income", "ACH-90001"),
            new Transaction(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), SavingsAccountId, now.AddDays(-7), "Monthly savings transfer", "Internal Transfer", new Money(500.00m, "USD"), "Transfer", "TRF-70001")
        };
    }

    public IReadOnlyCollection<Account> Accounts { get; }

    public IReadOnlyCollection<Balance> Balances { get; }

    public IReadOnlyCollection<Transaction> Transactions { get; }

    public IDictionary<Guid, Consent> Consents { get; } = new Dictionary<Guid, Consent>();

    public IDictionary<Guid, PaymentInstruction> Payments { get; } = new Dictionary<Guid, PaymentInstruction>();
}
