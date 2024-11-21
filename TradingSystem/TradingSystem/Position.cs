public class Position
{
    public string Symbol { get; set; }
    public int Quantity { get; set; }
    public double EntryPrice { get; set; }
    public DateTime EntryTime { get; set; }
    public string Side { get; set; } // "Buy" or "Sell"
}