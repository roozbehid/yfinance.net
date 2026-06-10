namespace YFinance.Net;

/// <summary>
/// Insider data returned by Yahoo Finance for a symbol.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Transactions">Insider transactions.</param>
/// <param name="RosterHolders">Insider roster holders.</param>
/// <param name="PurchaseActivity">Net share purchase activity summary.</param>
public sealed record InsiderSnapshot(
    string Symbol,
    InsiderTransaction[] Transactions,
    InsiderRosterHolder[] RosterHolders,
    NetSharePurchaseActivity? PurchaseActivity);

/// <summary>
/// Insider transaction row returned by Yahoo Finance.
/// </summary>
/// <param name="StartDate">Transaction start date.</param>
/// <param name="Insider">Insider name.</param>
/// <param name="Position">Insider position.</param>
/// <param name="Url">Source URL.</param>
/// <param name="Transaction">Transaction type.</param>
/// <param name="Text">Descriptive transaction text.</param>
/// <param name="Shares">Number of shares.</param>
/// <param name="Value">Transaction value.</param>
/// <param name="Ownership">Ownership label.</param>
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

/// <summary>
/// Insider roster holder row returned by Yahoo Finance.
/// </summary>
/// <param name="Name">Insider name.</param>
/// <param name="Position">Insider position.</param>
/// <param name="Url">Source URL.</param>
/// <param name="MostRecentTransaction">Most recent transaction description.</param>
/// <param name="LatestTransactionDate">Date of the latest transaction.</param>
/// <param name="PositionDirectDate">Date direct ownership was reported.</param>
/// <param name="SharesOwnedDirectly">Shares owned directly.</param>
/// <param name="PositionIndirectDate">Date indirect ownership was reported.</param>
/// <param name="SharesOwnedIndirectly">Shares owned indirectly.</param>
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

/// <summary>
/// Net insider share purchase activity summary returned by Yahoo Finance.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="BuyTransactionCount">Number of buy transactions.</param>
/// <param name="BuyShares">Shares bought.</param>
/// <param name="BuyPercentInsiderShares">Percentage of insider shares bought.</param>
/// <param name="SellTransactionCount">Number of sell transactions.</param>
/// <param name="SellShares">Shares sold.</param>
/// <param name="SellPercentInsiderShares">Percentage of insider shares sold.</param>
/// <param name="NetTransactionCount">Net transaction count.</param>
/// <param name="NetShares">Net shares bought or sold.</param>
/// <param name="NetPercentInsiderShares">Net percentage of insider shares bought or sold.</param>
/// <param name="TotalInsiderShares">Total insider shares.</param>
/// <param name="NetInstitutionSharesBuying">Net institution shares buying.</param>
/// <param name="NetInstitutionBuyingPercent">Net institution buying percentage.</param>
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