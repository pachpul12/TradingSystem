using System;
using System.Collections.Generic;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class OBVStrategy : TradingStrategy
    {
        private readonly int _lookbackPeriod;

        public OBVStrategy(MarketContext marketContext, IOrderManagementService orderService, int lookbackPeriod = 14)
            : base(marketContext, orderService)
        {
            _lookbackPeriod = lookbackPeriod;
        }

        public override TradingSignal Evaluate(MarketContext context)
        {
            var prices = context.HistoricalPrices;

            if (prices.Count < _lookbackPeriod + 1)
            {
                return new TradingSignal { Action = SignalType.Hold }; // Not enough data
            }

            // Calculate OBV
            var obvData = CalculateOBV(prices);

            // Check for OBV divergence
            var latestOBV = obvData[obvData.Count - 1];
            var previousOBV = obvData[obvData.Count - 1 - _lookbackPeriod];

            var latestPrice = prices[prices.Count - 1].Close;
            var previousPrice = prices[prices.Count - 1 - _lookbackPeriod].Close;

            if (latestOBV > previousOBV && latestPrice > previousPrice)
            {
                // Volume supports price uptrend
                return new TradingSignal { Action = SignalType.Buy };
            }
            else if (latestOBV < previousOBV && latestPrice < previousPrice)
            {
                // Volume supports price downtrend
                return new TradingSignal { Action = SignalType.Sell };
            }

            // No strong signal
            return new TradingSignal { Action = SignalType.Hold };
        }

        private List<decimal> CalculateOBV(List<PriceData> prices)
        {
            var obv = new List<decimal> { 0 }; // Initialize OBV with zero
            for (int i = 1; i < prices.Count; i++)
            {
                if (prices[i].Close > prices[i - 1].Close)
                {
                    obv.Add(obv[i - 1] + prices[i].Volume);
                }
                else if (prices[i].Close < prices[i - 1].Close)
                {
                    obv.Add(obv[i - 1] - prices[i].Volume);
                }
                else
                {
                    obv.Add(obv[i - 1]); // No change in OBV if price is flat
                }
            }

            return obv;
        }
    }
}
