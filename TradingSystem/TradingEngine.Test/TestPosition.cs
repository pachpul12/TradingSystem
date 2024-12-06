using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingEngine.Test
{
    internal class TestPosition
    {
        public int StockId;
        public int Quantity;
        public bool IsOpen;
        public decimal BuyPrice;
        public decimal? SellPrice;
        public DateTime BuyDate;
        public DateTime? SellDate;
        

    }
}
