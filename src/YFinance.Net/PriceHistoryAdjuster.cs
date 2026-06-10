namespace YFinance.Net;

internal static class PriceHistoryAdjuster
{
    public static PriceHistoryResult Apply(PriceHistoryResult history, PriceAdjustmentMode mode)
    {
        if (mode == PriceAdjustmentMode.None)
        {
            return history;
        }

        var adjustedBars = new PriceBar[history.Bars.Length];
        for (var index = 0; index < history.Bars.Length; index++)
        {
            adjustedBars[index] = Adjust(history.Bars[index], mode);
        }

        return history with { Bars = adjustedBars };
    }

    private static PriceBar Adjust(PriceBar bar, PriceAdjustmentMode mode)
    {
        if (bar.Close is null || bar.AdjustedClose is null || bar.Close == 0)
        {
            return bar;
        }

        var ratio = decimal.Divide(bar.AdjustedClose.Value, bar.Close.Value);
        var adjustedOpen = Multiply(bar.Open, ratio);
        var adjustedHigh = Multiply(bar.High, ratio);
        var adjustedLow = Multiply(bar.Low, ratio);

        return mode switch
        {
            PriceAdjustmentMode.AdjustAll => bar with
            {
                Open = adjustedOpen,
                High = adjustedHigh,
                Low = adjustedLow,
                Close = bar.AdjustedClose
            },
            PriceAdjustmentMode.AdjustOpenHighLow => bar with
            {
                Open = adjustedOpen,
                High = adjustedHigh,
                Low = adjustedLow
            },
            _ => bar
        };
    }

    private static decimal? Multiply(decimal? value, decimal ratio)
    {
        return value is null ? null : value.Value * ratio;
    }
}