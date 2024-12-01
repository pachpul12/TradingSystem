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
        [TestCase("NVDA", "NASDAQ")]
        [Ignore("Ignore a process data creation")]
        public void FetchStock_HistoryData_ForSymbol(string symbol, string exchange)
        {
            DateTime endDate = new DateTime(2024, 11, 29);
            DateTime startDate = new DateTime(2024, 11, 25);

            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);
            historyDataManager.InitEvents();
            ibClient.ConnectToTWS();

            historyDataManager.FetchHistoricalDataInChunks(symbol, exchange, "USD", "STK", "1 min", "TRADES", startDate, endDate);
            //historyDataManager.FetchHistoricalDataInChunks(symbol, exchange, "USD", "STK", "1 min", "TRADES", endDate.AddDays(-1), endDate);

            while (true)
            {
            }
        }


        [Test]
        public void FetchAllStocks_Missing_HistoryData_ForAllStocks_ByNvdaData()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);


            historyDataManager.InitEvents();

            ibClient.ConnectToTWS();

            string queryNvdaDataDates = @"SELECT * FROM (
 SELECT date_trunc('day', ""timestamp"") as timestamp, count(*) as count
 FROM public.historical_prices
 WHERE stock_id = 1
 GROUP BY date_trunc('day', ""timestamp"")
 order by date_trunc('day', ""timestamp"") DESC
 ) ";
            DataTable tblNvdaDates = _postgresHelper.ExecuteQuery(queryNvdaDataDates);

            if (tblNvdaDates == null || tblNvdaDates.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }

            string queryGetAllStocks = @"SELECT * FROM STOCKS;";
            DataTable tblStocks = _postgresHelper.ExecuteQuery(queryGetAllStocks);

            if (tblStocks == null || tblStocks.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }


            foreach (DataRow row in tblStocks.Rows)
            {
                int stockId = (int)row["id"];
                string symbol = row["symbol"].ToString();

                if (symbol.ToUpper() == "NVDA")
                {
                    continue;
                }
                if (symbol.ToUpper() == "FB")
                {
                    continue;
                }
                if (symbol.ToUpper() == "FISV")
                {
                    continue;
                }
                if (symbol.ToUpper() == "ATVI")
                {
                    continue;
                }
                if (symbol.ToUpper() == "AACG")
                {
                    continue;
                }

                foreach (DataRow rowNvdaDate in tblNvdaDates.Rows)
                {
                    long countNvda = (long)rowNvdaDate["count"];
                    DateTime timestampNvda = (DateTime)rowNvdaDate["timestamp"];

                    string queryCountPerDate = string.Format(@"SELECT COUNT(*) FROM public.historical_prices 
                                                    WHERE stock_id = {0} AND 
                                                        timestamp > '{1}' AND timestamp < '{2}'", 
                                                        stockId.ToString(),
                                                        timestampNvda.ToString("yyyy-MM-dd"),
                                                        timestampNvda.AddDays(1).ToString("yyyy-MM-dd")
                                                        );

                    DataTable tblCount = _postgresHelper.ExecuteQuery(queryCountPerDate);

                    long countStock = (long)tblCount.Rows[0][0];

                    if (countStock == countNvda)
                    {
                        continue;
                    }

                    historyDataManager.FetchHistoricalDataInChunks(symbol, "NASDAQ", "USD", "STK", "1 min", "TRADES", timestampNvda, timestampNvda.AddDays(1));

                }

            }

            
        }
    }
}
