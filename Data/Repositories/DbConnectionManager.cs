using Npgsql;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class DbConnectionManager
    {
        private readonly string _connectionString = null!;

        public DbConnectionManager(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            // Register type handlers
            // SqlMapper.AddTypeHandler(new ElementTypeHandler());
            // SqlMapper.AddTypeHandler(new CardTypeHandler());
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

    // public class ElementTypeHandler : SqlMapper.TypeHandler<Element>
    // {
    //     public override void SetValue(IDbDataParameter parameter, Element value)
    //     {
    //         parameter.Value = value.ToString();
    //     }

    //     public override Element Parse(object value)
    //     {
    //         return (Element)Enum.Parse(typeof(Element), (string)value);
    //     }
    // }

    // public class CardTypeHandler : SqlMapper.TypeHandler<CardType>
    // {
    //     public override void SetValue(IDbDataParameter parameter, CardType value)
    //     {
    //         parameter.Value = value.ToString();
    //     }

    //     public override CardType Parse(object value)
    //     {
    //         return (CardType)Enum.Parse(typeof(CardType), (string)value);
    //     }
    // }
}