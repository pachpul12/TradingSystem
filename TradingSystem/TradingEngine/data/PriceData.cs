using System;

namespace TradingEngine.Data
{
    public class PriceData
    {
        public int StockId { get; set; } // Reference to stock from the Stocks table
        public DateTime Timestamp { get; set; } // Time of the data point
        public decimal Open { get; set; } // Open price
        public decimal High { get; set; } // High price
        public decimal Low { get; set; } // Low price
        public decimal Close { get; set; } // Close price
        public long Volume { get; set; } // Number of shares traded

        // Optional: Constructor for ease of use
        public PriceData()
        {

        }
        public PriceData(int stockId, DateTime timestamp, decimal open, decimal high, decimal low, decimal close, long volume)
        {
            StockId = stockId;
            Timestamp = timestamp;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} | StockId: {StockId} | Open: {Open} | High: {High} | Low: {Low} | Close: {Close} | Volume: {Volume}";
        }
    }
}
