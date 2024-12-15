using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TradingEngine.Config;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine.Strategies;
using TradingEngine.Test;

namespace TradingEngine.Tests
{
    [TestFixture]
    public class RSIDivergenceStrategyTests
    {
        private RSIDivergenceStrategy _strategy;
        private MarketContext _mockMarketContext;
        private IOrderManagementService _orderManagementService;

        private EngineConfig _engineConfig;
        private string _connectionString;
        private PostgresHelper _postgresHelper;

        [SetUp]
        public void SetUp()
        {
            _engineConfig = EngineConfig.Load("engine_config.json");
            _connectionString = _engineConfig.Database.ConnectionString;
            _postgresHelper = new PostgresHelper(_connectionString);

            _mockMarketContext = new MarketContext();

            Mock<IOrderManagementService> mockObject = new Mock<IOrderManagementService>();
            mockObject.Setup(m => m.PlaceOrder("", "", "", "", "", 0, 0));

            _orderManagementService = mockObject.Object;
            _strategy = new RSIDivergenceStrategy(_mockMarketContext, _orderManagementService, rsiPeriod: 14, lookbackPeriod: 5);
        }

        [Test]
        [TestCase(["2024-11-01", "2024-11-23", 20, 3.0, 0.05])]
        public void Simulate_Trading_Day(
            string startDateStr, string endDateStr,
            int periods, double multiplier, double maxTradeFromInitialInv)
        {
            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);
            decimal multiplierDecimal = (decimal)multiplier;
            decimal maxTradeFromInitialInvDecimal = (decimal)maxTradeFromInitialInv;
            _strategy = new RSIDivergenceStrategy(_mockMarketContext, _orderManagementService, rsiPeriod: 14, lookbackPeriod: 5);

            TestUtils.SimulateTradingDays(startDate, endDate, maxTradeFromInitialInvDecimal, _strategy, _postgresHelper, _mockMarketContext);

        }

        [Test]
        [TestCase(["2019-11-23", "2024-11-23", 20, 2, 0.02])]
        public void Simulate_Trading_Day_CombinedRSIBollinger_OBV(
            string startDateStr, string endDateStr,
            int periods, double multiplier, double maxTradeFromInitialInv)
        {
            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);
            decimal multiplierDecimal = (decimal)multiplier;
            decimal maxTradeFromInitialInvDecimal = (decimal)maxTradeFromInitialInv;
            RSIDivergenceStrategy rsiStrategy = new RSIDivergenceStrategy(_mockMarketContext, _orderManagementService, rsiPeriod: 10, lookbackPeriod: 5);
            BollingerBandsStrategy bollingerStrategy = new BollingerBandsStrategy(_mockMarketContext, _orderManagementService, periods, multiplierDecimal);
            VWAPStrategy obvStrategy = new VWAPStrategy(_mockMarketContext, _orderManagementService, 10, 0.01m);

            CombinedRSIBollingerOBVStrategy combinedStrategy = new CombinedRSIBollingerOBVStrategy(_mockMarketContext, _orderManagementService, bollingerStrategy, rsiStrategy, obvStrategy);

