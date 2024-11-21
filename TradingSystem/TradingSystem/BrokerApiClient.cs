using System;
using System.Collections.Concurrent;
using IBApi;

namespace TradingSystem.Core
{
    public class BrokerApiClient //: EWrapper
    {
        public EClientSocket ClientSocket { get; private set; }
        private readonly EReaderMonitorSignal _signal;
        private readonly ConcurrentDictionary<int, Action<MarketData>> _marketDataCallbacks;

        private int _nextRequestId; // Field to track the next unique request ID

        public BrokerApiClient()
        {
            _signal = new EReaderMonitorSignal();
            //ClientSocket = new EClientSocket(this, _signal);
            _marketDataCallbacks = new ConcurrentDictionary<int, Action<MarketData>>();
            _nextRequestId = 1; // Initialize the request ID
        }

        /// <summary>
        /// Connects to Interactive Brokers' Trader Workstation (TWS) or IB Gateway.
        /// </summary>
        public void Connect()
        {
            System.Console.WriteLine("Connecting to Interactive Brokers...");
            ClientSocket.eConnect("127.0.0.1", 7497, 0);

            if (ClientSocket.IsConnected())
            {
                System.Console.WriteLine("Connected to Interactive Brokers.");
                StartReader();
            }
            else
            {
                System.Console.WriteLine("Failed to connect to Interactive Brokers.");
            }
        }

        /// <summary>
        /// Disconnects from Interactive Brokers.
        /// </summary>
        public void Disconnect()
        {
            System.Console.WriteLine("Disconnecting from Interactive Brokers...");
            ClientSocket.eDisconnect();
        }

        /// <summary>
        /// Subscribe to real-time market data for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol to subscribe to.</param>
        /// <param name="callback">The callback to handle market data updates.</param>
        public void SubscribeToSymbol(string symbol, Action<MarketData> callback)
        {
            int requestId = _nextRequestId++; // Increment the request ID for uniqueness

            // Create the contract for the symbol
            var contract = new Contract
            {
                Symbol = symbol,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };

            // Store the callback for this request ID
            _marketDataCallbacks[requestId] = callback;

            // Request market data
            ClientSocket.reqMktData(requestId, contract, "", false, false, null);

            System.Console.WriteLine($"Subscribed to market data for {symbol} (Request ID: {requestId})");
        }

        /// <summary>
        /// Unsubscribe from real-time market data for a symbol.
        /// </summary>
        /// <param name="requestId">The request ID to unsubscribe from.</param>
        public void UnsubscribeFromSymbol(int requestId)
        {
            if (_marketDataCallbacks.ContainsKey(requestId))
            {
                ClientSocket.cancelMktData(requestId);
                _marketDataCallbacks.TryRemove(requestId, out _);
                System.Console.WriteLine($"Unsubscribed from market data (Request ID: {requestId})");
            }
            else
            {
                System.Console.WriteLine($"No active subscription found for Request ID: {requestId}");
            }
        }

        /// <summary>
        /// Starts a thread to process incoming messages from Interactive Brokers.
        /// </summary>
        private void StartReader()
        {
            var reader = new EReader(ClientSocket, _signal);
            reader.Start();

            new System.Threading.Thread(() =>
            {
                while (ClientSocket.IsConnected())
                {
                    _signal.waitForSignal();
                    reader.processMsgs();
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        // EWrapper Methods (Partial Implementation)

        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            if (_marketDataCallbacks.TryGetValue(tickerId, out var callback))
            {
                var marketData = new MarketData
                {
                    Symbol = $"Ticker-{tickerId}", // Replace with a proper mapping
                    LastPrice = price,
                    Timestamp = DateTime.UtcNow
                };
                callback(marketData);
            }
        }

        public void tickSize(int tickerId, int field, int size) { }
        public void error(Exception e) => System.Console.WriteLine($"Error: {e.Message}");
        public void error(int id, int errorCode, string errorMsg) => System.Console.WriteLine($"Error (ID={id}, Code={errorCode}): {errorMsg}");
        public void connectionClosed() => System.Console.WriteLine("Connection closed.");
        public void nextValidId(int orderId)
        {
            _nextRequestId = orderId; // Update the request ID to ensure it is in sync with the broker
            System.Console.WriteLine($"Next valid request ID: {_nextRequestId}");
        }
        public int NextOrderId
        {
            get { return _nextRequestId; }
            set { _nextRequestId = value; }
        }
    }
}
