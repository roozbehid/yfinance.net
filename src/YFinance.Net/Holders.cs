namespace YFinance.Net;

/// <summary>
/// Holder data returned by Yahoo Finance for a symbol.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="MajorHolders">Major holders breakdown summary.</param>
/// <param name="InstitutionalHolders">Institutional holder positions.</param>
/// <param name="MutualFundHolders">Mutual fund holder positions.</param>
public sealed record HoldersSnapshot(
    string Symbol,
    MajorHoldersBreakdown? MajorHolders,
    HolderPosition[] InstitutionalHolders,
    HolderPosition[] MutualFundHolders);

/// <summary>
/// Summary percentages for major holders.
/// </summary>
/// <param name="InsidersPercentHeld">Percent of shares held by insiders.</param>
/// <param name="InstitutionsPercentHeld">Percent of shares held by institutions.</param>
/// <param name="InstitutionsFloatPercentHeld">Percent of float held by institutions.</param>
/// <param name="InstitutionsCount">Number of reporting institutions.</param>
public sealed record MajorHoldersBreakdown(
    decimal? InsidersPercentHeld,
    decimal? InstitutionsPercentHeld,
    decimal? InstitutionsFloatPercentHeld,
    int? InstitutionsCount);

/// <summary>
/// Holder position row returned by Yahoo Finance.
/// </summary>
/// <param name="ReportDate">Reporting date.</param>
/// <param name="Holder">Holder name.</param>
/// <param name="PercentHeld">Percent held.</param>
/// <param name="Shares">Number of shares held.</param>
/// <param name="Value">Position value.</param>
/// <param name="PercentChange">Percentage change in position.</param>
public readonly record struct HolderPosition(
    DateOnly? ReportDate,
    string Holder,
    decimal? PercentHeld,
    long? Shares,
    decimal? Value,
    decimal? PercentChange);