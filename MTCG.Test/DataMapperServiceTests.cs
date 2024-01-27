using MTCG.Data.Models;
using MTCG.Data.Repositories;
using MTCG.Data.Services;
using Npgsql;
using System.Data;

namespace MTCG.Test
{
    public class DataMapperServiceTests
    {
        private DbConnectionManager _dBConnectionManager;

        [SetUp]
        public void Setup()
        {
            _dBConnectionManager = new DbConnectionManager("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");        
        }

        /// <summary>
        /// Tests object mapping from users table to User model
        /// </summary>
        [Test]
        public void MapToObject_WithValidDataReader_ShouldMapToObjectOfTypeT()
        {
            using var connection = _dBConnectionManager.GetConnection();
            connection.Open();

            // Arrange - Create a sample data reader with known data
                // Insert sample user
            using var insertCommand = new NpgsqlCommand("INSERT INTO users (username, password, coins) VALUES ('sampleUser', 'samplePassword', 100);", connection);
            insertCommand.ExecuteNonQuery();
                // Select inserted user
            using var selectCommand = new NpgsqlCommand("SELECT * FROM users WHERE username = 'sampleUser'", connection);

            // Act
            User? user = null;
                // Read query result
            using var reader = selectCommand.ExecuteReader();
            if (reader.Read())
            {
                // Map user
                user = DataMapperService.MapToObject<User>(reader);
            }

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(user, Is.Not.Null, "Mapped user should not be null");
                Assert.That(user?.Username, Is.EqualTo("sampleUser"), "Username should match");
                Assert.That(user?.Password, Is.EqualTo("samplePassword"), "Password should match");
                Assert.That(user?.Coins, Is.EqualTo(100), "Coins should match");
            });
        }

        /// <summary>
        /// Clear all database tables
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            // Call your clear_all_tables function here
            ClearAllTables();
        }

        private void ClearAllTables()
        {
            using var connection = _dBConnectionManager.GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand("SELECT clear_all_tables()", connection);
            command.ExecuteNonQuery();
        }
    }
}