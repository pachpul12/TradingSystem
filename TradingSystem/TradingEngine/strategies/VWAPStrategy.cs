using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine;

public class VWAPStrategy : TradingStrategy
{
    private readonly int _lookbackWindow; // e.g., 10 minutes
    private readonly decimal _confirmationThreshold; // e.g., 0.01m for 1%

    public VWAPStrategy(MarketContext marketContext, IOrderManagementService orderService, int lookbackWindow, decimal confirmationThreshold)
        : base(marketContext, orderService)
    {
        _lookbackWindow = lookbackWindow;
        _confirmationThreshold = confirmationThreshold;
    }

    private decimal CalculateVWAP(List<PriceData> prices)
    {
        if (prices.Count < _lookbackWindow)
            throw new ArgumentException("Not enough data to calculate VWAP.");

        decimal cumulativeVolumePrice = 0;
        decimal cumulativeVolume = 0;

        for (int i = prices.Count - _lookbackWindow; i < prices.Count; i++)
        {
            cumulativeVolumePrice += prices[i].Close * prices[i].Volume;
            cumulativeVolume += prices[i].Volume;
        }

        if (cumulativeVolume == 0)
        {
            return 0;
        }

        return cumulativeVolumePrice / cumulativeVolume;
    }

    public override TradingSignal Evaluate(MarketContext context)
    {
        var prices = context.HistoricalPrices;

        if (prices.Count < _lookbackWindow)
            return new TradingSignal { Action = SignalType.Hold };

        decimal vwap = CalculateVWAP(prices);
        decimal latestPrice = prices.Last().Close;

        if (vwap == 0)
        {
            return new TradingSignal { Action = SignalType.Hold };
        }

        if (Math.Abs(latestPrice - vwap) / vwap <= _confirmationThreshold)
        {
            if (latestPrice > vwap) return new TradingSignal { Action = SignalType.Buy };
            else if (latestPrice < vwap) return new TradingSignal { Action = SignalType.Sell };
        }

        return new TradingSignal { Action = SignalType.Hold };
    }
}
