namespace YFinance.Net;

public sealed record DomainOverview(
    int? CompaniesCount,
    decimal? MarketCap,
    string? MessageBoardId,
    string? Description,
    int? IndustriesCount,
    decimal? MarketWeight,
    int? EmployeeCount);

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

public readonly record struct SymbolReference(
    string Symbol,
    string? Name);

public readonly record struct SectorIndustryReference(
    string Key,
    string? Name,
    string? Symbol,
    decimal? MarketWeight);

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