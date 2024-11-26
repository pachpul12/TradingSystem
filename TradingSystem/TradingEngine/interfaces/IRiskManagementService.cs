namespace TradingEngine.Interfaces
{
    public interface IRiskManagementService
    {
        bool CanPlaceOrder(string symbol, int quantity, double price, string action);
        double GetMaxAllowedRiskPerTrade();
        double CalculateRiskForOrder(double entryPrice, double stopLossPrice, int quantity);
        void UpdatePortfolioRiskMetrics(string symbol, double positionRisk);
    }
}
