using Npgsql;
using System.Data;

namespace mtcg.Data.Repositories
{
    public class DbConnectionManager(IDbConnection dbConnection) : IDbConnectionManager
    {
        private readonly string _connectionString = dbConnection.ConnectionString;
        private readonly IDbConnection? dbConnection = dbConnection;

        public IDbConnection GetConnection()
        {
            return dbConnection ?? new NpgsqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            using var connection = GetConnection();
            try
            {
                using var command = new NpgsqlCommand("SELECT 1", connection as NpgsqlConnection);
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
