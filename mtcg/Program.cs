using Microsoft.Extensions.Configuration;
using MTCG.Data.Repositories;
using MTCG.Controllers;
using Npgsql;

namespace MTCG
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

            // Keep the main thread alive
            ManualResetEvent quitEvent = new(false);
            Console.CancelKeyPress += (sender, eArgs) => {
                quitEvent.Set();
                eArgs.Cancel = true;
            };

            // Wait here until CTRL-C is received.
            Console.WriteLine("Server is running. Press CTRL-C to stop.");
            quitEvent.WaitOne();

            // Stop the server before exiting
            server.Stop();
        }
    }
}
