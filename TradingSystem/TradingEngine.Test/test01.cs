using NUnit.Framework;
using TradingEngine;
using TradingEngine.Strategies;
using System;
using System.Collections.Generic;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using Moq;

namespace TradingEngine.Tests
{
    [TestFixture]
    public class TradingStrategyTests
    {
        
        

        [Test]
        public void EvaluateStrategy_ShouldReturnCorrectSignal()
        {
            Mock<IOrderManagementService> mockObject = new Mock<IOrderManagementService>();
            mockObject.Setup(m => m.PlaceOrder("", "", "", "", "", 0, 0));
            IOrderManagementService value = mockObject.Object;

            // Arrange
            var marketContext = new MarketContext
            {
                CurrentPrice = 100,
                HistoricalPrices = new List<PriceData>
                {
                    new PriceData(1, DateTime.Now.AddMinutes(-5), 0, 0, 0, 90 ,0),
                    new PriceData(1, DateTime.Now.AddMinutes(-4), 0, 0, 0, 95 ,0),
                    new PriceData(1, DateTime.Now.AddMinutes(-3), 0, 0, 0, 97 ,0),
                    new PriceData(1, DateTime.Now.AddMinutes(-2), 0, 0, 0, 99 ,0),
                    new PriceData(1, DateTime.Now.AddMinutes(-1), 0, 0, 0, 100 ,0)
                }
            };

            var strategy = new ExampleTradingStrategy(marketContext, value); // Example implementation

            // Act
            var signal = strategy.Evaluate(marketContext);

            // Assert
            Assert.NotNull(signal, "Signal should not be null.");
            Assert.AreEqual(SignalType.Buy, signal.Action, "Signal type should be 'Buy'.");
        }
    }
}
