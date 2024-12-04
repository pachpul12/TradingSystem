using System;
using System.Collections.Generic;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class MovingAverageCrossoverStrategy : TradingStrategy
    {
        private readonly int _shortTermPeriod;
        private readonly int _longTermPeriod;
        private readonly decimal _minCrossoverThreshold;

        public MovingAverageCrossoverStrategy(MarketContext marketContext, IOrderManagementService orderService, int shortTermPeriod, int longTermPeriod, decimal minCrossoverThreshold)
            : base(marketContext, orderService)
        {
            _shortTermPeriod = shortTermPeriod;
            _longTermPeriod = longTermPeriod;
            _minCrossoverThreshold = minCrossoverThreshold;
        }

        public override TradingSignal Evaluate(MarketContext context)
        {
            // Retrieve historical price data from context
            List<PriceData> prices = context.HistoricalPrices;

            if (prices == null || prices.Count < _longTermPeriod)
            {
                return new TradingSignal { Action = SignalType.Hold}; // Not enough data for calculation
            }

            // Calculate moving averages
            decimal shortTermMA = CalculateMovingAverage(prices, _shortTermPeriod);
            decimal longTermMA = CalculateMovingAverage(prices, _longTermPeriod);

            // Check for crossover
            if (shortTermMA > longTermMA + _minCrossoverThreshold)
            {
                Console.WriteLine("Buy Signal: Short-term MA crossed above Long-term MA.");
                return new TradingSignal { Action = SignalType.Buy };
            }
            else if (shortTermMA < longTermMA - _minCrossoverThreshold)
            {
                Console.WriteLine("Sell Signal: Short-term MA crossed below Long-term MA.");
                return new TradingSignal { Action = SignalType.Sell };
            }

            // No signal
            return new TradingSignal { Action = SignalType.Hold };
        }

        private decimal CalculateMovingAverage(List<PriceData> prices, int period)
        {
            if (prices.Count < period)
                throw new ArgumentException("Not enough data points to calculate moving average.");

            decimal sum = 0;
            for (int i = prices.Count - period; i < prices.Count; i++)
            {
                sum += prices[i].Close;
            }

            return sum / period;
        }

    }
}
