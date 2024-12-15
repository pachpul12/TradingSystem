using TradingEngine.Interfaces;
using TradingEngine.Strategies;
using TradingEngine;

public class CombinedRSIBollingerOBVStrategy : TradingStrategy
{
    private readonly BollingerBandsStrategy _bollingerStrategy;
    private readonly RSIDivergenceStrategy _rsiStrategy;
    private readonly VWAPStrategy _obvStrategy;

    public CombinedRSIBollingerOBVStrategy(
        MarketContext marketContext,
        IOrderManagementService orderService,
        BollingerBandsStrategy bollingerBandsStrategy,
            RSIDivergenceStrategy rsiDivergenceStrategy,
            VWAPStrategy obvStrategy)
        : base(marketContext, orderService)
    {
        _bollingerStrategy = bollingerBandsStrategy;
        _rsiStrategy = rsiDivergenceStrategy;
        _obvStrategy = obvStrategy;
    }

    public override TradingSignal Evaluate(MarketContext context)
    {
        int buyScore = 0, sellScore = 0;

        // Evaluate signals from individual strategies
        var bollingerSignal = _bollingerStrategy.Evaluate(context);
        var rsiSignal = _rsiStrategy.Evaluate(context);
        var obvSignal = _obvStrategy.Evaluate(context);

        // Assign weights to the buy and sell scores based on individual strategy signals
        if (bollingerSignal.Action == SignalType.Buy) buyScore += 50;
        if (bollingerSignal.Action == SignalType.Sell) sellScore += 50;

        if (rsiSignal.Action == SignalType.Buy) buyScore += 30;
        if (rsiSignal.Action == SignalType.Sell) sellScore += 30;

        if (obvSignal.Action == SignalType.Buy) buyScore += 20;
        if (obvSignal.Action == SignalType.Sell) sellScore += 20;

        // Decide the final action based on the scores
        if (buyScore >= 70)
            return new TradingSignal { Action = SignalType.Buy };

        if (sellScore >= 70)
            return new TradingSignal { Action = SignalType.Sell };

        return new TradingSignal { Action = SignalType.Hold };
    }

}
