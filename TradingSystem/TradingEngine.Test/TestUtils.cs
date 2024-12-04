using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using TradingEngine.Config;
using TradingEngine.Data;
using TradingEngine.Interfaces;

namespace TradingEngine.Test
{
    public class TestUtils
    {
        public async Task<List<PriceData>> GetHistoricalDataAsync(int stockId, DateTime startDate, DateTime endDate)
        {
            var engineConfig = EngineConfig.Load("engine_config.json");
            
            var historicalPrices = new List<PriceData>();

            using (var connection = new NpgsqlConnection(engineConfig.Database.ConnectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT timestamp, open_price, high_price, low_price, close_price, volume
            FROM historical_prices
            WHERE stock_id = @stockId
              AND timestamp BETWEEN @startDate AND @endDate
            ORDER BY timestamp ASC";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@stockId", stockId);
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@endDate", endDate);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var priceData = new PriceData
                            {
                                Timestamp = reader.GetDateTime(0),
                                Open = reader.GetDecimal(1),
                                High = reader.GetDecimal(2),
                                Low = reader.GetDecimal(3),
                                Close = reader.GetDecimal(4),
                                Volume = reader.GetInt64(5),
                                StockId = stockId
                            };

                            historicalPrices.Add(priceData);
                        }
                    }
                }
            }

            return historicalPrices;
        }

        public void RunStrategyOnHistoricalData(TradingStrategy strategy, List<PriceData> historicalData)
        {
            var marketContext = new MarketContext { HistoricalPrices = historicalData };

            foreach (var priceData in historicalData)
            {
                marketContext.HistoricalPrices.Add(priceData);
                var signal = strategy.Evaluate(marketContext);

                Console.WriteLine($"{priceData.Timestamp}: {signal.Action}");
            }
        }

        public static void RunStrategyForStock(int stockId, DateTime startDate, DateTime endDate,
            TradingStrategy strategy, PostgresHelper postgresHelper, MarketContext mockMarketContext)
        {
            {

                // Query to filter days with exactly 390 records
                string validDaysQuery = @$"
SELECT date_trunc('day', timestamp) as day
FROM historical_prices
WHERE stock_id = {stockId}
AND timestamp >= '{startDate:yyyy-MM-dd}'
  AND timestamp < '{endDate:yyyy-MM-dd}'
GROUP BY date_trunc('day', timestamp)
HAVING COUNT(*) = 390
ORDER BY day ASC;";

                DataTable validDays = postgresHelper.ExecuteQuery(validDaysQuery);

                if (validDays == null || validDays.Rows.Count == 0)
                {
                    Console.WriteLine("No valid days with 390 records found for the specified stock and date range.");
                    return;
                }


                decimal funds = 1000;
                decimal initialFunds = 1000;

                decimal profitLoss = 0;
                decimal? entryPrice = null;
                int? entryAmount = null;
                decimal? entryPriceTotal = null;
                decimal? startTimePrice = null;
                decimal? endTimePrice = null;
                List<TradingSignal> signals = new List<TradingSignal>();

                foreach (DataRow validDayRow in validDays.Rows)
                {
                    DateTime currentDay = (DateTime)validDayRow["day"];

                    // Query historical prices for the valid day
                    string dailyQuery = @$"
SELECT * 
FROM historical_prices
WHERE stock_id = {stockId}
  AND timestamp >= '{currentDay:yyyy-MM-dd}'
  AND timestamp < '{currentDay.AddDays(1):yyyy-MM-dd}'
ORDER BY timestamp ASC;";

                    DataTable dailyPrices = postgresHelper.ExecuteQuery(dailyQuery);

                    if (dailyPrices == null || dailyPrices.Rows.Count != 390)
                    {
                        // Skip days with incomplete data
                        continue;
                    }

                    mockMarketContext.HistoricalPrices.Clear(); // Reset for the new day

                    foreach (DataRow row in dailyPrices.Rows)
                    {
                        DateTime timestamp = (DateTime)row["timestamp"];
                        decimal closePrice = (decimal)row["close_price"];

                        if (startTimePrice == null)
                        {
                            startTimePrice = closePrice;
                        }

                        // Update the end time price for the last timestamp
                        endTimePrice = closePrice;

                        // Add new price data to the context
                        mockMarketContext.HistoricalPrices.Add(new PriceData
                        {
                            StockId = stockId,
                            Timestamp = timestamp,
                            Open = (decimal)row["open_price"],
                            High = (decimal)row["high_price"],
                            Low = (decimal)row["low_price"],
                            Close = closePrice,
                            Volume = Convert.ToInt64((decimal)row["volume"])
                        });

                        // Evaluate the strategy and collect signals
                        var signal = strategy.Evaluate(mockMarketContext);
                        signals.Add(signal);

                        decimal stopLoss = 0.98M;

                        if (entryPrice != null && entryPrice.Value * stopLoss >= closePrice)
                        {
                            //stop loss logic
                            signal.Action = SignalType.Sell;
                        }

                        if (signal.Action == SignalType.Buy && entryPrice == null)
                        {
                            entryPrice = closePrice; // Record entry price
                            entryAmount = (int)Math.Floor(funds / entryPrice.Value);
                            entryPriceTotal = entryAmount * entryPrice.Value;
                            funds -= entryPriceTotal.Value;
                        }
                        else if (signal.Action == SignalType.Sell && entryPrice != null)
                        {
                            // Calculate profit/loss
                            profitLoss += closePrice - entryPrice.Value;
                            entryPrice = null; // Reset entry price
                            funds += closePrice * entryAmount.Value;
                            entryAmount = null;
                        }

                        if (timestamp == new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 15, 59, 0))
                        {
                            //on last trading minute - sell all holdings
                            if (entryPrice != null)
                            {
                                profitLoss += closePrice - entryPrice.Value;
                                entryPrice = null; // Reset entry price
                                funds += closePrice * entryAmount.Value;
                            }
                        }
                    }
                }

                // Prepare data for insertion
                var signalsJson = Newtonsoft.Json.JsonConvert.SerializeObject(signals);
                var parametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    //todo - uncomment
                    //ShortTermPeriod = shortTermPeriod,
                    //LongTermPeriod = longTermPeriod,
                    //MinCrossoverThreshold = minCrossoverThreshold
                });

                // Calculate price yield
                decimal? priceYield = null;
                if (startTimePrice.HasValue && endTimePrice.HasValue)
                {
                    priceYield = endTimePrice.Value / startTimePrice.Value;
                }



                // Insert execution results into the database
                string insertQuery = @$"
INSERT INTO strategy_execution (
    strategy_name,
    single_stock_id,
    start_date,
    end_date,
    parameters,
    signals,
    profit_loss,
    start_price,
    end_price,
    price_yield
)
VALUES (
    'CombinedMACAndOBV',
    {stockId},
    '{startDate:yyyy-MM-dd}',
    '{endDate:yyyy-MM-dd}',
    '{parametersJson}'::jsonb,
    '{signalsJson}'::jsonb,
    {funds / initialFunds},
    {startTimePrice},
    {endTimePrice},
    {priceYield}
);";

                postgresHelper.ExecuteNonQuery(insertQuery);

                Console.WriteLine($"Strategy execution completed for stock {stockId}. Total P/L: {profitLoss:C}");
                Console.WriteLine($"Start Price: {startTimePrice}, End Price: {endTimePrice}, Yield: {priceYield}");
            }

        }

    }
}
