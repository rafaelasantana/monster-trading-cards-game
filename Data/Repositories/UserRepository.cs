using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class UserRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _table = "users";
        private readonly string _fields = "id, username, password, coins";
        public UserRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Saves a new user to the database or updates an existing user record
        /// </summary>
        /// <param name="user"></param>
        public void Save(User user)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

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
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
            // set hashed password
            user.Password = hashedPassword;
            // set initial coins to 20
            user.Coins = 20;

            // insert new record and return the generated Id
            int generatedId = connection.QueryFirstOrDefault<int>($"INSERT INTO {_table} (username, password, coins) VALUES (@Username, @Password, @Coins) RETURNING Id", user);
            if (generatedId > 0)
            {
                // save generated Id to the object
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
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // build query
            string query = $"SELECT {_fields} FROM {_table} WHERE username = @Username";

            // execute query and retrieve result
            var result = connection.QueryFirstOrDefault<User>(query, new { Username = username});

            return result;
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="user"></param>
        public void Update(User user)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Update user record with new data
            var query = $"UPDATE {_table} SET username = @Username, password = @Password, coins = @Coins WHERE id = @Id";
            var rowsAffected = connection.Execute(query, user);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Update failed: No user found with ID {user.Id}.");
            }
        }

    }
}