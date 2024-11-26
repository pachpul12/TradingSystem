using System;
using TradingEngine.Data;

namespace TradingEngine.Strategies
{
    public abstract class TradingStrategy
    {
        public string Name { get; protected set; }
        public int StockId { get; protected set; }

        protected TradingStrategy(string name, int stockId)
        {
            Name = name;
            StockId = stockId;
        }

        /// <summary>
        /// Called to initialize the strategy with any required setup.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Executes the strategy logic on a new price data point.
        /// </summary>
        /// <param name="priceData">The latest price data.</param>
        public abstract void OnPriceDataReceived(PriceData priceData);

        /// <summary>
        /// Cleans up the strategy when no longer needed.
        /// </summary>
        public abstract void Cleanup();
    }
}
