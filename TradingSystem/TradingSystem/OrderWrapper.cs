using IBApi;

namespace TradingSystem.Core
{
    public class OrderWrapper
    {
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public string Side { get; set; } // "Buy" or "Sell"
        public string OrderType { get; set; } = "MKT"; // Default to Market Order
        public double? Price { get; set; } // For Limit/Stop orders, null for Market orders

        public Contract ToContract()
        {
            return new Contract
            {
                Symbol = Symbol,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };
        }

        public Order ToOrder()
        {
            var order = new Order
            {
                Action = Side,
                OrderType = OrderType,
                TotalQuantity = Quantity
            };

            if (Price.HasValue)
            {
                order.LmtPrice = Price.Value;
            }

            return order;
        }
    }
}
