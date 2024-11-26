using System;
using TradingEngine.Interfaces;
using TradingEngine;

namespace TradingEngine.Interfaces
{
    public abstract class TradingStrategy
    {
        protected readonly MarketContext MarketContext;
        protected readonly IOrderManagementService OrderManagementService;

        /// <summary>
        /// Initializes a new instance of the TradingStrategy class.
        /// </summary>
        /// <param name="marketContext">The market context for accessing market data.</param>
        /// <param name="orderManagementService">The service for managing orders.</param>
        protected TradingStrategy(MarketContext marketContext, IOrderManagementService orderManagementService)
        {
            MarketContext = marketContext ?? throw new ArgumentNullException(nameof(marketContext));
            OrderManagementService = orderManagementService ?? throw new ArgumentNullException(nameof(orderManagementService));
        }

        /// <summary>
        /// Evaluates the current market conditions and decides on trading actions.
        /// </summary>
        public abstract TradingSignal Evaluate(MarketContext marketContext);

        /// <summary>
        /// Logs the strategy's decision for debugging and analysis.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void Log(string message)
        {
            Console.WriteLine($"[{GetType().Name}] {message}");
        }
    }
}
