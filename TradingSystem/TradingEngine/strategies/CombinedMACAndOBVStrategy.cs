using TradingEngine.Interfaces;
using TradingEngine.Strategies;
using TradingEngine;

public class CombinedMACAndOBVStrategy : TradingStrategy
{
    private readonly MovingAverageCrossoverStrategy _macStrategy;
    private readonly OBVStrategy _obvStrategy;

    public CombinedMACAndOBVStrategy(MarketContext marketContext, IOrderManagementService orderService, int shortTermPeriod, int longTermPeriod, decimal minCrossoverThreshold = 0.01m)
        : base(marketContext, orderService)
    {
        _macStrategy = new MovingAverageCrossoverStrategy(marketContext, orderService, shortTermPeriod, longTermPeriod, minCrossoverThreshold);
        _obvStrategy = new OBVStrategy(marketContext, orderService);
    }

    public override TradingSignal Evaluate(MarketContext context)
    {
        // Get signals from both strategies
        var macSignal = _macStrategy.Evaluate(context);
        var obvSignal = _obvStrategy.Evaluate(context);

        // Combine signals (AND Logic Example)
        if (macSignal.Action == SignalType.Buy && obvSignal.Action == SignalType.Buy)
        {
            return new TradingSignal { Action = SignalType.Buy };
        }
        else if (macSignal.Action == SignalType.Sell || obvSignal.Action == SignalType.Sell)
        {
            return new TradingSignal { Action = SignalType.Sell };
        }

        // No consensus or conflicting signals
        return new TradingSignal { Action = SignalType.Hold };
    }
}
