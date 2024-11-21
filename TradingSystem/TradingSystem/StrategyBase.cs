using System;
using System.Collections.Generic;

namespace TradingSystem.Core
{
    public abstract class StrategyBase
    {
        protected readonly MarketDataService MarketDataService;
        protected readonly OrderService OrderService;
        protected readonly RiskManagementService RiskManagementService;
        private readonly List<string> _subscribedSymbols;

        protected StrategyBase(MarketDataService marketDataService, OrderService orderService, RiskManagementService riskManagementService)
        {
            MarketDataService = marketDataService;
            OrderService = orderService;
            RiskManagementService = riskManagementService;
            _subscribedSymbols = new List<string>();
            
        }

        protected bool IsPositionOpen(string symbol)
        {
            // Check if the symbol exists in the open positions dictionary
            return _openPositions.ContainsKey(symbol);
        }

        protected double GetLastPrice(string symbol)
        {
            // Assuming MarketDataService maintains the latest prices
            return MarketDataService.GetLastPrice(symbol);
        }

        protected readonly Dictionary<string, Position> _openPositions = new();

        /// <summary>
        /// Starts the strategy by subscribing to required market data and initializing resources.
        /// </summary>
        public virtual void Start()
        {
            System.Console.WriteLine($"Starting strategy: {GetType().Name}");
            SubscribeToMarketData();
            InitializeResources();
            System.Console.WriteLine($"Strategy {GetType().Name} started.");
        }

        /// <summary>
        /// Stops the strategy by unsubscribing from market data and releasing resources.
        /// </summary>
        public virtual void Stop()
        {
            System.Console.WriteLine($"Stopping strategy: {GetType().Name}");
            UnsubscribeFromMarketData();
            ReleaseResources();
            System.Console.WriteLine($"Strategy {GetType().Name} stopped.");
        }

        /// <summary>
        /// Subscribes to required market data for the strategy.
        /// Override this method to define specific symbols for the strategy.
        /// </summary>
        protected virtual void SubscribeToMarketData()
        {
            foreach (var symbol in GetSubscribedSymbols())
            {
                _subscribedSymbols.Add(symbol);
                MarketDataService.Subscribe(symbol, OnMarketDataReceived);
                System.Console.WriteLine($"Strategy {GetType().Name} subscribed to {symbol}");
            }
        }

        /// <summary>
        /// Unsubscribes from all market data subscriptions.
        /// </summary>
        protected virtual void UnsubscribeFromMarketData()
        {
            foreach (var symbol in _subscribedSymbols)
            {
                MarketDataService.Unsubscribe(symbol);
                System.Console.WriteLine($"Strategy {GetType().Name} unsubscribed from {symbol}");
            }

            _subscribedSymbols.Clear();
        }

        /// <summary>
        /// Initializes any resources needed by the strategy.
        /// Override this method for strategy-specific initialization.
        /// </summary>
        protected virtual void InitializeResources()
        {
            // Default implementation: No initialization.
        }

        /// <summary>
        /// Releases any resources used by the strategy.
        /// Override this method for strategy-specific cleanup.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            // Default implementation: No cleanup.
        }

        /// <summary>
        /// Define the symbols the strategy subscribes to.
        /// Override this method to specify the symbols for the strategy.
        /// </summary>
        /// <returns>A list of symbols to subscribe to.</returns>
        protected abstract IEnumerable<string> GetSubscribedSymbols();

        /// <summary>
        /// Handles market data received for subscribed symbols.
        /// Override this method to define strategy behavior upon receiving data.
        /// </summary>
        /// <param name="data">The market data received.</param>
        protected abstract void OnMarketDataReceived(MarketData data);


    }
}
