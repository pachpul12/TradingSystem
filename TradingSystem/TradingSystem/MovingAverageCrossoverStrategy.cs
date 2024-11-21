using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBApi;
using TradingSystem.Core;

namespace TradingSystem
{
    public class MovingAverageCrossoverStrategy : StrategyBase
    {
        public MovingAverageCrossoverStrategy(MarketDataService marketDataService, OrderService orderService, RiskManagementService riskManagementService)
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

            double shortMA = 0;//CalculateMovingAverage(data.Symbol, period: 20);
            double longMA = 0;//CalculateMovingAverage(data.Symbol, period: 50);

            if (shortMA > longMA && !IsPositionOpen(data.Symbol))
            {
                PlaceBuyOrder(data.Symbol, quantity: 100);
            }
            else if (shortMA < longMA && IsPositionOpen(data.Symbol))
            {
                PlaceSellOrder(data.Symbol, quantity: 100);
            }
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

        protected void PlaceBuyOrder(string symbol, int quantity)
        {
            if (_openPositions.ContainsKey(symbol))
            {
                System.Console.WriteLine($"Buy order for {symbol} rejected: Position already exists.");
                return;
            }

            var orderWrapper = new OrderWrapper
            {
                Symbol = symbol,
                Quantity = quantity,
                Side = "BUY"
            };

            var contract = orderWrapper.ToContract();
            var order = orderWrapper.ToOrder();

            OrderService.PlaceOrder(contract, order);

            // Add position to the open positions dictionary
            _openPositions[symbol] = new Position
            {
                Symbol = symbol,
                Quantity = quantity,
                EntryPrice = GetLastPrice(symbol),
                EntryTime = DateTime.UtcNow,
                Side = "Buy"
            };

            System.Console.WriteLine($"Buy order placed for {quantity} shares of {symbol}");
        }

        protected void PlaceSellOrder(string symbol, int quantity)
        {
            if (!_openPositions.ContainsKey(symbol))
            {
                System.Console.WriteLine($"Sell order for {symbol} rejected: No open position exists.");
                return;
            }

            var existingPosition = _openPositions[symbol];
            if (existingPosition.Side != "Buy")
            {
                System.Console.WriteLine($"Sell order for {symbol} rejected: Position is not a Buy.");
                return;
            }

            if (quantity > existingPosition.Quantity)
            {
                System.Console.WriteLine($"Sell order for {symbol} rejected: Insufficient quantity.");
                return;
            }

            var orderWrapper = new OrderWrapper
            {
                Symbol = symbol,
                Quantity = quantity,
                Side = "SELL"
            };

            var contract = orderWrapper.ToContract();
            var order = orderWrapper.ToOrder();

            OrderService.PlaceOrder(contract, order);

            // Update or remove the position
            existingPosition.Quantity -= quantity;
            if (existingPosition.Quantity == 0)
            {
                _openPositions.Remove(symbol);
            }

            System.Console.WriteLine($"Sell order placed for {quantity} shares of {symbol}");
        }




    }


}
