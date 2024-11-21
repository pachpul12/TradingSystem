using IBApi;
using TradingSystem.Core;

public class OrderService
{
    public void PlaceOrder(Contract contract, Order order)
    {
        Console.WriteLine($"Placing {order.Action} order for {contract.Symbol}...");
        // Send the order to the broker API
        //BrokerApiClient.ClientSocket.placeOrder(BrokerApiClient.NextOrderId++, contract, order);
    }
}