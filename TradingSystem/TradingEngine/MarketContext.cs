using System;
using System.Collections.Generic;
using TradingEngine.Data;

namespace TradingEngine
{
    public class MarketContext
    {
        public decimal CurrentPrice { get; set; }
        public List<PriceData> HistoricalPrices { get; set; }
        private readonly object _lock = new object();

        public MarketContext()
        {
            HistoricalPrices = new List<PriceData>();
        }

        
    }
}
