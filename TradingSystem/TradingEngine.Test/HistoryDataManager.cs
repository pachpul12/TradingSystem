using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using IBApi;
using TradingEngine;
using TradingEngine.messages;
using Contract = IBApi.Contract;

namespace TradingEngine.Tests
{
    class HistoryDataManager
    {
        private readonly PostgresHelper postgresHelper;
        private readonly IBClient ibClient;

        public HistoryDataManager(PostgresHelper postgresHelper, IBClient ibClient) {
            this.postgresHelper = postgresHelper;
            this.ibClient = ibClient;


        }

        public void FetchHistoricalDataInChunks(
        string symbol,
        string exchange,
        string currency,
        string secType,
        string barSize,
        string whatToShow,
        DateTime startDate,
        DateTime endDate)
        {
            DateTime currentEndDate = endDate;
            DateTime currentStartDate;

            while (currentEndDate > startDate)
            {
                // Adjust to fetch one day of data per chunk
                currentStartDate = currentEndDate.AddDays(-1);
                if (currentStartDate < startDate)
                {
                    currentStartDate = startDate; // Adjust to the start date if it exceeds the range
                }

                Console.WriteLine($"Requesting data for {symbol} from {currentStartDate:yyyyMMdd} to {currentEndDate:yyyyMMdd}...");
                try
                {
                    Contract contract = new Contract{
                        Symbol = symbol,
                            SecType = secType,
                            Exchange = exchange,
                            Currency = currency
                        };

                    int reqId = this.ibClient.nextRequestId++;
                    this.ibClient.RequestIdToContract[reqId] = contract;
                    this.ibClient.RequestIdToType[reqId] = whatToShow;
                    this.ibClient.HistoryDataRequestIdCompletion[reqId] = false;

                    ibClient.ClientSocket.reqHistoricalData(
                        tickerId: reqId,
                        contract: contract,
                        endDateTime: currentEndDate.ToString("yyyyMMdd HH:mm:ss"),
                        durationStr: "1 D", // 1 day of data
                        barSizeSetting: barSize,
                        whatToShow: whatToShow,
                        useRTH: 1, // Use regular trading hours
                        formatDate: 1,
                        keepUpToDate: false,
                        null
                    );

                    // Adjust for the next chunk
                    currentEndDate = currentStartDate;

                    // Pause to respect API rate limits
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
                    break; // Exit loop if an error occurs
                }
            }

            Console.WriteLine($"Completed fetching historical data for {symbol}.");
        }


        public void FetchDataForMultipleStocks(List<string> symbols, string exchange, string currency, string secType, string barSize, string whatToShow)
        {
            List<Task> tasks = new List<Task>();

            foreach (var symbol in symbols)
            {
                //tasks.Add(Task.Run(() => FetchHistoricalDataInChunks(symbol, exchange, currency, secType, barSize, whatToShow)));
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
            CultureInfo provider = CultureInfo.InvariantCulture;
            string format = "yyyyMMdd HH:mm:ss";

            var dateArray = e.Date.Split(" ");

            string dateFinal = dateArray[0] + " " + dateArray[1];

            DateTime date = DateTime.ParseExact(dateFinal, format, provider);

            postgresHelper.InsertToHistoricalPrices(1, date, (decimal)e.Open, (decimal)e.High, (decimal)e.Low, (decimal)e.Close, e.Volume);
            Contract contract = ibClient.RequestIdToContract[e.RequestId];
            string whatToShow = ibClient.RequestIdToType[e.RequestId];

            string a = "";
        }

        void historicalDataEnd(HistoricalDataEndMessage e)
        {
            Contract contract = ibClient.RequestIdToContract[e.RequestId];
            string whatToShow = ibClient.RequestIdToType[e.RequestId];

            string a = "";
        }

        public void SaveHistoricalData(HistoricalDataMessage e)
        {
            var contract = ibClient.RequestIdToContract[e.RequestId];
            var query = @"
            INSERT INTO HistoricalPrices (Symbol, TimeStamp, Open, High, Low, Close, Volume)
            VALUES (@Symbol, @TimeStamp, @Open, @High, @Low, @Close, @Volume)";

            postgresHelper.ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("Symbol", contract.Symbol);
                cmd.Parameters.AddWithValue("TimeStamp", e.Date);
                cmd.Parameters.AddWithValue("Open", e.Open);
                cmd.Parameters.AddWithValue("High", e.High);
                cmd.Parameters.AddWithValue("Low", e.Low);
                cmd.Parameters.AddWithValue("Close", e.Close);
                cmd.Parameters.AddWithValue("Volume", e.Volume);
            });
        }
    }
}

