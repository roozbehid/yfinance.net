namespace YFinance.Net;

public sealed record InsiderSnapshot(
    string Symbol,
    InsiderTransaction[] Transactions,
    InsiderRosterHolder[] RosterHolders,
    NetSharePurchaseActivity? PurchaseActivity);

public readonly record struct InsiderTransaction(
    DateOnly? StartDate,
    string Insider,
    string? Position,
    string? Url,
    string? Transaction,
    string? Text,
    long? Shares,
    decimal? Value,
    string? Ownership);

public readonly record struct InsiderRosterHolder(
    string Name,
    string? Position,
    string? Url,
    string? MostRecentTransaction,
    DateOnly? LatestTransactionDate,
    DateOnly? PositionDirectDate,
    long? SharesOwnedDirectly,
    DateOnly? PositionIndirectDate,
    long? SharesOwnedIndirectly);

public sealed record NetSharePurchaseActivity(
    string? Period,
    int? BuyTransactionCount,
    long? BuyShares,
    decimal? BuyPercentInsiderShares,
    int? SellTransactionCount,
    long? SellShares,
    decimal? SellPercentInsiderShares,
    int? NetTransactionCount,
    long? NetShares,
    decimal? NetPercentInsiderShares,
    long? TotalInsiderShares,
    long? NetInstitutionSharesBuying,
    decimal? NetInstitutionBuyingPercent);