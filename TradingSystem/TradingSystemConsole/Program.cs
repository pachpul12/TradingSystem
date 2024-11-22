using System;
using IBApi;

namespace TradingSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            EReaderMonitorSignal signal = new EReaderMonitorSignal();

            Console.WriteLine("Starting TWS Connection...");

            var twsConnector = new IBClient(signal);
            twsConnector.ConnectToTWS();

            twsConnector.GetRealtimeDataForSymbol("NVDA", "NASDAQ", "USD", "STK");

            Console.WriteLine("Press any key to disconnect and exit...");
            Console.ReadKey();

            twsConnector.DisconnectFromTWS();
        }
    }
}
