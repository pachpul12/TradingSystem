SELECT id, symbol, exchange_id, active
	FROM public.stocks
	order by id desc;

-- update public.stocks
-- set active = false
-- where id = 4209;