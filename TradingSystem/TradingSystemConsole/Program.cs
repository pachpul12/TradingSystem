using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using IBApi;
using TradingEngine.messages;
using TradingEngine.Test;
using TradingEngine.Tests;
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
            DataProcessesAsTests dataProcessesAsTests = new DataProcessesAsTests();
            dataProcessesAsTests.SetUp();

            //RealtimeTests realtimeTests = new RealtimeTests();
            //realtimeTests.SetUp();

            //realtimeTests.Run_Realtime_Engine();

            dataProcessesAsTests.FetchDataForAllStocksInt5Secs();

            Console.WriteLine("Press any key to disconnect and exit...");
            Console.ReadKey();

            

            return;

        }
    }
}
