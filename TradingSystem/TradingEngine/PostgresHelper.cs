﻿using Npgsql;
using System;
using System.Data;

public class PostgresHelper
{
    private readonly string _connectionString;

    public PostgresHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void ExecuteNonQuery(string query, Action<NpgsqlCommand> configureCommand = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(query, connection);

        configureCommand?.Invoke(command);

        command.ExecuteNonQuery();
    }

    public T ExecuteScalar<T>(string query, Action<NpgsqlCommand> configureCommand = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(query, connection);

        configureCommand?.Invoke(command);

        return (T)command.ExecuteScalar();
    }

    public DataTable ExecuteQuery(string query, Action<NpgsqlCommand> configureCommand = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(query, connection);
        configureCommand?.Invoke(command);

        using var adapter = new NpgsqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);

        return dataTable;
    }

    public void InsertToHistoricalPrices(int stockId, DateTime timestamp, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, decimal volume)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            try
            {
                connection.Open();

                string query = @"
                        INSERT INTO stocks_prices (stock_id, timestamp, open_price, high_price, low_price, close_price, volume)
                        VALUES (@stock_id, @timestamp, @open_price, @high_price, @low_price, @close_price, @volume);
                    ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@stock_id", stockId);
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@open_price", openPrice);
                    command.Parameters.AddWithValue("@high_price", highPrice);
                    command.Parameters.AddWithValue("@low_price", lowPrice);
                    command.Parameters.AddWithValue("@close_price", closePrice);
                    command.Parameters.AddWithValue("@volume", volume);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Successfully inserted data for stock_id: {stockId} at {timestamp}.");
                    }
                    else
                    {
                        Console.WriteLine($"No rows were inserted for stock_id: {stockId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting data into HistoricalPrices: {ex.Message}");
            }
        }
    }

    public void InsertToHistoricalPricesInt5Secs(int stockId, DateTime timestamp, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, decimal volume)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            try
            {
                connection.Open();

                string query = @"
                        INSERT INTO stocks_prices_int5secs (stock_id, timestamp, open_price, high_price, low_price, close_price, volume)
                        VALUES (@stock_id, @timestamp, @open_price, @high_price, @low_price, @close_price, @volume)
                        ON CONFLICT (stock_id, timestamp) DO NOTHING;
                    ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@stock_id", stockId);
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@open_price", openPrice);
                    command.Parameters.AddWithValue("@high_price", highPrice);
                    command.Parameters.AddWithValue("@low_price", lowPrice);
                    command.Parameters.AddWithValue("@close_price", closePrice);
                    command.Parameters.AddWithValue("@volume", volume);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Successfully inserted data for stock_id: {stockId} at {timestamp}.");
                    }
                    else
                    {
                        Console.WriteLine($"No rows were inserted for stock_id: {stockId} at {timestamp}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting data into HistoricalPrices: {ex.Message}");
            }
        }
    }

    public void InsertToRealtimeStocksPrices(int stockId, DateTime timestamp, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, decimal volume, int count, decimal wap)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            try
            {
                connection.Open();

                string query = @"
                        INSERT INTO realtime_stocks_prices (stock_id, timestamp, open, high, low, close, volume, count, wap)
                        VALUES (@stock_id, @timestamp, @open, @high, @low, @close, @volume, @count, @wap);
                    ";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@stock_id", stockId);
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@open", openPrice);
                    command.Parameters.AddWithValue("@high", highPrice);
                    command.Parameters.AddWithValue("@low", lowPrice);
                    command.Parameters.AddWithValue("@close", closePrice);
                    command.Parameters.AddWithValue("@volume", volume);
                    command.Parameters.AddWithValue("@count", count);
                    command.Parameters.AddWithValue("@wap", wap);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"Successfully inserted real-time data for stock_id: {stockId} at {timestamp}.");
                    }
                    else
                    {
                        Console.WriteLine($"No rows were inserted for stock_id: {stockId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting data into RealtimeStocksPrices: {ex.Message}");
            }
        }
    }
}
