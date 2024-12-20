﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Moq;
using NUnit.Framework;
using TradingEngine.Config;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine.OrderManagement;
using TradingEngine.Strategies;
using TradingEngine.Test;

namespace TradingEngine.Tests
{
    [TestFixture]
    public class OBVStrategyTests
    {
        private TradingStrategy _strategy;
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
            _strategy = new OBVStrategy(_mockMarketContext, _orderManagementService);
        }

        private Dictionary<string, Dictionary<int, TestPosition>> _dictStockPositionsByDayByStock = new Dictionary<string, Dictionary<int, TestPosition>>();
        private List<TestPosition> _testPositions = new List<TestPosition>();

        [Test]
        public void RunStrategyOnOneYearData_IntradayTrading_OneStock()
        {
            // Input parameters
            int stockId = 1; // Example stock ID
            DateTime startDate = new DateTime(2023, 01, 01);
            DateTime endDate = new DateTime(2023, 12, 31);

            RunStrategyForStock(stockId, startDate, endDate);
        }

        [Test]
        [TestCase(["2023-11-23", "2024-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2022-11-23", "2023-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2021-11-23", "2022-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2020-11-23", "2021-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2019-11-23", "2020-11-23", 3, 0, 5, 15, 0.02])]
        public void RunStrategyOnOneYearData_IntradayTrading_MultipleStocks(
            string startDateStr, string endDateStr, int pastPeriods, int stockId,
            int shortTermPeriod, int longTermPeriod, double minCrossoverThresholdStr)
        {

            DateTime startDate = DateTime.Parse(startDateStr);
                DateTime endDate = DateTime.Parse(endDateStr);
            decimal minCrossoverThreshold = (decimal)minCrossoverThresholdStr;

            // Input parameters
            if (stockId > 0)
            {
                RunStrategyForStock(stockId, startDate, endDate);
            }
            else
            {
                //run on all stocks
                for (int i = 1; i < 100; i++)
                {
                    RunStrategyForStock(i, startDate, endDate);
                }
            }
        }

        [Test]
        [TestCase(["2023-11-23", "2024-11-23", 3, 0, 5, 15, 0.01])]
        //[TestCase(["2022-11-23", "2023-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2021-11-23", "2022-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2020-11-23", "2021-11-23", 3, 0, 5, 15, 0.02])]
        //[TestCase(["2019-11-23", "2020-11-23", 3, 0, 5, 15, 0.02])]
        public void RunStrategyOnOneYearData_OBVBreakout(
            string startDateStr, string endDateStr, int pastPeriods, int stockId,
            int shortTermPeriod, int longTermPeriod, double minCrossoverThresholdStr)
        {

            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);
            
