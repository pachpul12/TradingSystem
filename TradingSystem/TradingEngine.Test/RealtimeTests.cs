using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBApi;
using Moq;
using TradingEngine.Config;
using TradingEngine.Interfaces;
using TradingEngine.Strategies;
using TradingEngine.Tests;

namespace TradingEngine.Test
{
    [TestFixture]
    public class RealtimeTests
    {

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
        }

        [Test]
        public void Run_Realtime_Engine()
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();
            IBClient ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = new HistoryDataManager(_postgresHelper, ibClient);

            historyDataManager.InitEvents();

            ibClient.ConnectToTWS();

            string queryGetAllSnpStocks = @"SELECT * FROM stocks WHERE active = true ORDER BY id ASC;";
            DataTable tblStocks = _postgresHelper.ExecuteQuery(queryGetAllSnpStocks);

            if (tblStocks == null || tblStocks.Rows.Count == 0)
            {
                Assert.Fail("Stocks data for test is invalid, no records.");
            }

            const int maxRequestsPerBatch = 50; // Maximum requests per 10-minute window
            const int delayBetweenBatches = 10 * 60 * 1000; // 10 minutes in milliseconds

            List<DataRow> stockRows = tblStocks.AsEnumerable().ToList();
            int totalStocks = stockRows.Count;
            int batchNumber = 0;

            for (int i = 0; i < totalStocks; i += maxRequestsPerBatch)
            {
                batchNumber++;
                Console.WriteLine($"Processing batch {batchNumber}...");

                // Process up to maxRequestsPerBatch stocks in this batch
                var currentBatch = stockRows.Skip(i).Take(maxRequestsPerBatch);

                foreach (var row in currentBatch)
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

                    historyDataManager.StartGetRealtimeStockPrices(stockId, symbol, exchange, "USD", "STK", 5);
                }

                // If there are more stocks to process, wait for the next batch
                if (i + maxRequestsPerBatch < totalStocks)
                {
                    Console.WriteLine($"Batch {batchNumber} completed. Waiting {delayBetweenBatches / 1000} seconds before the next batch...");
                    Thread.Sleep(delayBetweenBatches);
                }
            }

            Console.WriteLine("All realtime stock price requests have been processed.");
        }

    }
}
