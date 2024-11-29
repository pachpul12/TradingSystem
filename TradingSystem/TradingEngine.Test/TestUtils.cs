using System;
using System.Collections.Generic;
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

    }
}