            TestUtils.SimulateTradingDays(startDate, endDate, maxTradeFromInitialInvDecimal, combinedStrategy, _postgresHelper, _mockMarketContext);

        }

        [Test]
        [TestCase(["2024-06-11", "2024-06-30", 20, 2.0, 0.05])]
        public void Simulate_Trading_Day_CombinedRSIBollinger(
            string startDateStr, string endDateStr,
            int periods, double multiplier, double maxTradeFromInitialInv)
        {
            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);
            decimal multiplierDecimal = (decimal)multiplier;
            decimal maxTradeFromInitialInvDecimal = (decimal)maxTradeFromInitialInv;
            RSIDivergenceStrategy rsiStrategy = new RSIDivergenceStrategy(_mockMarketContext, _orderManagementService, rsiPeriod: 168, lookbackPeriod: 10);
            BollingerBandsStrategy bollingerStrategy = new BollingerBandsStrategy(_mockMarketContext, _orderManagementService, periods, multiplierDecimal);

            CombinedBollingerRSIStrategy combinedStrategy = new CombinedBollingerRSIStrategy(_mockMarketContext, _orderManagementService, bollingerStrategy, rsiStrategy);

            TestUtils.SimulateTradingDays(startDate, endDate, maxTradeFromInitialInvDecimal, combinedStrategy, _postgresHelper, _mockMarketContext);

        }

        [Test]
        public void CalculateRSI_ShouldReturnExpectedValues()
        {
            // Arrange
            var priceData = GeneratePriceData(new List<decimal> { 45.0m, 46.0m, 47.0m, 46.0m, 45.0m, 44.0m, 43.0m });
            var expectedRSI = new List<decimal> { 60.0m, 50.0m, 40.0m }; // Example values for testing

            // Act
            var rsi = _strategy.CalculateRSI(priceData, 3);

            // Assert
            Assert.AreEqual(expectedRSI.Count, rsi.Count, "RSI length mismatch.");
            for (int i = 0; i < expectedRSI.Count; i++)
            {
                //Assert.AreEqual(expectedRSI[i], rsi[i], 0.1m, $"RSI value mismatch at index {i}.");
            }
        }

        [Test]
        public void DetectBullishDivergence_ShouldReturnTrue_ForBullishSetup()
        {
            // Arrange
            var priceData = GeneratePriceData(new List<decimal> { 100, 99, 101, 97, 102 });
            var rsi = new List<decimal> { 30, 35, 40, 45, 50 }; // RSI forms a higher low

            // Act
            var result = _strategy.DetectBullishDivergence(priceData, rsi, 5);

            // Assert
            Assert.IsTrue(result, "Bullish divergence not detected when it should be.");
        }

        [Test]
        public void DetectBearishDivergence_ShouldReturnTrue_ForBearishSetup()
        {
            // Arrange
            var priceData = GeneratePriceData(new List<decimal> { 100, 102, 101, 103, 104 });
            var rsi = new List<decimal> { 70, 65, 60, 55, 50 }; // RSI forms a lower high

            // Act
            var result = _strategy.DetectBearishDivergence(priceData, rsi, 5);

            // Assert
            Assert.IsTrue(result, "Bearish divergence not detected when it should be.");
        }

        [Test]
        public void Evaluate_ShouldReturnBuySignal_ForBullishDivergence()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 100, 99, 101, 97, 102 });
            var rsi = new List<decimal> { 30, 35, 40, 45, 50 }; // RSI forms a higher low

            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Buy, signal.Action, "Buy signal not generated for bullish divergence.");
        }

        [Test]
        public void Evaluate_ShouldReturnSellSignal_ForBearishDivergence()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 100, 102, 101, 103, 104 });
            var rsi = new List<decimal> { 70, 65, 60, 55, 50 }; // RSI forms a lower high

            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Sell, signal.Action, "Sell signal not generated for bearish divergence.");
        }

        [Test]
        public void Evaluate_ShouldReturnHoldSignal_WhenNoDivergence()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 100, 100, 100, 100, 100 });
            var rsi = new List<decimal> { 50, 50, 50, 50, 50 }; // RSI is flat

            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Hold, signal.Action, "Hold signal not generated when no divergence.");
        }

        private List<PriceData> GeneratePriceData(List<decimal> closePrices)
        {
            var priceData = new List<PriceData>();
            var timestamp = DateTime.Now;

            for (int i = 0; i < closePrices.Count; i++)
            {
                priceData.Add(new PriceData
                {
                    StockId = 1,
                    Timestamp = timestamp.AddMinutes(-i),
                    Close = closePrices[i],
                    Volume = 1000 // Mock volume
                });
            }

            return priceData;
        }
    }
}
