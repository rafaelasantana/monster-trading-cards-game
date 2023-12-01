using Npgsql;
using Microsoft.Extensions.Configuration;

namespace mtcg
{
    public class DbConnectionManager
    {
        private readonly string _connectionString = null!;

        public DbConnectionManager(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            try
            {
                connection.Open();
                using var command = new NpgsqlCommand("SELECT 1", connection);
                // ExecuteScalar executes the query and returns the first column of the first row in the result set.
                var result = command.ExecuteScalar();
                return result != null && result.Equals(1);
            }
            catch (Exception ex)
            {
                // Handle or log any exception that occurs during the connection test.
                Console.WriteLine($"Error testing database connection: {ex.Message}");
                return false;
            }
        }
    }
}