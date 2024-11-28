using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using IBApi;
using TradingEngine.messages;
using TradingEngineConsole;

namespace TradingEngine
{
    class Program
    {
        static IBClient ibClient;
        
        public static void tickPrice(TickPriceMessage e)
        {
            //twsConnector.RequestIdToContract
            //var button = (Button)sender; //Need to cast here
        }

        public static void fundamentalData(FundamentalsMessage e)
        {
            //var button = (Button)sender; //Need to cast here
        }

        public static void histogramData(HistogramDataMessage e)
        {
            //var button = (Button)sender; //Need to cast here
        }

        public static void historicalTick(HistoricalTickMessage e)
        {
            //var button = (Button)sender; //Need to cast here
        }

        public static void historicalTickLast(HistoricalTickLastMessage e)
        {
            long unixTimeStamp = e.Time;
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static void realtimeBar(RealTimeBarMessage e)
        {

        }

        public static void historicalDataAllEnded(object sender, EventArgs e)
        {
            
        }

        static void Main(string[] args)
        {

            var syncCtx = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            EReaderMonitorSignal signal = new EReaderMonitorSignal();

            Console.WriteLine("Starting TWS Connection...");

            ibClient = new IBClient(signal);

            HistoryDataManager historyDataManager = 
                new HistoryDataManager(new PostgresHelper(
                    //ConfigurationManager.AppSettings["PostgresConnection"]
                    "Host=localhost;Port=5432;Database=tradingdb;Username=postgres;Password=postgres"
                    ), ibClient);
            historyDataManager.InitEvents();

            ibClient.TickPrice += tickPrice;
            ibClient.FundamentalData += fundamentalData;
            ibClient.HistogramData+= histogramData;
            ibClient.historicalTick += historicalTick;
            ibClient.historicalTickLast += historicalTickLast;
            
            ibClient.RealtimeBar += realtimeBar;




            ibClient.ConnectToTWS();

            //ibClient.GetRealtimeDataForSymbol("NVDA", "NASDAQ", "USD", "STK");
            //ibClient.GetRealtimeDataForSymbol("MSFT", "NASDAQ", "USD", "STK");

            
            string oneMonthAgo = String.Concat(DateTime.Now.AddMonths(-1).ToString("yyyyMMdd hh:mm:ss"), "");
            string yesterday = String.Concat(DateTime.Now.AddDays(-1).ToString("yyyyMMdd hh:mm:ss"), "");
            string twoDaysAgo = String.Concat(DateTime.Now.AddDays(-2).ToString("yyyyMMdd hh:mm:ss"), "");


            //historyDataManager.GetDataForSymbol(
            //    new DateTime(2023, 01, 01, 0, 0, 0), 
            //    new DateTime(2024, 01, 01, 0, 0, 0), 
            //    "NVDA",
            //    "NASDAQ");

            historyDataManager.FetchHistoricalDataInChunks("AMD", "NASDAQ", "USD", "STK", "1 min", "TRADES", DateTime.Now.AddYears(-5), DateTime.Now);
            //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "5 secs", "STK", "20241120 23:59:59", "TRADES");

            //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "5 secs", "STK", "20241119 23:59:59", "TRADES");

            ////7
            //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "5 secs", "STK", "20241118 23:59:59", "TRADES");

            Console.WriteLine("Requesting historical tick data...");
            //ibClient.GetHistoricalTickForSymbol(
            //    symbol: "NVDA",              // Example symbol
            //    exchange: "NASDAQ",          // Example exchange
            //    currency: "USD",             // Example currency
            //    secType: "STK",              // Security type: Stock
            //    startTime: "20241120-09:30:00", // Example start time
            //    endTime: "20241121-23:00:00",   // Example end time
            //    numberOfTicks: 1,          // Number of ticks to retrieve
            //    whatToShow: "MIDPOINT"// "TRADES"         // Data type to retrieve
            //);

            //Simulate monitoring for a short period

            //int requestId = 1; // Assuming this is the request ID for AAPL
            //for (int i = 0; i < 10; i++)
            //    {
            //        var latestPrice = twsConnector.GetLatestPrice(requestId);
            //        if (latestPrice.HasValue)
            //        {
            //            Console.WriteLine($"Latest price for request {requestId}: {latestPrice.Value}");
            //        }
            //        else
            //        {
            //            Console.WriteLine("Waiting for price update...");
            //        }

            //        Thread.Sleep(1000); // Wait for 1 second
            //    }

            Console.WriteLine("Press any key to disconnect and exit...");
            Console.ReadKey();

            ibClient.DisconnectFromTWS();
        }
    }
}
