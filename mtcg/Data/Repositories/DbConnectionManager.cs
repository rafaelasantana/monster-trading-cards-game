using Npgsql;
using System.Data;

namespace MTCG.Data.Repositories
{
    public class DbConnectionManager : IDbConnectionManager
    {
        private readonly string _connectionString;

        public DbConnectionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            using var connection = GetConnection();
            try
            {
                connection.Open();
                using var command = new NpgsqlCommand("SELECT 1", connection);
                var result = command.ExecuteScalar();
                return result != null && result.Equals(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing database connection: {ex.Message}");
                return false;
            }
        }
    }
}
