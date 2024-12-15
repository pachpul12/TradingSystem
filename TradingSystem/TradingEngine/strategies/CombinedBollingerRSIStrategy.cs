using System;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Strategies
{
    public class CombinedBollingerRSIStrategy : TradingStrategy
    {
        private readonly BollingerBandsStrategy _bollingerBandsStrategy;
        private readonly RSIDivergenceStrategy _rsiDivergenceStrategy;

        public CombinedBollingerRSIStrategy(
            MarketContext marketContext,
            IOrderManagementService orderService,
            BollingerBandsStrategy bollingerBandsStrategy,
            RSIDivergenceStrategy rsiDivergenceStrategy)
            : base(marketContext, orderService)
        {
            _bollingerBandsStrategy = bollingerBandsStrategy;
            _rsiDivergenceStrategy = rsiDivergenceStrategy;
        }

        public override TradingSignal Evaluate(MarketContext context)
        {
            var bollingerSignal = _bollingerBandsStrategy.Evaluate(context);
            var rsiSignal = _rsiDivergenceStrategy.Evaluate(context);

            // Combine signals
            if (bollingerSignal.Action == SignalType.Buy && rsiSignal.Action == SignalType.Buy)
            {
                return new TradingSignal { Action = SignalType.Buy };
            }
            else if (bollingerSignal.Action == SignalType.Sell && rsiSignal.Action == SignalType.Sell)
            {
                return new TradingSignal { Action = SignalType.Sell };
            }

            return new TradingSignal { Action = SignalType.Hold };
        }
    }
}
