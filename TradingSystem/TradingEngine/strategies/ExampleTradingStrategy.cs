using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class ExampleTradingStrategy : TradingStrategy
    {
        public ExampleTradingStrategy(MarketContext marketContext, IOrderManagementService orderManagementService) : base(marketContext, orderManagementService)
        {
            
        }
        public override TradingSignal Evaluate(MarketContext context)
        {
            // Simplified strategy: Buy if price increased steadily
            bool isUptrend = context.HistoricalPrices.All(p => p.Close <= context.CurrentPrice);
            if (isUptrend)
            {
                //return new TradingSignal(SignalType.Buy, "Strong uptrend detected.");
                return new TradingSignal { Action = SignalType.Buy };
            }

            return new TradingSignal { Action = SignalType.Hold };
        }
    }
}