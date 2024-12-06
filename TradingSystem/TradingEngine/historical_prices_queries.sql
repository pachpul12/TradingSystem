SELECT stock_id, stocks.symbol, count(*)
from historical_prices
inner join stocks on stocks.id = historical_prices.stock_id
	group by stock_id, stocks.symbol
	order by stock_id desc
	limit 100;


SELECT COUNT(*) FROM historical_prices; --70797603