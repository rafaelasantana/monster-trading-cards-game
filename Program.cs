using Microsoft.Extensions.Configuration;
using mtcg.Data.Repositories;
using mtcg.Controllers;
using Npgsql;

namespace mtcg
{
    class Program
    {
        static void Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Retrieve the connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Create database connection
            var dbConnection = new NpgsqlConnection(connectionString);

            // Create database manager
            var dbConnectionManager = new DbConnectionManager(dbConnection);

            // Retrieve server URL from configuration
            var serverUrl = configuration["ServerUrl"] ?? "http://localhost:10001/";

            // Create HTTP server
            HttpServer server = new(serverUrl, dbConnectionManager);
        }
    }
}
