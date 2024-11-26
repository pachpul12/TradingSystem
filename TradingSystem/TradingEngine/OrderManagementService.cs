using System;
using System.Diagnostics.Contracts;
using System.Net;
using IBApi;
using TradingEngine.Interfaces;
using TradingEngine.messages;
using Contract = IBApi.Contract;

namespace TradingEngine.OrderManagement
{
    public class OrderManagementService : IOrderManagementService
    {
        private readonly IBClient _ibClient;

        public OrderManagementService(IBClient ibClient)
        {
            _ibClient = ibClient;
        }

        /// <inheritdoc />
        public void PlaceOrder(string symbol, string exchange, string currency, string orderType, string action, int quantity, double price = 0)
        {
            var contract = new Contract
            {
                Symbol = symbol,
                SecType = "STK", // Stock
                Exchange = exchange,
                Currency = currency
            };

            var order = new Order
            {
                Action = action,
                OrderType = orderType,
                TotalQuantity = quantity,
                LmtPrice = price, // Set for limit orders
                Tif = "GTC" // Good Till Canceled
            };

            int orderId = _ibClient.NextOrderId++;
            Console.WriteLine($"Placing {action} order for {symbol}: {quantity} shares at {price} {currency}");

            _ibClient.ClientSocket.placeOrder(orderId, contract, order);
        }

        /// <inheritdoc />
        public void ModifyOrder(int orderId, int quantity, double price)
        {
            // Fetch the original order details from the broker (this might require additional implementation)
            var contract = _ibClient.RequestIdToContract[orderId];

            var updatedOrder = new Order
            {
                Action = "BUY", // Assuming modification is for a buy order, adapt as necessary
                OrderType = "LMT",
                TotalQuantity = quantity,
                LmtPrice = price,
                Tif = "GTC" // Good Till Canceled
            };

            Console.WriteLine($"Modifying order {orderId}: New quantity = {quantity}, New price = {price}");
            _ibClient.ClientSocket.placeOrder(orderId, contract, updatedOrder);
        }

        /// <inheritdoc />
        public void CancelOrder(int orderId)
        {
            //todo - uncomment and implement
            //Console.WriteLine($"Cancelling order {orderId}");
            //_ibClient.ClientSocket.cancelOrder(orderId);
        }


        /// <inheritdoc />
        public void OnOrderStatusUpdated(OrderStatusMessage orderStatusMessage)
        {
            //todo - uncomment and implement
            //Console.WriteLine($"Order {orderStatusMessage.OrderId} status: {orderStatusMessage.Status}");
        }
    }
}
