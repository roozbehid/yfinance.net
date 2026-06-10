namespace YFinance.Net;

public sealed record ValuationMeasures(
    string[] Columns,
    ValuationMeasureRow[] Rows)
{
    public bool IsEmpty => Columns.Length == 0 || Rows.Length == 0;
}

public readonly record struct ValuationMeasureRow(
    string Metric,
    string?[] Values);