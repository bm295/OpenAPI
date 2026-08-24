using TaskApi.Application.Abstractions;
using TaskApi.Application.Contracts;
using TaskApi.Domain;

namespace TaskApi.Application.Services;

public sealed class OpenBankingService : IOpenBankingService
{
    private readonly IOpenBankingStore _store;
    private readonly TimeProvider _timeProvider;

    public OpenBankingService(IOpenBankingStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public PagedResponse<Account> ListAccounts(int pageSize, string? cursor)
    {
        return Page(_store.ListAccounts().OrderBy(account => account.DisplayName), pageSize, cursor);
    }

    public Account? GetAccount(Guid accountId) => _store.FindAccount(accountId);

    public Balance? GetBalance(Guid accountId) => _store.FindBalance(accountId);

    public PagedResponse<Transaction>? ListTransactions(Guid accountId, DateTimeOffset? from, DateTimeOffset? to, int pageSize, string? cursor)
    {
        if (GetAccount(accountId) is null)
        {
            return null;
        }

        var query = _store.ListTransactions(accountId).AsEnumerable();

        if (from is not null)
        {
            query = query.Where(transaction => transaction.BookedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(transaction => transaction.BookedAt <= to.Value);
        }

        return Page(query.OrderByDescending(transaction => transaction.BookedAt), pageSize, cursor);
    }

    public Consent CreateConsent(CreateConsentRequest request)
    {
        var now = _timeProvider.GetUtcNow();
        var consent = new Consent(
            Guid.NewGuid(),
            request.CustomerId.Trim(),
            request.Permissions.Select(permission => permission.Trim()).Where(permission => permission.Length > 0).Distinct().ToArray(),
            ConsentStatus.AwaitingAuthorization,
            request.ExpiresAt,
            now);

        _store.SaveConsent(consent);
        return consent;
    }

    public Consent? GetConsent(Guid consentId) => _store.FindConsent(consentId);

    public Consent? RevokeConsent(Guid consentId)
    {
        var existing = _store.FindConsent(consentId);
        if (existing is null)
        {
            return null;
        }

        var revoked = existing with { Status = ConsentStatus.Revoked };
        _store.SaveConsent(revoked);
        return revoked;
    }

    public PaymentInstruction? CreatePayment(CreatePaymentRequest request)
    {
        var consent = _store.FindConsent(request.ConsentId);
        if (consent is null || consent.Status is ConsentStatus.Revoked or ConsentStatus.Expired)
        {
            return null;
        }

        var payment = new PaymentInstruction(
            Guid.NewGuid(),
            request.ConsentId,
            request.DebtorAccountId.Trim(),
            request.CreditorAccountNumber.Trim(),
            request.CreditorName.Trim(),
            request.Amount,
            request.RemittanceInformation.Trim(),
            PaymentStatus.Pending,
            _timeProvider.GetUtcNow());

        _store.SavePayment(payment);
        return payment;
    }

    public PaymentInstruction? GetPayment(Guid paymentId) => _store.FindPayment(paymentId);

    private static PagedResponse<T> Page<T>(IEnumerable<T> source, int pageSize, string? cursor)
    {
        var ordered = source.ToList();
        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && int.TryParse(cursor, out var parsedCursor))
        {
            startIndex = Math.Max(parsedCursor, 0);
        }

        var page = ordered.Skip(startIndex).Take(pageSize).ToList();
        var nextCursor = startIndex + page.Count < ordered.Count ? (startIndex + page.Count).ToString() : null;
        return new PagedResponse<T>(page, nextCursor);
    }
}
