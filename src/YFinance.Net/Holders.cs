namespace YFinance.Net;

public sealed record HoldersSnapshot(
    string Symbol,
    MajorHoldersBreakdown? MajorHolders,
    HolderPosition[] InstitutionalHolders,
    HolderPosition[] MutualFundHolders);

public sealed record MajorHoldersBreakdown(
    decimal? InsidersPercentHeld,
    decimal? InstitutionsPercentHeld,
    decimal? InstitutionsFloatPercentHeld,
    int? InstitutionsCount);

public readonly record struct HolderPosition(
    DateOnly? ReportDate,
    string Holder,
    decimal? PercentHeld,
    long? Shares,
    decimal? Value,
    decimal? PercentChange);