using Microsoft.Extensions.Configuration;
using mtcg.Data.Repositories;
using mtcg.Controllers;

namespace mtcg
{
    class Program
    {
        static void Main(string[] args)
        {
            // build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // create database manager
            var dbConnectionManager = new DbConnectionManager(configuration);

            // create HTTP server
            HttpServer server = new(configuration["ServerUrl"]!, dbConnectionManager);

        }
    }
}
