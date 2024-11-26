using TradingEngine.messages;

namespace TradingEngine.Interfaces
{
    public interface IOrderManagementService
    {
        void PlaceOrder(string symbol, string exchange, string currency, string orderType, string action, int quantity, double price = 0);
        void ModifyOrder(int orderId, int quantity, double price);
        void CancelOrder(int orderId);
        void OnOrderStatusUpdated(OrderStatusMessage orderStatusMessage);
    }
}
