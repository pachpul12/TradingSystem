using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace TradingSystem.Core
{
    public class MarketDataService
    {
        private readonly ConcurrentDictionary<string, List<Action<MarketData>>> _subscriptions;
        private readonly BrokerApiClient _brokerApiClient;
        private readonly ConcurrentDictionary<string, MarketData> _latestMarketData = new();

        public MarketDataService()
        {
            _subscriptions = new ConcurrentDictionary<string, List<Action<MarketData>>>();
            _brokerApiClient = new BrokerApiClient();
        }

        /// <summary>
        /// Starts the market data service.
        /// Connects to the broker and begins streaming data.
        /// </summary>
        public void Start()
        {
            System.Console.WriteLine("Market Data Service started.");
            _brokerApiClient.Connect();
        }

        /// <summary>
        /// Stops the market data service.
        /// Disconnects from the broker and clears subscriptions.
        /// </summary>
        public void Stop()
        {
            System.Console.WriteLine("Market Data Service stopped.");
            _brokerApiClient.Disconnect();
            _subscriptions.Clear();
        }

        /// <summary>
        /// Subscribe to market data for a specific symbol.
        /// </summary>
        /// <param name="symbol">The symbol to subscribe to.</param>
        /// <param name="onDataReceived">Callback for receiving market data updates.</param>
        public void Subscribe(string symbol, Action<MarketData> onDataReceived)
        {
            if (!_subscriptions.ContainsKey(symbol))
            {
                _subscriptions[symbol] = new List<Action<MarketData>>();
                _brokerApiClient.SubscribeToSymbol(symbol, OnMarketDataReceived);
            }

            _subscriptions[symbol].Add(onDataReceived);
            System.Console.WriteLine($"Subscribed to {symbol}");
        }

        /// <summary>
        /// Unsubscribe from market data for a specific symbol.
        /// </summary>
        /// <param name="symbol">The symbol to unsubscribe from.</param>
        public void Unsubscribe(string symbol)
        {
            if (_subscriptions.TryRemove(symbol, out _))
            {
                //todo - uncomment
                //_brokerApiClient.UnsubscribeFromSymbol(symbol);
                System.Console.WriteLine($"Unsubscribed from {symbol}");
            }
        }

        private void OnMarketDataReceived(MarketData data)
        {
            // Update the latest market data for the symbol
            _latestMarketData[data.Symbol] = data;

            // Notify subscribers
            if (_subscriptions.TryGetValue(data.Symbol, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    Task.Run(() => listener(data)); // Notify subscribers asynchronously
                }
            }
        }

        public double GetLastPrice(string symbol)
        {
            if (_latestMarketData.TryGetValue(symbol, out var marketData))
            {
                return marketData.LastPrice;
            }

            throw new InvalidOperationException($"No market data available for symbol: {symbol}");
        }
    }

    /// <summary>
    /// Represents market data for a specific symbol.
    /// </summary>
    public class MarketData
    {
        public string Symbol { get; set; }
        public double LastPrice { get; set; }
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public double Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
