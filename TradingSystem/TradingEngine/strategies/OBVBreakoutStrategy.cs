using System;
using System.Collections.Generic;
using System.Linq;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class OBVBreakoutStrategy : TradingStrategy
    {
        private readonly int _obvLookbackPeriod;
        private decimal _breakoutThreshold;
        private decimal _obvHigh;
        private decimal _obvLow;
        private decimal _lastOBV;

        public OBVBreakoutStrategy(
            MarketContext marketContext,
            IOrderManagementService orderService,
            int obvLookbackPeriod,
            decimal breakoutThreshold)
            : base(marketContext, orderService)
        {
            _obvLookbackPeriod = obvLookbackPeriod;
            _breakoutThreshold = breakoutThreshold;
            _obvHigh = decimal.MinValue;
            _obvLow = decimal.MaxValue;
            _lastOBV = 0;
        }

        private decimal CalculateOBVIncrement(decimal currentClose, decimal previousClose, decimal volume)
        {
            if (currentClose > previousClose) return volume;
            if (currentClose < previousClose) return -volume;
            return 0; // No change in OBV if prices are equal
        }

        public override TradingSignal Evaluate(MarketContext context)
        {
            var prices = context.HistoricalPrices;
            if (prices == null || prices.Count < _obvLookbackPeriod)
                return new TradingSignal { Action = SignalType.Hold };

            // Calculate OBV incrementally
            var latestPrice = prices.Last();
            var previousPrice = prices[prices.Count - 2];
            _lastOBV += CalculateOBVIncrement(latestPrice.Close, previousPrice.Close, latestPrice.Volume);

            // Update OBV range over the lookback period
            var recentPrices = prices.Skip(Math.Max(0, prices.Count - _obvLookbackPeriod)).ToList();
            _obvHigh = Math.Max(_obvHigh, _lastOBV);
            _obvLow = Math.Min(_obvLow, _lastOBV);

            // Determine breakout levels
            decimal upperBreakoutLevel = _obvHigh * (1 + _breakoutThreshold);
            decimal lowerBreakoutLevel = _obvLow * (1 - _breakoutThreshold);

            // Generate signals based on breakout levels
            if (_lastOBV > upperBreakoutLevel)
            {
                Console.WriteLine($"OBV breakout detected above {upperBreakoutLevel}. Generating Buy signal.");
                return new TradingSignal { Action = SignalType.Buy };
            }
            else if (_lastOBV < lowerBreakoutLevel)
            {
                Console.WriteLine($"OBV breakout detected below {lowerBreakoutLevel}. Generating Sell signal.");
                return new TradingSignal { Action = SignalType.Sell };
            }

            // No breakout, hold position
            return new TradingSignal { Action = SignalType.Hold };
        }
    }
}
