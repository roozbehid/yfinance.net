namespace YFinance.Net;

/// <summary>
/// Company profile and key statistics returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="ShortName">Short display name.</param>
/// <param name="LongName">Long display name.</param>
/// <param name="QuoteType">Quote type.</param>
/// <param name="Exchange">Exchange code or name.</param>
/// <param name="Currency">Reporting currency.</param>
/// <param name="MarketCap">Market capitalization.</param>
/// <param name="EnterpriseValue">Enterprise value.</param>
/// <param name="TrailingPe">Trailing price-to-earnings ratio.</param>
/// <param name="ForwardPe">Forward price-to-earnings ratio.</param>
/// <param name="DividendYield">Dividend yield.</param>
/// <param name="Beta">Beta coefficient.</param>
/// <param name="CurrentPrice">Current price.</param>
/// <param name="EarningsGrowth">Earnings growth rate.</param>
/// <param name="RevenueGrowth">Revenue growth rate.</param>
/// <param name="ProfitMargins">Profit margin.</param>
/// <param name="GrossMargins">Gross margin.</param>
/// <param name="OperatingMargins">Operating margin.</param>
/// <param name="ReturnOnAssets">Return on assets.</param>
/// <param name="ReturnOnEquity">Return on equity.</param>
/// <param name="Sector">Sector name.</param>
/// <param name="Industry">Industry name.</param>
/// <param name="Website">Company website.</param>
/// <param name="InvestorRelationsWebsite">Investor relations website.</param>
/// <param name="Phone">Company phone number.</param>
/// <param name="AddressLine1">Primary address line.</param>
/// <param name="City">City.</param>
/// <param name="State">State or province.</param>
/// <param name="PostalCode">Postal code.</param>
/// <param name="Country">Country.</param>
/// <param name="LongBusinessSummary">Long business summary text.</param>
/// <param name="FullTimeEmployees">Full-time employee count.</param>
/// <param name="LogoUrl">Logo URL.</param>
/// <param name="Officers">Company officers returned by Yahoo Finance.</param>
public sealed record CompanyProfile(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? QuoteType,
    string? Exchange,
    string? Currency,
    decimal? MarketCap,
    decimal? EnterpriseValue,
    decimal? TrailingPe,
    decimal? ForwardPe,
    decimal? DividendYield,
    decimal? Beta,
    decimal? CurrentPrice,
    decimal? EarningsGrowth,
    decimal? RevenueGrowth,
    decimal? ProfitMargins,
    decimal? GrossMargins,
    decimal? OperatingMargins,
    decimal? ReturnOnAssets,
    decimal? ReturnOnEquity,
    string? Sector,
    string? Industry,
    string? Website,
    string? InvestorRelationsWebsite,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? LongBusinessSummary,
    int? FullTimeEmployees,
    string? LogoUrl,
    CompanyOfficer[] Officers);

/// <summary>
/// Batch request for company profiles.
/// </summary>
/// <param name="Symbols">Symbols to query.</param>
/// <param name="MaxConcurrency">Maximum number of concurrent symbol requests.</param>
public sealed record BatchCompanyProfileRequest(
    IReadOnlyList<string> Symbols,
    int MaxConcurrency = 4);

/// <summary>
/// Batch company profile results keyed by symbol.
/// </summary>
/// <param name="Profiles">Successful profiles keyed by symbol.</param>
/// <param name="Failures">Failed symbol requests keyed by symbol.</param>
public sealed record BatchCompanyProfileResult(
    IReadOnlyDictionary<string, CompanyProfile> Profiles,
    IReadOnlyDictionary<string, BatchCompanyProfileFailure> Failures);

/// <summary>
/// Describes a failed company profile request in a batch operation.
/// </summary>
/// <param name="Symbol">Symbol that failed.</param>
/// <param name="Message">Failure message captured for the symbol.</param>
public sealed record BatchCompanyProfileFailure(
    string Symbol,
    string Message);

/// <summary>
/// Company officer metadata returned by Yahoo Finance.
/// </summary>
/// <param name="Name">Officer name.</param>
/// <param name="Title">Officer title.</param>
/// <param name="Age">Officer age.</param>
/// <param name="YearBorn">Officer birth year.</param>
/// <param name="TotalPay">Total pay when available.</param>
public readonly record struct CompanyOfficer(
    string? Name,
    string? Title,
    int? Age,
    int? YearBorn,
    decimal? TotalPay);