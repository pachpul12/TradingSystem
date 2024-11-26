using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingEngine
{
    public class TradingSignal
    {
        public string Symbol { get; set; }
        public string Action { get; set; } // "Buy", "Sell", "Hold"
        public int Quantity { get; set; }
        public decimal? TargetPrice { get; set; }
    }
}
