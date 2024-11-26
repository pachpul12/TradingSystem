using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine.OrderManagement;
using TradingEngine.Strategies;

namespace TradingEngine.Tests
{
    [TestFixture]
    public class MovingAverageCrossoverStrategyTests
    {
        private MovingAverageCrossoverStrategy _strategy;
        private MarketContext _mockMarketContext;
        private IOrderManagementService _orderManagementService;

        [SetUp]
        public void SetUp()
        {
            _mockMarketContext = new MarketContext();
            
            Mock<IOrderManagementService> mockObject = new Mock<IOrderManagementService>();
            mockObject.Setup(m => m.PlaceOrder("", "", "", "", "", 0, 0));
            _orderManagementService = mockObject.Object;
            _strategy = new MovingAverageCrossoverStrategy(_mockMarketContext, _orderManagementService, 5, 15);
        }

        [Test]
        public void Evaluate_BuySignal_WhenShortTermMACrossesAboveLongTermMA()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 10, 11, 12, 13, 14, 15, 16, 17 });


            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Buy, signal.Action);
        }

        [Test]
        public void Evaluate_SellSignal_WhenShortTermMACrossesBelowLongTermMA()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 17, 16, 15, 14, 13, 12, 11, 10 });
            
            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Sell, signal.Action);
        }

        [Test]
        public void Evaluate_HoldSignal_WhenNoCrossoverOccurs()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 10, 10, 10, 10, 10, 10, 10, 10 });
            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Hold, signal.Action);
        }

        private List<PriceData> GeneratePriceData(List<decimal> closePrices)
        {
            var priceData = new List<PriceData>();
            var timestamp = DateTime.Now;

            for (int i = 0; i < closePrices.Count; i++)
            {
                priceData.Add(new PriceData
                {
                    StockId = 1, // Mock stock ID
                    Timestamp = timestamp.AddMinutes(-i),
                    Close = closePrices[i],
                    Volume = 0,
                    High = 0,
                    Low = 0
                });
            }

            return priceData;
        }
    }
}