            // Input parameters
            if (stockId > 0)
            {
                RunStrategyForStockBreakoutStrategy(stockId, startDate, endDate, (decimal)minCrossoverThresholdStr);
            }
            else
            {
                //run on all stocks
                for (int i = 1; i < 100; i++)
                {
                    RunStrategyForStockBreakoutStrategy(i, startDate, endDate, (decimal)minCrossoverThresholdStr);
                }
            }
        }

        public void RunStrategyForStock(int stockId, DateTime startDate, DateTime endDate)
        {

            _strategy = new OBVStrategy(_mockMarketContext, _orderManagementService);
            TestUtils.RunStrategyForStock(stockId, startDate, endDate, _strategy, _postgresHelper, _mockMarketContext);
        }

        public void RunStrategyForStockBreakoutStrategy(int stockId, DateTime startDate, DateTime endDate, decimal breakoutThreshold)
        {

            _strategy = new OBVBreakoutStrategy(_mockMarketContext, _orderManagementService, 2, breakoutThreshold);
            TestUtils.RunStrategyForStock(stockId, startDate, endDate, _strategy, _postgresHelper, _mockMarketContext);
        }


        [Test]
        public void SimulateEarnings_MovingAverageCrossoverStrategy_HistoryData_EntireDataForStock()
        {
            _testPositions.Clear();
            _dictStockPositionsByDayByStock.Clear();

            string query = @"SELECT * FROM (
                             SELECT date_trunc('day', ""timestamp"") as timestamp, stock_id, count(*) as count
                             FROM public.stocks_prices
                             WHERE stock_id = 1
                             GROUP BY date_trunc('day', ""timestamp""), stock_id
                             order by date_trunc('day', ""timestamp"") DESC
                             ) 
                             WHERE count = 390;";

            DataTable tblValidDays = _postgresHelper.ExecuteQuery(query);

            if (tblValidDays == null || tblValidDays.Rows.Count == 0)
            {
                Assert.Fail("history data for test is invalid, should contain 390 records");
                return;
            }

            foreach (DataRow rowDay in tblValidDays.Rows)
            {
                _mockMarketContext.HistoricalPrices.Clear();

                int stockId = (int)rowDay["stock_id"];
                DateTime timestampDay = (DateTime)rowDay["timestamp"];

                string query2 = @"SELECT * FROM public.stocks_prices 
		                        WHERE timestamp < '{0}' AND timestamp > '{1}' AND stock_id = {2}
		                        order by timestamp asc;";

                DataTable tblPrices = 
                    _postgresHelper.ExecuteQuery(
                        String.Format(query2, timestampDay.AddDays(1).ToString("yyyyMMdd HH:mm:ss"), timestampDay.ToString("yyyyMMdd HH:mm:ss"), stockId));

                if (tblPrices == null || tblPrices.Rows.Count != 390)
                {
                    Assert.Fail("history data for test is invalid, should contain 390 records");
                    return;
                }

                foreach (DataRow rowPrice in tblPrices.Rows)
                {
                    DateTime timestamp = (DateTime)rowPrice["timestamp"];
                    decimal open = (decimal)rowPrice["open_price"];
                    decimal high = (decimal)rowPrice["high_price"];
                    decimal low = (decimal)rowPrice["low_price"];
                    decimal close = (decimal)rowPrice["close_price"];
                    decimal volume = (decimal)rowPrice["volume"];
                    int volumeInt = (int)volume;

                    PriceData priceData = new PriceData
                    {
                        StockId = stockId,
                        Timestamp = timestamp,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = volumeInt
                    };

                    _mockMarketContext.HistoricalPrices.Add(priceData);

                    TradingSignal tradingSignal = _strategy.Evaluate(_mockMarketContext);

                    if (tradingSignal.Action == SignalType.Buy)
                    {
                        TestPosition testPosition = new TestPosition
                        {
                            BuyPrice = priceData.Close,
                            IsOpen = true,
                            Quantity = 1,
                            SellPrice = null,
                            BuyDate = priceData.Timestamp,
                            SellDate = null
                        };

                        if (!_dictStockPositionsByDayByStock.ContainsKey(priceData.Timestamp.ToString("yyyyMMdd")))
                        {
                            _dictStockPositionsByDayByStock[priceData.Timestamp.ToString("yyyyMMdd")] = new Dictionary<int, TestPosition>();
                        }

                        var dictStockPositionsByDay = _dictStockPositionsByDayByStock[priceData.Timestamp.ToString("yyyyMMdd")];

                        if (dictStockPositionsByDay.ContainsKey(stockId))
                        {
                            //open position
                            continue;
                        }

                        dictStockPositionsByDay[stockId] = testPosition;



                    }
                    else if (tradingSignal.Action == SignalType.Sell)
                    {
                        if (!_dictStockPositionsByDayByStock.ContainsKey(priceData.Timestamp.ToString("yyyyMMdd")))
                        {
                            continue;
                        }

                        var dictStockPositionsByDay = _dictStockPositionsByDayByStock[priceData.Timestamp.ToString("yyyyMMdd")];

                        if (!dictStockPositionsByDay.ContainsKey(stockId))
                        {
                            //open position
                            continue;
                        }


                        TestPosition testPosition = dictStockPositionsByDay[stockId];

                        testPosition.SellPrice = priceData.Close;
                        testPosition.SellDate = priceData.Timestamp;
                        testPosition.IsOpen = false;

                        dictStockPositionsByDay.Remove(stockId);

                        _testPositions.Add(testPosition);
                    }

                } 
            }


            decimal totalEarnings = 0;

            foreach (TestPosition testPosition in _testPositions)
            {
                if (!testPosition.IsOpen && testPosition.SellPrice.HasValue)
                {
                    // Calculate the earnings for the position
                    decimal earnings = (testPosition.SellPrice.Value - testPosition.BuyPrice) * testPosition.Quantity;
                    totalEarnings += earnings;
                }
            }

            Assert.Warn("totalEarnings is " + totalEarnings);
            Assert.IsTrue(totalEarnings > 0, "totalEarnings is negative");


        }


        [Test]
        public void Check_MovingAverageCrossoverStrategy_HistoryData_OneDay()
        {   
            DataTable tblPrices = _postgresHelper.ExecuteQuery("SELECT * FROM public.stocks_prices WHERE timestamp < '2024-11-26' AND timestamp > '2024-11-25' AND stock_id = 1 order by timestamp asc");

            if (tblPrices == null || tblPrices.Rows.Count != 390)
            {
                Assert.Fail("history data for test is invalid, should contain 390 records");
            }

            _mockMarketContext.HistoricalPrices.Clear();

            foreach (DataRow row in tblPrices.Rows)
            {
                int stockId = (int)row["stock_id"];
                DateTime timestamp = (DateTime)row["timestamp"];
                decimal open = (decimal)row["open_price"];
                decimal high = (decimal)row["high_price"];
                decimal low = (decimal)row["low_price"];
                decimal close = (decimal)row["close_price"];
                decimal volume = (decimal)row["volume"];
                int volumeInt = (int)volume;

                PriceData priceData = new PriceData
                {
                    StockId = stockId,
                    Timestamp = timestamp,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volumeInt
                };

                _mockMarketContext.HistoricalPrices.Add(priceData);

                TradingSignal tradingSignal = _strategy.Evaluate(_mockMarketContext);

                if (_mockMarketContext.HistoricalPrices.Count == 33 || _mockMarketContext.HistoricalPrices.Count == 34
                        || _mockMarketContext.HistoricalPrices.Count == 35)
                {
                    Assert.AreEqual(tradingSignal.Action, SignalType.Buy);
                }
            }
        }

        [Test]
        public void Evaluate_BuySignal_WhenShortTermMACrossesAboveLongTermMA()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 10, 11, 12, 13, 14, 15, 16, 17 , 18, 19, 20, 21, 22, 23, 24, 25});


            // Act
            var signal = _strategy.Evaluate(_mockMarketContext);

            // Assert
            Assert.AreEqual(SignalType.Buy, signal.Action);
        }

        [Test]
        public void Evaluate_SellSignal_WhenShortTermMACrossesBelowLongTermMA()
        {
            // Arrange
            _mockMarketContext.HistoricalPrices = GeneratePriceData(new List<decimal> { 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 });
            
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
