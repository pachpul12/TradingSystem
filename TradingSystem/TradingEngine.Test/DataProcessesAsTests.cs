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
            _strategy = new MovingAverageCrossoverStrategy(_mockMarketContext, _orderManagementService, 5, 15, 0.02m);
        }

        [Test]
        public void FetchDataForAllStocksInt5Secs()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);
            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);

            historyDataManager.InitEvents();
            ibClient.ConnectToTWS();

            // Fetch all active stocks from the database
            string queryGetAllActiveStocks = @"SELECT id, symbol, exchange_id FROM stocks WHERE active = true ORDER BY id ASC;";
            DataTable tblStocks = _postgresHelper.ExecuteQuery(queryGetAllActiveStocks);

            if (tblStocks == null || tblStocks.Rows.Count == 0)
            {
                Assert.Fail("No active stocks found in the database.");
            }

            DateTime today = DateTime.Today;
            //DateTime sixMonthsAgo = today.AddMonths(-6);
            DateTime sixMonthsAgo = new DateTime(2024, 7, 2);

            // Define trading hours data (9:00 AM to 17:00 PM)
            TimeSpan tradingStart = new TimeSpan(9, 0, 0);
            TimeSpan tradingEnd = new TimeSpan(17, 0, 0);

            int requestsCount = 0;
            DateTime lastRequestTime = DateTime.MinValue;

            for (DateTime currentDate = sixMonthsAgo; currentDate <= today; currentDate = currentDate.AddDays(1))
            {
                // Skip non-business days (Saturday and Sunday)
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                //for (TimeSpan currentTime = tradingStart; currentTime <= tradingEnd; currentTime = currentTime.Add(TimeSpan.FromHours(1)))
                //{
                    foreach (DataRow row in tblStocks.Rows)
                    {
                        int stockId = (int)row["id"];
                        string symbol = row["symbol"].ToString();
                        int exchangeId = (int)row["exchange_id"];

                        string exchange = exchangeId switch
                        {
                            1 => "NASDAQ",
                            2 => "NYSE",
                            _ => throw new ArgumentException("Invalid exchange ID")
                        };


                    string queryGetExistingPrices = String.Format(@"SELECT COUNT(*) FROM public.stocks_prices_int5secs 
                                                        WHERE stock_id = {0} AND DATE(timestamp) = '{1}';", stockId, currentDate.AddDays(-1).ToString("yyyy-MM-dd"));

                    DataTable existingRecordsCount = _postgresHelper.ExecuteQuery(queryGetExistingPrices);

                    if (existingRecordsCount != null && existingRecordsCount.Rows.Count == 1 && (long)existingRecordsCount.Rows[0][0] == 4680)
                    {
                        //4680 records already exist for this stock and date
                        continue;
                    }


                    //DateTime requestTimestamp = currentDate.Date + currentTime;

                    // Ensure no more than 60 requests in a 10-minute window
                    if (requestsCount >= 55)
                        {
                            TimeSpan elapsedTime = DateTime.Now - lastRequestTime;
                            if (elapsedTime < TimeSpan.FromMinutes(12))
                            {
                                Thread.Sleep(TimeSpan.FromMinutes(12) - elapsedTime);
                            }

                            requestsCount = 0;
                        }

                        // Assign start and end dates
                        DateTime startDate = currentDate + tradingStart;
                        DateTime endDate = (currentDate + tradingStart).AddHours(1);

                        // Fetch data for the stock
                        historyDataManager.FetchHistoricalDataInChunks(symbol, exchange, "USD", "STK", "5 secs", "TRADES", startDate, endDate);

                        requestsCount++;
                        lastRequestTime = DateTime.Now;

                        // Pause to avoid exceeding the IB API rate limits
                        Thread.Sleep(1000); // Adjust as needed
                    }
                //}
            }

            ibClient.DisconnectFromTWS();
            Console.WriteLine("Completed fetching data for all stocks.");
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
                    FROM public.stocks_prices
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
        public void Fetch_Missing_SnP500_HistoryData_ForAllStocks_ByNvdaData()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);


            historyDataManager.InitEvents();

            ibClient.ConnectToTWS();

            string queryNvdaDataDates = @"SELECT * FROM (
 SELECT date_trunc('day', ""timestamp"") as timestamp, count(*) as count
 FROM public.stocks_prices
 WHERE stock_id = 1
 GROUP BY date_trunc('day', ""timestamp"")
 order by date_trunc('day', ""timestamp"") DESC
 ) ";
            DataTable tblNvdaDates = _postgresHelper.ExecuteQuery(queryNvdaDataDates);

            if (tblNvdaDates == null || tblNvdaDates.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }

            string queryGetAllSnpStocks = @"SELECT * FROM stocks WHERE exchange_id = 2 order by id asc;";
            DataTable tblStocks = _postgresHelper.ExecuteQuery(queryGetAllSnpStocks);

            if (tblStocks == null || tblStocks.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }


            foreach (DataRow row in tblStocks.Rows)
            {
                int stockId = (int)row["id"];
                string symbol = row["symbol"].ToString();
                bool active = (bool)row["active"];
                if (!active)
                {
                    continue;
                }

                //if (stockId < 4252)
                if (stockId < 4281)
                {
                    continue;
                }

                if (symbol.ToUpper() == "NVDA")
                {
                    continue;
                }

                foreach (DataRow rowNvdaDate in tblNvdaDates.Rows)
                {
                    long countNvda = (long)rowNvdaDate["count"];
                    DateTime timestampNvda = (DateTime)rowNvdaDate["timestamp"];

                    string queryCountPerDate = string.Format(@"SELECT COUNT(*) FROM public.stocks_prices 
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



                    historyDataManager.FetchHistoricalDataInChunks(symbol, "NYSE", "USD", "STK", "1 min", "TRADES", timestampNvda, timestampNvda.AddDays(1));

                }

            }

            ibClient.DisconnectFromTWS();

        }


        [Test]
        public void Fetch_Missing_Nasdaq_HistoryData_ForAllStocks_ByNvdaData()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);


            historyDataManager.InitEvents();

            ibClient.ConnectToTWS();

            string queryNvdaDataDates = @"SELECT * FROM (
 SELECT date_trunc('day', ""timestamp"") as timestamp, count(*) as count
 FROM public.stocks_prices
 WHERE stock_id = 1
 GROUP BY date_trunc('day', ""timestamp"")
 order by date_trunc('day', ""timestamp"") DESC
 ) ";
            DataTable tblNvdaDates = _postgresHelper.ExecuteQuery(queryNvdaDataDates);

            if (tblNvdaDates == null || tblNvdaDates.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }

            string queryGetAllNasdaqStocks = @"SELECT * FROM stocks WHERE exchange_id = 1;";
            DataTable tblStocks = _postgresHelper.ExecuteQuery(queryGetAllNasdaqStocks);

            if (tblStocks == null || tblStocks.Rows.Count == 0)
            {
                Assert.Fail("stocks data for test is invalid, no records");
            }


            foreach (DataRow row in tblStocks.Rows)
            {
                int stockId = (int)row["id"];
                string symbol = row["symbol"].ToString();
                bool active = (bool)row["active"];
                if (!active)
                {
                    continue;
                }

                if (stockId < 70)
                {
                    continue;
                }

                if (symbol.ToUpper() == "NVDA")
                {
                    continue;
                }

                foreach (DataRow rowNvdaDate in tblNvdaDates.Rows)
                {
                    long countNvda = (long)rowNvdaDate["count"];
                    DateTime timestampNvda = (DateTime)rowNvdaDate["timestamp"];

                    string queryCountPerDate = string.Format(@"SELECT COUNT(*) FROM public.stocks_prices 
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

            ibClient.DisconnectFromTWS();

        }
    }
}
