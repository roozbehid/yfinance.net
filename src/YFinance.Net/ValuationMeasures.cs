namespace YFinance.Net;

/// <summary>
/// Valuation measures table parsed from Yahoo Finance's key statistics page.
/// </summary>
/// <param name="Columns">Table column headers.</param>
/// <param name="Rows">Table rows.</param>
public sealed record ValuationMeasures(
    string[] Columns,
    ValuationMeasureRow[] Rows)
{
    /// <summary>
    /// Gets whether the table contains no rows or columns.
    /// </summary>
    public bool IsEmpty => Columns.Length == 0 || Rows.Length == 0;
}

/// <summary>
/// Single row in the valuation measures table.
/// </summary>
/// <param name="Metric">Metric name.</param>
/// <param name="Values">Metric values aligned with <see cref="ValuationMeasures.Columns"/>.</param>
public readonly record struct ValuationMeasureRow(
    string Metric,
    string?[] Values);