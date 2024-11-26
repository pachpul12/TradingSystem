-- Create Exchanges table
CREATE TABLE Exchanges (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT
);

-- Create Stocks table
CREATE TABLE Stocks (
    id SERIAL PRIMARY KEY,
    symbol VARCHAR(10) NOT NULL UNIQUE,
    exchange_id INT NOT NULL,
    FOREIGN KEY (exchange_id) REFERENCES Exchanges(id)
);

-- Create historical_prices table
CREATE TABLE historical_prices (
    stock_id INTEGER NOT NULL, -- Foreign key referencing the stock
    timestamp TIMESTAMP NOT NULL, -- The timestamp for the data point
    open_price NUMERIC(18, 6) NOT NULL, -- The opening price of the stock
    high_price NUMERIC(18, 6) NOT NULL, -- The highest price of the stock
    low_price NUMERIC(18, 6) NOT NULL, -- The lowest price of the stock
    close_price NUMERIC(18, 6) NOT NULL, -- The closing price of the stock
    volume NUMERIC(18, 6) NOT NULL, -- The trading volume
    PRIMARY KEY (stock_id, timestamp), -- Composite primary key
    FOREIGN KEY (stock_id) REFERENCES stocks(id) -- Foreign key constraint
);