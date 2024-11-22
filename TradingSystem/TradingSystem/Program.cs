using System;

namespace TradingSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting TWS Connection...");

            var twsConnector = new TwsConnector();
            twsConnector.ConnectToTWS();

            Console.WriteLine("Press any key to disconnect and exit...");
            Console.ReadKey();

            twsConnector.DisconnectFromTWS();
        }
    }
}
