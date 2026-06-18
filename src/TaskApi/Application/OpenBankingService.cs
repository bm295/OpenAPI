using TaskApi.Domain;

namespace TaskApi.Application;

public sealed class OpenBankingService : IOpenBankingService
{
    private readonly IOpenBankingStore _store;

    public OpenBankingService(IOpenBankingStore store)
    {
        _store = store;
    }

    public PagedResponse<Account> ListAccounts(int pageSize, string? cursor)
    {
        return Page(_store.Accounts.OrderBy(account => account.DisplayName), pageSize, cursor);
    }

    public Account? GetAccount(Guid accountId) => _store.Accounts.FirstOrDefault(account => account.Id == accountId);

    public Balance? GetBalance(Guid accountId) => _store.Balances.FirstOrDefault(balance => balance.AccountId == accountId);

    public PagedResponse<Transaction>? ListTransactions(Guid accountId, DateTimeOffset? from, DateTimeOffset? to, int pageSize, string? cursor)
    {
        if (GetAccount(accountId) is null)
        {
            return null;
        }

        var query = _store.Transactions.Where(transaction => transaction.AccountId == accountId);

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
        var now = DateTimeOffset.UtcNow;
        var consent = new Consent(
            Guid.NewGuid(),
            request.CustomerId.Trim(),
            request.Permissions.Select(permission => permission.Trim()).Where(permission => permission.Length > 0).Distinct().ToArray(),
            ConsentStatus.AwaitingAuthorization,
            request.ExpiresAt,
            now);

        _store.Consents[consent.Id] = consent;
        return consent;
    }

    public Consent? GetConsent(Guid consentId) => _store.Consents.TryGetValue(consentId, out var consent) ? consent : null;

    public Consent? RevokeConsent(Guid consentId)
    {
        if (!_store.Consents.TryGetValue(consentId, out var existing))
        {
            return null;
        }

        var revoked = existing with { Status = ConsentStatus.Revoked };
        _store.Consents[consentId] = revoked;
        return revoked;
    }

    public PaymentInstruction? CreatePayment(CreatePaymentRequest request)
    {
        if (!_store.Consents.TryGetValue(request.ConsentId, out var consent) || consent.Status is ConsentStatus.Revoked or ConsentStatus.Expired)
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
            DateTimeOffset.UtcNow);

        _store.Payments[payment.Id] = payment;
        return payment;
    }

    public PaymentInstruction? GetPayment(Guid paymentId) => _store.Payments.TryGetValue(paymentId, out var payment) ? payment : null;

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
