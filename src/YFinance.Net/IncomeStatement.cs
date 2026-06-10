namespace YFinance.Net;

/// <summary>
/// Frequencies supported by Yahoo Finance financial statement endpoints.
/// </summary>
public enum FinancialStatementFrequency
{
    /// <summary>
    /// Annual statements.
    /// </summary>
    Annual,
    /// <summary>
    /// Quarterly statements.
    /// </summary>
    Quarterly,
    /// <summary>
    /// Trailing twelve month statements.
    /// </summary>
    Trailing
}

/// <summary>
/// Income statement returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Frequency">Requested statement frequency.</param>
/// <param name="Periods">Reporting periods in the statement.</param>
/// <param name="LineItems">Statement line items aligned with <see cref="Periods"/>.</param>
public sealed record IncomeStatement(
    string Symbol,
    FinancialStatementFrequency Frequency,
    FinancialStatementPeriod[] Periods,
    IncomeStatementLineItem[] LineItems);

/// <summary>
/// Reporting period descriptor for financial statements.
/// </summary>
/// <param name="AsOfDate">As-of date for the period.</param>
/// <param name="PeriodType">Period type string returned by Yahoo Finance.</param>
public readonly record struct FinancialStatementPeriod(
    DateOnly AsOfDate,
    string? PeriodType);

/// <summary>
/// Single financial statement line item.
/// </summary>
/// <param name="Key">Yahoo Finance field key.</param>
/// <param name="CurrencyCode">Currency code when available.</param>
/// <param name="Values">Values aligned with the statement periods.</param>
public readonly record struct IncomeStatementLineItem(
    string Key,
    string? CurrencyCode,
    decimal?[] Values);

internal static class IncomeStatementKeys
{
    public static string[] All { get; } =
    [
        "TaxEffectOfUnusualItems", "TaxRateForCalcs", "NormalizedEBITDA", "NormalizedDilutedEPS",
        "NormalizedBasicEPS", "TotalUnusualItems", "TotalUnusualItemsExcludingGoodwill",
        "NetIncomeFromContinuingOperationNetMinorityInterest", "ReconciledDepreciation",
        "ReconciledCostOfRevenue", "EBITDA", "EBIT", "NetInterestIncome", "InterestExpense",
        "InterestIncome", "ContinuingAndDiscontinuedDilutedEPS", "ContinuingAndDiscontinuedBasicEPS",
        "NormalizedIncome", "NetIncomeFromContinuingAndDiscontinuedOperation", "TotalExpenses",
        "RentExpenseSupplemental", "ReportedNormalizedDilutedEPS", "ReportedNormalizedBasicEPS",
        "TotalOperatingIncomeAsReported", "DividendPerShare", "DilutedAverageShares", "BasicAverageShares",
        "DilutedEPS", "DilutedEPSOtherGainsLosses", "TaxLossCarryforwardDilutedEPS",
        "DilutedAccountingChange", "DilutedExtraordinary", "DilutedDiscontinuousOperations",
        "DilutedContinuousOperations", "BasicEPS", "BasicEPSOtherGainsLosses", "TaxLossCarryforwardBasicEPS",
        "BasicAccountingChange", "BasicExtraordinary", "BasicDiscontinuousOperations",
        "BasicContinuousOperations", "DilutedNIAvailtoComStockholders", "AverageDilutionEarnings",
        "NetIncomeCommonStockholders", "OtherunderPreferredStockDividend", "PreferredStockDividends",
        "NetIncome", "MinorityInterests", "NetIncomeIncludingNoncontrollingInterests",
        "NetIncomeFromTaxLossCarryforward", "NetIncomeExtraordinary", "NetIncomeDiscontinuousOperations",
        "NetIncomeContinuousOperations", "EarningsFromEquityInterestNetOfTax", "TaxProvision",
        "PretaxIncome", "OtherIncomeExpense", "OtherNonOperatingIncomeExpenses", "SpecialIncomeCharges",
        "GainOnSaleOfPPE", "GainOnSaleOfBusiness", "OtherSpecialCharges", "WriteOff",
        "ImpairmentOfCapitalAssets", "RestructuringAndMergernAcquisition", "SecuritiesAmortization",
        "EarningsFromEquityInterest", "GainOnSaleOfSecurity", "NetNonOperatingInterestIncomeExpense",
        "TotalOtherFinanceCost", "InterestExpenseNonOperating", "InterestIncomeNonOperating",
        "OperatingIncome", "OperatingExpense", "OtherOperatingExpenses", "OtherTaxes",
        "ProvisionForDoubtfulAccounts", "DepreciationAmortizationDepletionIncomeStatement",
        "DepletionIncomeStatement", "DepreciationAndAmortizationInIncomeStatement", "Amortization",
        "AmortizationOfIntangiblesIncomeStatement", "DepreciationIncomeStatement", "ResearchAndDevelopment",
        "SellingGeneralAndAdministration", "SellingAndMarketingExpense", "GeneralAndAdministrativeExpense",
        "OtherGandA", "InsuranceAndClaims", "RentAndLandingFees", "SalariesAndWages", "GrossProfit",
        "CostOfRevenue", "TotalRevenue", "ExciseTaxes", "OperatingRevenue", "LossAdjustmentExpense",
        "NetPolicyholderBenefitsAndClaims", "PolicyholderBenefitsGross", "PolicyholderBenefitsCeded",
        "OccupancyAndEquipment", "ProfessionalExpenseAndContractServicesExpense", "OtherNonInterestExpense"
    ];
}

internal static class FinancialStatementFrequencyExtensions
{
    public static string ToWirePrefix(this FinancialStatementFrequency frequency)
    {
        return frequency switch
        {
            FinancialStatementFrequency.Annual => "annual",
            FinancialStatementFrequency.Quarterly => "quarterly",
            FinancialStatementFrequency.Trailing => "trailing",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
    }
}