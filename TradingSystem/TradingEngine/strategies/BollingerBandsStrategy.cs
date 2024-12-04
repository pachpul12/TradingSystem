using System.Linq;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine;

public class BollingerBandsStrategy : TradingStrategy
{
    private readonly int _period;
    private readonly decimal _multiplier;

    public BollingerBandsStrategy(MarketContext marketContext, IOrderManagementService orderService, int period = 20, decimal multiplier = 2.0m)
        : base(marketContext, orderService)
    {
        _period = period;
        _multiplier = multiplier;
    }

    public override TradingSignal Evaluate(MarketContext context)
    {
        var prices = context.HistoricalPrices;
        if (prices.Count < _period)
            return new TradingSignal { Action = SignalType.Hold };

        decimal sma = CalculateSMA(prices, _period);
        decimal stdDev = CalculateStandardDeviation(prices, _period);
        decimal upperBand = sma + (_multiplier * stdDev);
        decimal lowerBand = sma - (_multiplier * stdDev);

        decimal currentPrice = prices.Last().Close;

        if (currentPrice <= lowerBand)
        {
            return new TradingSignal { Action = SignalType.Buy };
        }
        else if (currentPrice >= upperBand)
        {
            return new TradingSignal { Action = SignalType.Sell };
        }

        return new TradingSignal { Action = SignalType.Hold };
    }

    private decimal CalculateSMA(List<PriceData> prices, int period)
    {
        return prices.Skip(prices.Count - period).Average(p => p.Close);
    }

    private decimal CalculateStandardDeviation(List<PriceData> prices, int period)
    {
        var recentPrices = prices.Skip(prices.Count - period).Select(p => p.Close).ToList();
        decimal sma = recentPrices.Average();
        decimal variance = recentPrices.Sum(p => (p - sma) * (p - sma)) / period;
        return (decimal)Math.Sqrt((double)variance);
    }
}
