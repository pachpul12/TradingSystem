using System;
using System.Net;
using System.Runtime.CompilerServices;
using IBApi;
using TradingSystem.messages;

namespace TradingSystem
{
    class Program
    {
        static IBClient twsConnector;
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

        public static void realtimeBar(RealTimeBarMessage e)
        {

        }

        static void Main(string[] args)
        {

            var syncCtx = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            EReaderMonitorSignal signal = new EReaderMonitorSignal();

            Console.WriteLine("Starting TWS Connection...");

            twsConnector = new IBClient(signal);

            twsConnector.TickPrice += tickPrice;
            twsConnector.FundamentalData += fundamentalData;
            twsConnector.HistogramData+= histogramData;
            twsConnector.HistoricalData += historicalData;
            twsConnector.RealtimeBar += realtimeBar;




            twsConnector.ConnectToTWS();

            twsConnector.GetRealtimeDataForSymbol("NVDA", "NASDAQ", "USD", "STK");
            twsConnector.GetRealtimeDataForSymbol("MSFT", "NASDAQ", "USD", "STK");

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

            twsConnector.DisconnectFromTWS();
        }
    }
}
