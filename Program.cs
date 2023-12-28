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
            // create database connection
            var dbConnection = new NpgsqlConnection("Host=localhost;Port=5433;Database=mtcgdb;Username=mtcguser;Password=mtcgpassword;");

            // create database manager
            var dbConnectionManager = new DbConnectionManager(dbConnection);

            // create HTTP server
            HttpServer server = new("http://localhost:10001/", dbConnectionManager);

        }
    }
}
