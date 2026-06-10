namespace YFinance.Net;

/// <summary>
/// Shared overview data returned for Yahoo Finance sector and industry endpoints.
/// </summary>
/// <param name="CompaniesCount">Number of companies in the domain.</param>
/// <param name="MarketCap">Aggregate market capitalization.</param>
/// <param name="MessageBoardId">Yahoo message board identifier.</param>
/// <param name="Description">Description text.</param>
/// <param name="IndustriesCount">Number of industries in the domain.</param>
/// <param name="MarketWeight">Market weight.</param>
/// <param name="EmployeeCount">Total employee count.</param>
public sealed record DomainOverview(
    int? CompaniesCount,
    decimal? MarketCap,
    string? MessageBoardId,
    string? Description,
    int? IndustriesCount,
    decimal? MarketWeight,
    int? EmployeeCount);

/// <summary>
/// Company summary row returned in a sector or industry response.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Name">Company name.</param>
/// <param name="Rating">Rating text.</param>
/// <param name="MarketWeight">Market weight.</param>
/// <param name="MarketCap">Market capitalization.</param>
/// <param name="LastPrice">Last price.</param>
/// <param name="TargetPrice">Analyst target price.</param>
/// <param name="YearToDateReturn">Year-to-date return.</param>
/// <param name="GrowthEstimate">Growth estimate.</param>
/// <param name="RegularMarketChangePercent">Regular market percentage change.</param>
public readonly record struct DomainCompany(
    string Symbol,
    string? Name,
    string? Rating,
    decimal? MarketWeight,
    decimal? MarketCap,
    decimal? LastPrice,
    decimal? TargetPrice,
    decimal? YearToDateReturn,
    decimal? GrowthEstimate,
    decimal? RegularMarketChangePercent);

/// <summary>
/// Research report metadata returned in a sector or industry response.
/// </summary>
/// <param name="Id">Report identifier.</param>
/// <param name="HeadHtml">HTML heading fragment.</param>
/// <param name="Provider">Report provider.</param>
/// <param name="TargetPrice">Analyst target price.</param>
/// <param name="TargetPriceStatus">Target price status label.</param>
/// <param name="InvestmentRating">Investment rating.</param>
/// <param name="ReportDate">Report date.</param>
/// <param name="ReportTitle">Report title.</param>
/// <param name="ReportType">Report type.</param>
public readonly record struct DomainResearchReport(
    string? Id,
    string? HeadHtml,
    string? Provider,
    decimal? TargetPrice,
    string? TargetPriceStatus,
    string? InvestmentRating,
    DateTimeOffset? ReportDate,
    string? ReportTitle,
    string? ReportType);

/// <summary>
/// Symbol reference returned in domain detail responses.
/// </summary>
/// <param name="Symbol">Symbol key.</param>
/// <param name="Name">Display name.</param>
public readonly record struct SymbolReference(
    string Symbol,
    string? Name);

/// <summary>
/// Sector or industry reference row returned in domain detail responses.
/// </summary>
/// <param name="Key">Yahoo key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Symbol">Representative symbol.</param>
/// <param name="MarketWeight">Market weight.</param>
public readonly record struct SectorIndustryReference(
    string Key,
    string? Name,
    string? Symbol,
    decimal? MarketWeight);

/// <summary>
/// Sector details returned by Yahoo Finance.
/// </summary>
/// <param name="Key">Sector key.</param>
/// <param name="Region">Region code.</param>
/// <param name="Name">Sector name.</param>
/// <param name="Symbol">Sector symbol.</param>
/// <param name="Overview">Overview summary.</param>
/// <param name="TopCompanies">Top companies in the sector.</param>
/// <param name="ResearchReports">Research reports associated with the sector.</param>
/// <param name="TopEtfs">Top ETFs associated with the sector.</param>
/// <param name="TopMutualFunds">Top mutual funds associated with the sector.</param>
/// <param name="Industries">Industries contained in the sector.</param>
public sealed record SectorDetails(
    string Key,
    string Region,
    string? Name,
    string? Symbol,
    DomainOverview? Overview,
    DomainCompany[] TopCompanies,
    DomainResearchReport[] ResearchReports,
    SymbolReference[] TopEtfs,
    SymbolReference[] TopMutualFunds,
    SectorIndustryReference[] Industries);

/// <summary>
/// Industry details returned by Yahoo Finance.
/// </summary>
/// <param name="Key">Industry key.</param>
/// <param name="Region">Region code.</param>
/// <param name="Name">Industry name.</param>
/// <param name="Symbol">Industry symbol.</param>
/// <param name="SectorKey">Parent sector key.</param>
/// <param name="SectorName">Parent sector name.</param>
/// <param name="Overview">Overview summary.</param>
/// <param name="TopCompanies">Top companies in the industry.</param>
/// <param name="ResearchReports">Research reports associated with the industry.</param>
/// <param name="TopPerformingCompanies">Top performing companies in the industry.</param>
/// <param name="TopGrowthCompanies">Top growth companies in the industry.</param>
public sealed record IndustryDetails(
    string Key,
    string Region,
    string? Name,
    string? Symbol,
    string? SectorKey,
    string? SectorName,
    DomainOverview? Overview,
    DomainCompany[] TopCompanies,
    DomainResearchReport[] ResearchReports,
    DomainCompany[] TopPerformingCompanies,
    DomainCompany[] TopGrowthCompanies);