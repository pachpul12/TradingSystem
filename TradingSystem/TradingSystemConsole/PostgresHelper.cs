using Npgsql;
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
                        INSERT INTO historical_prices (stock_id, timestamp, open_price, high_price, low_price, close_price, volume)
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
}
