using System.Collections.Generic;

namespace TradingSystem.Core
{
    public class MomentumStrategy : StrategyBase
    {
        public MomentumStrategy(MarketDataService marketDataService, OrderService orderService, RiskManagementService riskManagementService)
            : base(marketDataService, orderService, riskManagementService)
        {
        }

        protected override IEnumerable<string> GetSubscribedSymbols()
        {
            // Define the symbols this strategy is interested in.
            return new List<string> { "AAPL", "GOOGL", "MSFT" };
        }

        protected override void OnMarketDataReceived(MarketData data)
        {
            // Example logic: Print received data.
            System.Console.WriteLine($"MomentumStrategy received data for {data.Symbol}: LastPrice = {data.LastPrice}");

            // Add strategy-specific logic here (e.g., generate buy/sell signals).
        }

        protected override void InitializeResources()
        {
            // Example: Initialize any required resources (e.g., historical data cache).
            System.Console.WriteLine("MomentumStrategy initializing resources...");
        }

        protected override void ReleaseResources()
        {
            // Example: Release any allocated resources.
            System.Console.WriteLine("MomentumStrategy releasing resources...");
        }
    }
}
