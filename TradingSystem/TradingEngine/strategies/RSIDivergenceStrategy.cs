using TradingEngine.Interfaces;
using TradingEngine;
using TradingEngine.Data;

public class RSIDivergenceStrategy : TradingStrategy
{
    private int _rsiPeriod;
    private int _lookbackPeriod;

    public RSIDivergenceStrategy(MarketContext marketContext, IOrderManagementService orderService, int rsiPeriod = 14, int lookbackPeriod = 5)
        : base(marketContext, orderService)
    {
        _rsiPeriod = rsiPeriod;
        _lookbackPeriod = lookbackPeriod;
    }

    public override TradingSignal Evaluate(MarketContext context)
    {
        var prices = context.HistoricalPrices;
        if (prices == null || prices.Count < _rsiPeriod + 1)
        {
            // Log insufficient data if necessary
            Console.WriteLine($"Insufficient data: {prices?.Count ?? 0} data points provided, but {_rsiPeriod + 1} required.");
            return new TradingSignal { Action = SignalType.Hold }; // Default action
        }

        decimal currentRSI;
        decimal previousRSI;


        try
        {
            // Proceed with RSI calculations
            currentRSI = CalculateRSI(prices, _rsiPeriod).Last();
            previousRSI = CalculateRSI(prices.Take(prices.Count - 1).ToList(), _rsiPeriod).Last();
        }
        catch (ArgumentException e)
        {
            return new TradingSignal { Action = SignalType.Hold };
        }

        

        // Divergence logic
        if (currentRSI > previousRSI && currentRSI > 70)
        {
            return new TradingSignal { Action = SignalType.Sell };
        }
        else if (currentRSI < previousRSI && currentRSI < 30)
        {
            return new TradingSignal { Action = SignalType.Buy };
        }

        return new TradingSignal { Action = SignalType.Hold };
    }



    public List<decimal> CalculateRSI(List<PriceData> prices, int period)
    {
        if (prices == null || prices.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data to calculate RSI. Requires at least {period + 1} data points, but received {prices?.Count ?? 0}.");
        }

        List<decimal> rsiValues = new List<decimal>();
        decimal gainSum = 0, lossSum = 0;

        // Calculate initial gains and losses for the first `period`
        for (int i = 1; i <= period; i++)
        {
            decimal change = prices[i].Close - prices[i - 1].Close;
            if (change > 0)
            {
                gainSum += change;
            }
            else
            {
                lossSum -= change;
            }
        }

        // Calculate the first RSI value
        decimal avgGain = gainSum / period;
        decimal avgLoss = lossSum / period;
        decimal rs = avgGain / (avgLoss == 0 ? 1 : avgLoss); // Avoid division by zero
        rsiValues.Add(100 - (100 / (1 + rs)));

        // Calculate subsequent RSI values using a rolling sum
        for (int i = period + 1; i < prices.Count; i++)
        {
            decimal change = prices[i].Close - prices[i - 1].Close;
            if (change > 0)
            {
                gainSum = gainSum - (gainSum / period) + change;
                lossSum = lossSum - (lossSum / period);
            }
            else
            {
                gainSum = gainSum - (gainSum / period);
                lossSum = lossSum - (lossSum / period) - change;
            }

            avgGain = gainSum / period;
            avgLoss = lossSum / period;
            rs = avgGain / (avgLoss == 0 ? 1 : avgLoss); // Avoid division by zero
            rsiValues.Add(100 - (100 / (1 + rs)));
        }

        return rsiValues;
    }




    public bool DetectBullishDivergence(List<PriceData> prices, List<decimal> rsi, int lookback)
    {
        if (prices.Count < lookback || rsi.Count < lookback)
            return false;

        var recentPrices = prices.Skip(prices.Count - lookback).ToList();
        var recentRSI = rsi.Skip(rsi.Count - lookback).ToList();

        // Check for divergence
        bool priceLowerLow = recentPrices.Last().Low < recentPrices[lookback - 1].Low;
        bool rsiHigherLow = recentRSI.Last() > recentRSI[lookback - 1];

        return priceLowerLow && rsiHigherLow;
    }

    public bool DetectBearishDivergence(List<PriceData> prices, List<decimal> rsi, int lookback)
    {
        if (prices.Count < lookback || rsi.Count < lookback)
            return false;

        var recentPrices = prices.Skip(prices.Count - lookback).ToList();
        var recentRSI = rsi.Skip(rsi.Count - lookback).ToList();

        // Check for divergence
        bool priceHigherHigh = recentPrices.Last().High > recentPrices[lookback - 1].High;
        bool rsiLowerHigh = recentRSI.Last() < recentRSI[lookback - 1];

        return priceHigherHigh && rsiLowerHigh;
    }


}
