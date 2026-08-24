using TaskApi.Application.Contracts;
using TaskApi.Application.Services;
using TaskApi.Domain;
using TaskApi.Infrastructure;

namespace TaskApi.Tests.Application.Services;

public sealed class OpenBankingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateConsent_NormalizesInputAndPersistsIt()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var store = new InMemoryOpenBankingStore(timeProvider);
        var service = new OpenBankingService(store, timeProvider);

        var created = service.CreateConsent(new CreateConsentRequest(
            "  customer-1  ",
            [" accounts:read ", "accounts:read", " ", "payments:create"],
            Now.AddDays(1)));

        Assert.Equal("customer-1", created.CustomerId);
        Assert.Equal(["accounts:read", "payments:create"], created.Permissions);
        Assert.Equal(ConsentStatus.AwaitingAuthorization, created.Status);
        Assert.Equal(Now, created.CreatedAt);
        Assert.Equal(created, service.GetConsent(created.Id));
    }

    [Fact]
    public void RevokeConsent_ReplacesPersistedConsentAndPreventsPayment()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var store = new InMemoryOpenBankingStore(timeProvider);
        var service = new OpenBankingService(store, timeProvider);
        var consent = service.CreateConsent(new CreateConsentRequest(
            "customer-1", ["payments:create"], Now.AddDays(1)));

        var revoked = service.RevokeConsent(consent.Id);
        var payment = service.CreatePayment(PaymentRequest(consent.Id));

        Assert.NotNull(revoked);
        Assert.Equal(ConsentStatus.Revoked, revoked.Status);
        Assert.Equal(revoked, service.GetConsent(consent.Id));
        Assert.Null(payment);
    }

    [Fact]
    public void CreatePayment_NormalizesInputUsesClockAndPersistsIt()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var store = new InMemoryOpenBankingStore(timeProvider);
        var service = new OpenBankingService(store, timeProvider);
        var consent = service.CreateConsent(new CreateConsentRequest(
            "customer-1", ["payments:create"], Now.AddDays(1)));

        var payment = service.CreatePayment(PaymentRequest(consent.Id));

        Assert.NotNull(payment);
        Assert.Equal("debtor-1", payment.DebtorAccountId);
        Assert.Equal("GB123", payment.CreditorAccountNumber);
        Assert.Equal("A Merchant", payment.CreditorName);
        Assert.Equal("Invoice 42", payment.RemittanceInformation);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(Now, payment.CreatedAt);
        Assert.Equal(payment, service.GetPayment(payment.Id));
    }

    [Fact]
    public void ListAccounts_OrdersAndPaginatesSeedData()
    {
        var timeProvider = new FixedTimeProvider(Now);
        var service = new OpenBankingService(new InMemoryOpenBankingStore(timeProvider), timeProvider);

        var firstPage = service.ListAccounts(1, null);
        var secondPage = service.ListAccounts(1, firstPage.NextCursor);

        Assert.Equal("Everyday Current", Assert.Single(firstPage.Data).DisplayName);
        Assert.Equal("1", firstPage.NextCursor);
        Assert.Equal("Rainy Day Savings", Assert.Single(secondPage.Data).DisplayName);
        Assert.Null(secondPage.NextCursor);
    }

    private static CreatePaymentRequest PaymentRequest(Guid consentId) => new(
        consentId,
        " debtor-1 ",
        " GB123 ",
        " A Merchant ",
        new Money(12.50m, "USD"),
        " Invoice 42 ");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
