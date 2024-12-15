SELECT stock_id, stocks.symbol, count(*)
from stocks_prices
inner join stocks on stocks.id = stocks_prices.stock_id
--where timestamp > '2024-01-01'
	group by stock_id, stocks.symbol
	order by stock_id desc
	limit 20;


--SELECT COUNT(*) FROM historical_prices; --70797603
--SELECT COUNT(*) FROM stocks_prices;

--INSERT INTO stocks_prices (stock_id, timestamp, open_price, high_price, low_price, close_price, volume)
--SELECT stock_id, timestamp, open_price, high_price, low_price, close_price, volume
--FROM historical_prices;
SELECT * FROM
(SELECT 
    DATE(timestamp) AS trading_date, 
    stock_id, 
    COUNT(*) AS record_count
FROM 
    stocks_prices
WHERE timestamp > '2024-01-01'
AND timestamp < '2025-01-01'
GROUP BY 
    DATE(timestamp), stock_id
ORDER BY 
    trading_date ASC, stock_id ASC)
WHERE record_count > 390;

DELETE FROM stocks_prices where stock_id = 4170 or stock_id = 4210 or stock_id = 4164 or stock_id = 4170 or 