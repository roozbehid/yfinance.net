namespace YFinance.Net;

/// <summary>
/// Balance sheet returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Frequency">Requested statement frequency.</param>
/// <param name="Periods">Reporting periods in the statement.</param>
/// <param name="LineItems">Statement line items aligned with <see cref="Periods"/>.</param>
public sealed record BalanceSheet(
    string Symbol,
    FinancialStatementFrequency Frequency,
    FinancialStatementPeriod[] Periods,
    BalanceSheetLineItem[] LineItems);

/// <summary>
/// Single balance sheet line item.
/// </summary>
/// <param name="Key">Yahoo Finance field key.</param>
/// <param name="CurrencyCode">Currency code when available.</param>
/// <param name="Values">Values aligned with the statement periods.</param>
public readonly record struct BalanceSheetLineItem(
    string Key,
    string? CurrencyCode,
    decimal?[] Values);

internal static class BalanceSheetKeys
{
    public static string[] All { get; } =
    [
        "TreasurySharesNumber", "PreferredSharesNumber", "OrdinarySharesNumber", "ShareIssued", "NetDebt",
        "TotalDebt", "TangibleBookValue", "InvestedCapital", "WorkingCapital", "NetTangibleAssets",
        "CapitalLeaseObligations", "CommonStockEquity", "PreferredStockEquity", "TotalCapitalization",
        "TotalEquityGrossMinorityInterest", "MinorityInterest", "StockholdersEquity",
        "OtherEquityInterest", "GainsLossesNotAffectingRetainedEarnings", "OtherEquityAdjustments",
        "FixedAssetsRevaluationReserve", "ForeignCurrencyTranslationAdjustments",
        "MinimumPensionLiabilities", "UnrealizedGainLoss", "TreasuryStock", "RetainedEarnings",
        "AdditionalPaidInCapital", "CapitalStock", "OtherCapitalStock", "CommonStock", "PreferredStock",
        "TotalPartnershipCapital", "GeneralPartnershipCapital", "LimitedPartnershipCapital",
        "TotalLiabilitiesNetMinorityInterest", "TotalNonCurrentLiabilitiesNetMinorityInterest",
        "OtherNonCurrentLiabilities", "LiabilitiesHeldforSaleNonCurrent", "RestrictedCommonStock",
        "PreferredSecuritiesOutsideStockEquity", "DerivativeProductLiabilities", "EmployeeBenefits",
        "NonCurrentPensionAndOtherPostretirementBenefitPlans", "NonCurrentAccruedExpenses",
        "DuetoRelatedPartiesNonCurrent", "TradeandOtherPayablesNonCurrent",
        "NonCurrentDeferredLiabilities", "NonCurrentDeferredRevenue",
        "NonCurrentDeferredTaxesLiabilities", "LongTermDebtAndCapitalLeaseObligation",
        "LongTermCapitalLeaseObligation", "LongTermDebt", "LongTermProvisions", "CurrentLiabilities",
        "OtherCurrentLiabilities", "CurrentDeferredLiabilities", "CurrentDeferredRevenue",
        "CurrentDeferredTaxesLiabilities", "CurrentDebtAndCapitalLeaseObligation",
        "CurrentCapitalLeaseObligation", "CurrentDebt", "OtherCurrentBorrowings", "LineOfCredit",
        "CommercialPaper", "CurrentNotesPayable", "PensionandOtherPostRetirementBenefitPlansCurrent",
        "CurrentProvisions", "PayablesAndAccruedExpenses", "CurrentAccruedExpenses", "InterestPayable",
        "Payables", "OtherPayable", "DuetoRelatedPartiesCurrent", "DividendsPayable", "TotalTaxPayable",
        "IncomeTaxPayable", "AccountsPayable", "TotalAssets", "TotalNonCurrentAssets",
        "OtherNonCurrentAssets", "DefinedPensionBenefit", "NonCurrentPrepaidAssets",
        "NonCurrentDeferredAssets", "NonCurrentDeferredTaxesAssets", "DuefromRelatedPartiesNonCurrent",
        "NonCurrentNoteReceivables", "NonCurrentAccountsReceivable", "FinancialAssets",
        "InvestmentsAndAdvances", "OtherInvestments", "InvestmentinFinancialAssets",
        "HeldToMaturitySecurities", "AvailableForSaleSecurities",
        "FinancialAssetsDesignatedasFairValueThroughProfitorLossTotal", "TradingSecurities",
        "LongTermEquityInvestment", "InvestmentsinJointVenturesatCost",
        "InvestmentsInOtherVenturesUnderEquityMethod", "InvestmentsinAssociatesatCost",
        "InvestmentsinSubsidiariesatCost", "InvestmentProperties", "GoodwillAndOtherIntangibleAssets",
        "OtherIntangibleAssets", "Goodwill", "NetPPE", "AccumulatedDepreciation", "GrossPPE", "Leases",
        "ConstructionInProgress", "OtherProperties", "MachineryFurnitureEquipment",
        "BuildingsAndImprovements", "LandAndImprovements", "Properties", "CurrentAssets",
        "OtherCurrentAssets", "HedgingAssetsCurrent", "AssetsHeldForSaleCurrent", "CurrentDeferredAssets",
        "CurrentDeferredTaxesAssets", "RestrictedCash", "PrepaidAssets", "Inventory",
        "InventoriesAdjustmentsAllowances", "OtherInventories", "FinishedGoods", "WorkInProcess",
        "RawMaterials", "Receivables", "ReceivablesAdjustmentsAllowances", "OtherReceivables",
        "DuefromRelatedPartiesCurrent", "TaxesReceivable", "AccruedInterestReceivable", "NotesReceivable",
        "LoansReceivable", "AccountsReceivable", "AllowanceForDoubtfulAccountsReceivable",
        "GrossAccountsReceivable", "CashCashEquivalentsAndShortTermInvestments",
        "OtherShortTermInvestments", "CashAndCashEquivalents", "CashEquivalents", "CashFinancial",
        "CashCashEquivalentsAndFederalFundsSold"
    ];
}