using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TradingEngine.Data;
using TradingEngine.Strategies;

namespace TradingEngine.TradingStrategies
{
    public class MovingAverageCrossoverStrategy : TradingStrategy
    {
        private readonly int shortWindow;
        private readonly int longWindow;
        private readonly Queue<decimal> shortMovingAverageQueue;
        private readonly Queue<decimal> longMovingAverageQueue;

        public event Action<int, string> SignalGenerated; // Event for buy/sell signals

        public MovingAverageCrossoverStrategy(int stockId, int shortWindow = 5, int longWindow = 20)
            : base("Moving Average Crossover", stockId)
        {
            this.shortWindow = shortWindow;
            this.longWindow = longWindow;
            shortMovingAverageQueue = new Queue<decimal>();
            longMovingAverageQueue = new Queue<decimal>();
        }

        public override void Initialize()
        {
            Console.WriteLine($"Initializing {Name} for StockId: {StockId}");
            // Load historical data or any other initialization logic
        }

        public override void OnPriceDataReceived(PriceData priceData)
        {
            // Update moving average queues
            UpdateMovingAverage(shortMovingAverageQueue, priceData.Close, shortWindow);
            UpdateMovingAverage(longMovingAverageQueue, priceData.Close, longWindow);

            if (shortMovingAverageQueue.Count == shortWindow && longMovingAverageQueue.Count == longWindow)
            {
                var shortMA = shortMovingAverageQueue.Average();
                var longMA = longMovingAverageQueue.Average();

                // Generate buy/sell signals
                if (shortMA > longMA)
                {
                    SignalGenerated?.Invoke(StockId, "BUY");
                    Console.WriteLine($"BUY signal generated for StockId: {StockId}");
                }
                else if (shortMA < longMA)
                {
                    SignalGenerated?.Invoke(StockId, "SELL");
                    Console.WriteLine($"SELL signal generated for StockId: {StockId}");
                }
            }
        }

        private void UpdateMovingAverage(Queue<decimal> queue, decimal newValue, int window)
        {
            queue.Enqueue(newValue);
            if (queue.Count > window)
            {
                queue.Dequeue();
            }
        }

        public override void Cleanup()
        {
            Console.WriteLine($"Cleaning up {Name} for StockId: {StockId}");
        }
    }
}
