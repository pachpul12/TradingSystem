using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingEngine.Data;

namespace TradingEngine.Interfaces
{
    public interface IMarketDataService
    {
        IEnumerable<PriceData> GetHistoricalData(string symbol, DateTime startDate, DateTime endDate);

        PriceData GetRealtimeData(string symbol);
    }
}
