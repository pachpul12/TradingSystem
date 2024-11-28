using System;
using System.IO;
using Newtonsoft.Json;

namespace TradingEngine.Config
{
    public class EngineConfig
    {
        public DatabaseConfig Database { get; set; }
        public LoggingConfig Logging { get; set; }
        public StrategiesConfig Strategies { get; set; }
        public MarketDataConfig MarketData { get; set; }

        public static EngineConfig Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<EngineConfig>(json);
        }
    }

    public class DatabaseConfig
    {
        public string ConnectionString { get; set; }
    }

    public class LoggingConfig
    {
        public string LogLevel { get; set; }
        public string LogFilePath { get; set; }
    }

    public class StrategiesConfig
    {
        public MovingAverageCrossoverConfig MovingAverageCrossover { get; set; }
    }

    public class MovingAverageCrossoverConfig
    {
        public int ShortTermPeriod { get; set; }
        public int LongTermPeriod { get; set; }
        public decimal MinCrossoverThreshold { get; set; }
    }

    public class MarketDataConfig
    {
        public int PollingIntervalSeconds { get; set; }
    }
}
