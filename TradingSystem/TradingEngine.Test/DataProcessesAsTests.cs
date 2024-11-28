using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using IBApi;
using Moq;
using NUnit.Framework;
using TradingEngine.Config;
using TradingEngine.Data;
using TradingEngine.Interfaces;
using TradingEngine.OrderManagement;
using TradingEngine.Strategies;

namespace TradingEngine.Tests
{
    [TestFixture]
    public class DataProcessesAsTests
    {
        private MovingAverageCrossoverStrategy _strategy;
        private MarketContext _mockMarketContext;
        private IOrderManagementService _orderManagementService;

        private EngineConfig _engineConfig;
        private string _connectionString;
        private PostgresHelper _postgresHelper;

        [SetUp]
        public void SetUp()
        {
            var syncCtx = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            _engineConfig = EngineConfig.Load("engine_config.json");
            _connectionString = _engineConfig.Database.ConnectionString;
            _postgresHelper = new PostgresHelper(_connectionString);

            _mockMarketContext = new MarketContext();

            Mock<IOrderManagementService> mockObject = new Mock<IOrderManagementService>();
            mockObject.Setup(m => m.PlaceOrder("", "", "", "", "", 0, 0));

            _orderManagementService = mockObject.Object;
            _strategy = new MovingAverageCrossoverStrategy(_mockMarketContext, _orderManagementService, 5, 15);
        }

        [Test]
        [Ignore("Ignore a process data creation")]
        public void FetchStock_Missing_HistoryData()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);
            

            string query = @"SELECT * FROM (
                    SELECT date_trunc('day', ""timestamp"") as timestamp, stock_id, count(*) as count
                    FROM public.historical_prices
                    WHERE stock_id = 1
                    GROUP BY date_trunc('day', ""timestamp""), stock_id
                    order by date_trunc('day', ""timestamp"") desc
                    ) 
                    WHERE count < 390;";

            DataTable tblPrices = _postgresHelper.ExecuteQuery(query);

            if (tblPrices == null || tblPrices.Rows.Count == 0)
            {
                Assert.Fail("history data for test is invalid, no records");
            }

            historyDataManager.InitEvents();

            ibClient.ConnectToTWS();

            foreach (DataRow row in tblPrices.Rows)
            {
                int stockId = (int)row["stock_id"];
                DateTime timestamp = (DateTime)row["timestamp"];
                long count = (long)row["count"];

                historyDataManager.FetchHistoricalDataInChunks("NVDA", "NASDAQ", "USD", "STK", "1 min", "TRADES", timestamp, timestamp.AddDays(1));

            }
        }

        [Test]
        [Ignore("Ignore a process data creation")]
        public void FetchStock_HistoryData_ForSymbol()
        {

        }
    }
}
