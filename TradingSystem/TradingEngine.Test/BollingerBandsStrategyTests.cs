using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using TradingEngine.Config;
using TradingEngine.Interfaces;

namespace TradingEngine.Test
{
    [TestFixture]
    public class BollingerBandsStrategyTests
    {
        private BollingerBandsStrategy _strategy;
        private MarketContext _mockMarketContext;
        private IOrderManagementService _orderManagementService;

        private EngineConfig _engineConfig;
        private string _connectionString;
        private PostgresHelper _postgresHelper;

        [SetUp]
        public void SetUp()
        {
            _engineConfig = EngineConfig.Load("engine_config.json");
            _connectionString = _engineConfig.Database.ConnectionString;
            _postgresHelper = new PostgresHelper(_connectionString);

            _mockMarketContext = new MarketContext();

            Mock<IOrderManagementService> mockObject = new Mock<IOrderManagementService>();
            mockObject.Setup(m => m.PlaceOrder("", "", "", "", "", 0, 0));

            _orderManagementService = mockObject.Object;
            _strategy = new BollingerBandsStrategy(_mockMarketContext, _orderManagementService, 20, 2);
        }

        private Dictionary<string, Dictionary<int, TestPosition>> _dictStockPositionsByDayByStock = new Dictionary<string, Dictionary<int, TestPosition>>();
        private List<TestPosition> _testPositions = new List<TestPosition>();

        [Test]
        [TestCase(["2023-11-23", "2024-11-23", 0, 20, 2.0])]
        [TestCase(["2022-11-23", "2023-11-23", 0, 20, 2.0])]
        [TestCase(["2021-11-23", "2022-11-23", 0, 20, 2.0])]
        [TestCase(["2020-11-23", "2021-11-23", 0, 20, 2.0])]
        [TestCase(["2019-11-23", "2020-11-23", 0, 20, 2.0])]
        public void RunStrategyOnOneYearData_IntradayTrading_MultipleStocks(
           string startDateStr, string endDateStr, int stockId,
           int periods, double multiplier)
        {

            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);
            decimal multiplierDecimal = (decimal)multiplier;

            // Input parameters
            if (stockId > 0)
            {
                RunStrategyForStock(stockId, startDate, endDate, periods, multiplierDecimal);
            }
            else
            {
                string sqlActiveStocks = "SELECT * FROM stocks WHERE active = true ORDER BY Id ASC";

                DataTable activeStocks = _postgresHelper.ExecuteQuery(sqlActiveStocks);

                if (activeStocks == null || activeStocks.Rows.Count == 0)
                {
                    throw new Exception("no active stocks found");
                }

                //run on all stocks
                foreach (DataRow activeStock in activeStocks.Rows)
                {
                    int id = (int)activeStock["id"];
                    bool active = (bool)activeStock["active"];

                    if (!active)
                    {
                        continue;
                    }

                    RunStrategyForStock(id, startDate, endDate, periods, multiplierDecimal);
                }
            }
        }



        public void RunStrategyForStock(int stockId, DateTime startDate, DateTime endDate, int periods, decimal multiplier)
        {

            _strategy = new BollingerBandsStrategy(_mockMarketContext, _orderManagementService, periods, multiplier);

            TestUtils.RunStrategyForStock(stockId, startDate, endDate, _strategy, _postgresHelper, _mockMarketContext);
        }
    }
}
