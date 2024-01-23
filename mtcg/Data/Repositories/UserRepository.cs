using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class UserRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "users";
        private readonly string _fields = "id, username, password, coins";

        /// <summary>
        /// Saves a new user to the database or updates an existing user record
        /// </summary>
        /// <param name="user"></param>
        public void Save(User user)
        {
            // Check if the username already exists in the database
            var existingUser = GetByUsername(user.Username);
            if (existingUser == null)
            {
                SaveNew(user);
            }
            else
            {
                Update(user);
            }
        }

        /// <summary>
        /// Saves a new user to the database
        /// </summary>
        /// <param name="user"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void SaveNew(User user)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
            // set hashed password
            user.Password = hashedPassword;
            // set initial coins to 20
            user.Coins = 20;

            // insert new record and return the generated Id
            using var insertCommand = new NpgsqlCommand($"INSERT INTO {_table} (username, password, coins) VALUES (@Username, @Password, @Coins) RETURNING Id", connection as NpgsqlConnection);
            insertCommand.Parameters.AddWithValue("@Username", user.Username!);
            insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
            insertCommand.Parameters.AddWithValue("@Coins", 20); // Initial coins set to 20

            var result = insertCommand.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int generatedId) && generatedId > 0)
            {
                user.Id = generatedId;
            }
            else throw new InvalidOperationException("Failed to insert the new user.");

        }

        /// <summary>
        /// Returns the user with this username or null
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public User? GetByUsername(string? username)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // execute query and retrieve result
            using var selectCommand = new NpgsqlCommand($"SELECT {_fields} FROM {_table} WHERE username = @Username", connection as NpgsqlConnection);
            selectCommand.Parameters.AddWithValue("@Username", username!);

            User? user = null;
            using var reader = selectCommand.ExecuteReader();
            if (reader.Read())
            {
                user = DataMapperService.MapToObject<User>(reader);
            }
            return user;
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user"></param>
        public void Update(User user)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Update user record with new data
            var query = $"UPDATE {_table} SET username = @Username, password = @Password, coins = @Coins WHERE id = @Id";
            using var updateCommand = new NpgsqlCommand($"UPDATE {_table} SET username = @Username, password = @Password, coins = @Coins WHERE id = @Id", connection as NpgsqlConnection);
            updateCommand.Parameters.AddWithValue("@Username", user.Username!);
            updateCommand.Parameters.AddWithValue("@Password", user.Password!);
            updateCommand.Parameters.AddWithValue("@Coins", user.Coins!);
            updateCommand.Parameters.AddWithValue("@Id", user.Id!);

            int rowsAffected = updateCommand.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Update failed: No user found with ID {user.Id}.");
            }
        }

    }
}