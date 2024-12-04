using System;
using System.Collections.Generic;
using System.Linq;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class OBVStrategy : TradingStrategy
    {
        private readonly int _confirmationPeriod = 20;
        private int _consistentUpTrendCount = 0;
        private int _consistentDownTrendCount = 0;
        private decimal _lastOBV = 0;

        public OBVStrategy(MarketContext marketContext, IOrderManagementService orderService)
            : base(marketContext, orderService) { }

        private decimal CalculateOBVIncrement(decimal currentClose, decimal previousClose, decimal volume)
        {
            if (currentClose > previousClose) return volume;
            if (currentClose < previousClose) return -volume;
            return 0; // No change in OBV if prices are equal
        }

        public override TradingSignal Evaluate(MarketContext context)
        {
            var prices = context.HistoricalPrices;
            if (prices == null || prices.Count < _confirmationPeriod)
                return new TradingSignal { Action = SignalType.Hold };

            // Calculate current OBV incrementally
            var latestPrice = prices.Last();
            var previousPrice = prices[prices.Count - 2]; // Second-to-last price
            _lastOBV += CalculateOBVIncrement(latestPrice.Close, previousPrice.Close, latestPrice.Volume);

            // Check OBV trend and adjust counters
            if (_lastOBV > 0)
            {
                _consistentUpTrendCount++;
                _consistentDownTrendCount = 0; // Reset opposite trend counter

                if (_consistentUpTrendCount >= _confirmationPeriod)
                {
                    _consistentUpTrendCount = 0; // Reset after confirming the signal
                    return new TradingSignal { Action = SignalType.Buy };
                }
            }
            else if (_lastOBV < 0)
            {
                _consistentDownTrendCount++;
                _consistentUpTrendCount = 0; // Reset opposite trend counter

                if (_consistentDownTrendCount >= _confirmationPeriod)
                {
                    _consistentDownTrendCount = 0; // Reset after confirming the signal
                    return new TradingSignal { Action = SignalType.Sell };
                }
            }
            else
            {
                // Reset both counters if no trend
                _consistentUpTrendCount = 0;
                _consistentDownTrendCount = 0;
            }

            return new TradingSignal { Action = SignalType.Hold };
        }
    }
}
