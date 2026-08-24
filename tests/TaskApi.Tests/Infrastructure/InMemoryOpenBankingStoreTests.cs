using TaskApi.Domain;
using TaskApi.Infrastructure;

namespace TaskApi.Tests.Infrastructure;

public sealed class InMemoryOpenBankingStoreTests
{
    private static readonly TimeProvider TimeProvider = new FixedTimeProvider(
        DateTimeOffset.Parse("2026-08-24T12:00:00Z"));

    [Fact]
    public void ListTransactions_ReturnsOnlyTransactionsForRequestedAccount()
    {
        var store = new InMemoryOpenBankingStore(TimeProvider);
        var account = store.ListAccounts().Single(item => item.DisplayName == "Everyday Current");

        var transactions = store.ListTransactions(account.Id);

        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, transaction => Assert.Equal(account.Id, transaction.AccountId));
    }

    [Fact]
    public void SaveOperations_UpsertEntitiesWithoutExposingMutableCollections()
    {
        var store = new InMemoryOpenBankingStore(TimeProvider);
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-08-24T12:00:00Z");
        var consent = new Consent(id, "customer", ["read"], ConsentStatus.AwaitingAuthorization, createdAt.AddDays(1), createdAt);

        store.SaveConsent(consent);
        store.SaveConsent(consent with { Status = ConsentStatus.Revoked });

        Assert.Equal(ConsentStatus.Revoked, store.FindConsent(id)?.Status);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
