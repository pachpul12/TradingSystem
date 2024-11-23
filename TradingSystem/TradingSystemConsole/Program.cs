using System;
using System.Net;
using System.Runtime.CompilerServices;
using IBApi;
using TradingSystem.messages;

namespace TradingSystem
{
    class Program
    {
        static IBClient ibClient;
        public static void tickPrice(TickPriceMessage e)
        {
            //twsConnector.RequestIdToSymbol
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
        public static void historicalData(HistoricalDataMessage e)
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
            //var button = (Button)sender; //Need to cast here
        }

        public static void realtimeBar(RealTimeBarMessage e)
        {

        }

        static void Main(string[] args)
        {

            var syncCtx = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            EReaderMonitorSignal signal = new EReaderMonitorSignal();

            Console.WriteLine("Starting TWS Connection...");

            ibClient = new IBClient(signal);

            ibClient.TickPrice += tickPrice;
            ibClient.FundamentalData += fundamentalData;
            ibClient.HistogramData+= histogramData;
            ibClient.HistoricalData += historicalData;
            ibClient.historicalTick += historicalTick;
            ibClient.historicalTickLast += historicalTickLast;
            
            ibClient.RealtimeBar += realtimeBar;




            ibClient.ConnectToTWS();

            ibClient.GetRealtimeDataForSymbol("NVDA", "NASDAQ", "USD", "STK");
            ibClient.GetRealtimeDataForSymbol("MSFT", "NASDAQ", "USD", "STK");

            //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "1 min");
            
            string oneMonthAgo = String.Concat(DateTime.Now.AddMonths(-1).ToString("yyyyMMdd hh:mm:ss"), "");
            string yesterday = String.Concat(DateTime.Now.AddDays(-1).ToString("yyyyMMdd hh:mm:ss"), "");
            string twoDaysAgo = String.Concat(DateTime.Now.AddDays(-2).ToString("yyyyMMdd hh:mm:ss"), "");

            Console.WriteLine("Requesting historical tick data...");
            ibClient.GetHistoricalTickForSymbol(
                symbol: "AAPL",              // Example symbol
                exchange: "NASDAQ",          // Example exchange
                currency: "USD",             // Example currency
                secType: "STK",              // Security type: Stock
                startTime: "20241121 09:30:00", // Example start time
                endTime: "20241121 23:00:00",   // Example end time
                numberOfTicks: 100,          // Number of ticks to retrieve
                whatToShow: "TRADES"         // Data type to retrieve
            );

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
