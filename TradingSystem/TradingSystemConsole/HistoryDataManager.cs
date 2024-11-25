using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IBApi;
using TradingSystem;
using TradingSystem.messages;

namespace TradingSystemConsole
{
    class HistoryDataManager
    {
        private IBClient ibClient;
        private EReaderMonitorSignal signal;

        public HistoryDataManager(IBClient iBClient, EReaderMonitorSignal signal) {
            this.ibClient = iBClient;
            this.signal = signal;


            
        }

        public void FetchHistoricalDataInChunks(string symbol, string exchange, string currency, string secType, string barSize, string whatToShow)
        {
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddDays(-1); // Fetch 1 day at a time
            DateTime targetDate = endDate.AddYears(-1); // Target is 5 years of data

            while (endDate > targetDate)
            {
                try
                {
                    Console.WriteLine($"Requesting data from {startDate:yyyyMMdd} to {endDate:yyyyMMdd} for {symbol}.");
                    ibClient.GetHistoricalDataForSymbol(
                        symbol,
                        exchange,
                        currency,
                        $"{(endDate - startDate).TotalDays} D",
                        barSize,
                        secType,
                        endDate.ToString("yyyyMMdd HH:mm:ss"),
                        whatToShow
                    );

                    // Adjust dates for the next batch
                    endDate = startDate;
                    startDate = startDate.AddDays(-1);

                    // Throttle requests to avoid hitting IB's rate limit
                    Thread.Sleep(1000);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
                }
            }

            Console.WriteLine($"Completed fetching historical data for {symbol}.");
        }

        public void FetchDataForMultipleStocks(List<string> symbols, string exchange, string currency, string secType, string barSize, string whatToShow)
        {
            List<Task> tasks = new List<Task>();

            foreach (var symbol in symbols)
            {
                tasks.Add(Task.Run(() => FetchHistoricalDataInChunks(symbol, exchange, currency, secType, barSize, whatToShow)));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Completed fetching data for all symbols.");
        }




        public void GetDataForSymbol(DateTime from, DateTime to, string symbol, string exchange)
        {
            DateTime iterator = from;

            while (iterator <= to)
            {
                ibClient.GetHistoricalDataForSymbol(symbol, exchange, "USD", "1 D", "5 secs", "STK", to.ToString("yyyyMMdd HH:mm:ss"), "BID_ASK");

                //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "5 secs", "STK", "20241122 23:59:59", "TRADES");

                iterator = iterator.AddDays(1);
            }


            string a = "";

            //ibClient.GetHistoricalDataForSymbol("NVDA", "NASDAQ", "USD", "1 D", "5 secs", "STK", "20241121 23:59:59", "TRADES");

        }

        public void InitEvents()
        {
            ibClient.HistoricalData += historicalData;
            ibClient.HistoricalDataEnd += historicalDataEnd;
            ibClient.HistoricalDataAllEnded += historicalDataAllEnded;
        }

        public void historicalDataAllEnded(object sender, EventArgs e)
        {

        }

        public void historicalData(HistoricalDataMessage e)
        {
            Contract contract = ibClient.RequestIdToContract[e.RequestId];
            string whatToShow = ibClient.RequestIdToType[e.RequestId];

            string a = "";
        }

        public void historicalDataEnd(HistoricalDataEndMessage e)
        {
            Contract contract = ibClient.RequestIdToContract[e.RequestId];
            string whatToShow = ibClient.RequestIdToType[e.RequestId];

            string a = "";
        }
    }
}
