using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using TradingEngine.Interfaces;

namespace TradingEngine
{
    public class Engine
    {
        private readonly IMarketDataService _marketDataService;
        private readonly IOrderManagementService _orderService;
        private readonly IRiskManagementService _riskManagementService;
        private readonly IEnumerable<TradingStrategy> _strategies;
        private readonly ILogger _logger;
        private bool _isRunning;

        public Engine(
            IMarketDataService marketDataService,
            IOrderManagementService orderService,
            IRiskManagementService riskManagementService,
            IEnumerable<TradingStrategy> strategies,
            ILogger logger)
        {
            _marketDataService = marketDataService;
            _orderService = orderService;
            _riskManagementService = riskManagementService;
            _strategies = strategies;
            _logger = logger;
        }

        public void Start()
        {
            _isRunning = true;
            Task.Run(() => TradingLoop());
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void TradingLoop()
        {
            while (_isRunning)
            {
                foreach (var strategy in _strategies)
                {
                    var context = new MarketContext
                    {
                        // Populate market data and context as needed
                    };

                    var signal = strategy.Evaluate(context);

                    //if (signal != null && _riskManagementService.EvaluateTrade(signal))
                    //{
                    //    _logger.LogInfo($"Executing trade for strategy: {strategy.GetName()}");
                    //    _orderService.PlaceOrder(signal.Order);
                    //}
                }

                Thread.Sleep(5000); // Adjust for desired trading loop frequency
            }
        }
    }
}