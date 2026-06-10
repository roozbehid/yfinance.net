namespace YFinance.Net;

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

public sealed record BatchCompanyProfileRequest(
    IReadOnlyList<string> Symbols,
    int MaxConcurrency = 4);

public sealed record BatchCompanyProfileResult(
    IReadOnlyDictionary<string, CompanyProfile> Profiles,
    IReadOnlyDictionary<string, BatchCompanyProfileFailure> Failures);

public sealed record BatchCompanyProfileFailure(
    string Symbol,
    string Message);

public readonly record struct CompanyOfficer(
    string? Name,
    string? Title,
    int? Age,
    int? YearBorn,
    decimal? TotalPay);