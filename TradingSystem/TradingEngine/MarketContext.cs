using System;
using System.Collections.Generic;
using TradingEngine.Data;

namespace TradingEngine
{
    public class MarketContext
    {
        private readonly Dictionary<string, PriceData> _realTimePrices;
        private readonly Dictionary<string, List<PriceData>> _historicalPrices;
        private readonly object _lock = new object();

        public MarketContext()
        {
            _realTimePrices = new Dictionary<string, PriceData>();
            _historicalPrices = new Dictionary<string, List<PriceData>>();
        }

        /// <summary>
        /// Updates the real-time price for a given symbol.
        /// </summary>
        /// <param name="symbol">The stock symbol.</param>
        /// <param name="priceData">The latest price data.</param>
        public void UpdateRealTimePrice(string symbol, PriceData priceData)
        {
            lock (_lock)
            {
                _realTimePrices[symbol] = priceData;
                Console.WriteLine($"Updated real-time price for {symbol}: {priceData.Close}");
            }
        }

        /// <summary>
        /// Gets the latest real-time price for a given symbol.
        /// </summary>
        /// <param name="symbol">The stock symbol.</param>
        /// <returns>The latest price data, or null if not available.</returns>
        public PriceData GetRealTimePrice(string symbol)
        {
            lock (_lock)
            {
                return _realTimePrices.TryGetValue(symbol, out var priceData) ? priceData : null;
            }
        }

        /// <summary>
        /// Adds historical prices for a given symbol.
        /// </summary>
        /// <param name="symbol">The stock symbol.</param>
        /// <param name="historicalData">The list of historical price data.</param>
        public void AddHistoricalPrices(string symbol, List<PriceData> historicalData)
        {
            lock (_lock)
            {
                if (!_historicalPrices.ContainsKey(symbol))
                {
                    _historicalPrices[symbol] = new List<PriceData>();
                }

                _historicalPrices[symbol].AddRange(historicalData);
                Console.WriteLine($"Added {historicalData.Count} historical price records for {symbol}.");
            }
        }

        /// <summary>
        /// Gets the historical prices for a given symbol.
        /// </summary>
        /// <param name="symbol">The stock symbol.</param>
        /// <returns>The list of historical price data, or an empty list if not available.</returns>
        public List<PriceData> GetHistoricalPrices(string symbol)
        {
            lock (_lock)
            {
                return _historicalPrices.TryGetValue(symbol, out var historicalData)
                    ? new List<PriceData>(historicalData)
                    : new List<PriceData>();
            }
        }

        /// <summary>
        /// Clears all market data.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _realTimePrices.Clear();
                _historicalPrices.Clear();
                Console.WriteLine("Cleared all market data.");
            }
        }
    }
}
