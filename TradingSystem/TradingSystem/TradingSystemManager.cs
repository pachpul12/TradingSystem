using System;
using System.Collections.Generic;
using System.Threading;

namespace TradingSystem.Core
{
    public class TradingSystemManager
    {
        private MarketDataService _marketDataService;
        private OrderService _orderService;
        private RiskManagementService _riskManagementService;
        private List<StrategyBase> _strategies;
        private Logger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public TradingSystemManager()
        {
            _strategies = new List<StrategyBase>();
            _logger = Logger.Instance;
        }

        /// <summary>
        /// Initialize the trading system components.
        /// </summary>
        public void Initialize()
        {
            _logger.LogInfo("Initializing trading system...");

            // Initialize services
            _marketDataService = new MarketDataService();
            _orderService = new OrderService();
            _riskManagementService = new RiskManagementService();

            // Register a sample strategy
            RegisterStrategy(new MomentumStrategy(_marketDataService, _orderService, _riskManagementService));

            _logger.LogInfo("Trading system initialized.");
        }

        /// <summary>
        /// Start the trading system and run its main loop.
        /// </summary>
        public void Start()
        {
            _logger.LogInfo("Starting trading system...");

            _cancellationTokenSource = new CancellationTokenSource();

            // Start Market Data Service
            _marketDataService.Start();

            // Start strategies
            foreach (var strategy in _strategies)
            {
                strategy.Start();
            }

            // Run main loop
            RunMainLoop(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stop the trading system gracefully.
        /// </summary>
        public void Stop()
        {
            _logger.LogInfo("Stopping trading system...");

            // Stop strategies
            foreach (var strategy in _strategies)
            {
                strategy.Stop();
            }

            // Stop Market Data Service
            _marketDataService.Stop();

            // Cancel the main loop
            _cancellationTokenSource.Cancel();

            _logger.LogInfo("Trading system stopped.");
        }

        /// <summary>
        /// Register a strategy with the trading system.
        /// </summary>
        /// <param name="strategy">The strategy to register.</param>
        public void RegisterStrategy(StrategyBase strategy)
        {
            _strategies.Add(strategy);
            _logger.LogInfo($"Strategy {strategy.GetType().Name} registered.");
        }

        /// <summary>
        /// Main loop of the trading system.
        /// </summary>
        private void RunMainLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Example of system monitoring logic
                _logger.LogInfo("Trading system main loop running...");
                Thread.Sleep(5000); // Placeholder for periodic tasks
            }
        }
    }
}
