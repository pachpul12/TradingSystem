using System;
using IBApi;
using TradingSystem.Core;

namespace TradingSystem.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            TradingSystemManager tradingSystemManager = new TradingSystemManager();

            tradingSystemManager.Initialize();

            //todo - register strategies
            //tradingSystemManager.RegisterStrategy();
            //tradingSystemManager.RegisterStrategy();


            tradingSystemManager.Start();

            


            tradingSystemManager.Stop();



            //IB's main object
            EWrapperImpl ibClient = new EWrapperImpl();

            //Connect
            ibClient.ClientSocket.eConnect("127.0.0.1", 7497, 0);

            //Creat and define a contract to fetch data for
            Contract contract = new Contract();
            contract.Symbol = "EUR";
            contract.SecType = "CASH";
            contract.Currency = "USD";
            contract.Exchange = "IDEALPRO";

            // Create a new TagValue List object (for API version 9.71) 
            List<TagValue> mktDataOptions = new List<TagValue>();


            // calling method every X seconds
            /*
            var timer = new System.Threading.Timer(
            e => ibClient.ClientSocket.reqMktData(1, contract, "", true, mktDataOptions),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10));
            */



        }
    }
}
