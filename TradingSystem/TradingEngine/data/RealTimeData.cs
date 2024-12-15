public class RealTimeData
{
    public int RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public int Count { get; set; }
    public decimal WAP { get; set; } // Weighted Average Price
}
