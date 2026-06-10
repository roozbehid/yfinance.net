namespace YFinance.Net;

public sealed record CashFlow(
    string Symbol,
    FinancialStatementFrequency Frequency,
    FinancialStatementPeriod[] Periods,
    CashFlowLineItem[] LineItems);

public readonly record struct CashFlowLineItem(
    string Key,
    string? CurrencyCode,
    decimal?[] Values);

internal static class CashFlowKeys
{
    public static string[] All { get; } =
    [
        "ForeignSales", "DomesticSales", "AdjustedGeographySegmentData", "FreeCashFlow",
        "RepurchaseOfCapitalStock", "RepaymentOfDebt", "IssuanceOfDebt", "IssuanceOfCapitalStock",
        "CapitalExpenditure", "InterestPaidSupplementalData", "IncomeTaxPaidSupplementalData",
        "EndCashPosition", "OtherCashAdjustmentOutsideChangeinCash", "BeginningCashPosition",
        "EffectOfExchangeRateChanges", "ChangesInCash", "OtherCashAdjustmentInsideChangeinCash",
        "CashFlowFromDiscontinuedOperation", "FinancingCashFlow", "CashFromDiscontinuedFinancingActivities",
        "CashFlowFromContinuingFinancingActivities", "NetOtherFinancingCharges", "InterestPaidCFF",
        "ProceedsFromStockOptionExercised", "CashDividendsPaid", "PreferredStockDividendPaid",
        "CommonStockDividendPaid", "NetPreferredStockIssuance", "PreferredStockPayments",
        "PreferredStockIssuance", "NetCommonStockIssuance", "CommonStockPayments", "CommonStockIssuance",
        "NetIssuancePaymentsOfDebt", "NetShortTermDebtIssuance", "ShortTermDebtPayments",
        "ShortTermDebtIssuance", "NetLongTermDebtIssuance", "LongTermDebtPayments", "LongTermDebtIssuance",
        "InvestingCashFlow", "CashFromDiscontinuedInvestingActivities",
        "CashFlowFromContinuingInvestingActivities", "NetOtherInvestingChanges", "InterestReceivedCFI",
        "DividendsReceivedCFI", "NetInvestmentPurchaseAndSale", "SaleOfInvestment", "PurchaseOfInvestment",
        "NetInvestmentPropertiesPurchaseAndSale", "SaleOfInvestmentProperties",
        "PurchaseOfInvestmentProperties", "NetBusinessPurchaseAndSale", "SaleOfBusiness",
        "PurchaseOfBusiness", "NetIntangiblesPurchaseAndSale", "SaleOfIntangibles", "PurchaseOfIntangibles",
        "NetPPEPurchaseAndSale", "SaleOfPPE", "PurchaseOfPPE", "CapitalExpenditureReported",
        "OperatingCashFlow", "CashFromDiscontinuedOperatingActivities",
        "CashFlowFromContinuingOperatingActivities", "TaxesRefundPaid", "InterestReceivedCFO",
        "InterestPaidCFO", "DividendReceivedCFO", "DividendPaidCFO", "ChangeInWorkingCapital",
        "ChangeInOtherWorkingCapital", "ChangeInOtherCurrentLiabilities", "ChangeInOtherCurrentAssets",
        "ChangeInPayablesAndAccruedExpense", "ChangeInAccruedExpense", "ChangeInInterestPayable",
        "ChangeInPayable", "ChangeInDividendPayable", "ChangeInAccountPayable", "ChangeInTaxPayable",
        "ChangeInIncomeTaxPayable", "ChangeInPrepaidAssets", "ChangeInInventory", "ChangeInReceivables",
        "ChangesInAccountReceivables", "OtherNonCashItems", "ExcessTaxBenefitFromStockBasedCompensation",
        "StockBasedCompensation", "UnrealizedGainLossOnInvestmentSecurities", "ProvisionandWriteOffofAssets",
        "AssetImpairmentCharge", "AmortizationOfSecurities", "DeferredTax", "DeferredIncomeTax",
        "DepreciationAmortizationDepletion", "Depletion", "DepreciationAndAmortization",
        "AmortizationCashFlow", "AmortizationOfIntangibles", "Depreciation", "OperatingGainsLosses",
        "PensionAndEmployeeBenefitExpense", "EarningsLossesFromEquityInvestments",
        "GainLossOnInvestmentSecurities", "NetForeignCurrencyExchangeGainLoss", "GainLossOnSaleOfPPE",
        "GainLossOnSaleOfBusiness", "NetIncomeFromContinuingOperations",
        "CashFlowsfromusedinOperatingActivitiesDirect", "TaxesRefundPaidDirect", "InterestReceivedDirect",
        "InterestPaidDirect", "DividendsReceivedDirect", "DividendsPaidDirect", "ClassesofCashPayments",
        "OtherCashPaymentsfromOperatingActivities", "PaymentsonBehalfofEmployees",
        "PaymentstoSuppliersforGoodsandServices", "ClassesofCashReceiptsfromOperatingActivities",
        "OtherCashReceiptsfromOperatingActivities", "ReceiptsfromGovernmentGrants", "ReceiptsfromCustomers"
    ];
}