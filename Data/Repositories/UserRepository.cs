using System.Data;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class UserRepository(IDbConnectionManager dbConnectionManager, UserStatsRepository userStatsRepository, UserProfileRepository userProfileRepository)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly UserStatsRepository _userStatsRepository = userStatsRepository;
        private readonly UserProfileRepository _userProfileRepository = userProfileRepository;
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
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // build query
            string query = $"SELECT {_fields} FROM {_table} WHERE username = @Username";

            // execute query and retrieve result
            var result = connection.QueryFirstOrDefault<User>(query, new { Username = username});

            // connection.Close();
            return result;
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
            var rowsAffected = connection.Execute(query, user);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Update failed: No user found with ID {user.Id}.");
            }
        }

        /// <summary>
        /// Verifies the credentials for a registered user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public User LoginUser(string username, string password)
        {
            var user = GetByUsername(username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                throw new InvalidOperationException("Invalid username or password.");
            }
            return user;
        }

        /// <summary>
        /// Registers a new user, creates a user profile and user stats
        /// </summary>
        /// <param name="newUser"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterUser(User newUser)
        {
            if (string.IsNullOrWhiteSpace(newUser.Username))
            {
                throw new ArgumentException("Username cannot be empty.");
            }

            if (GetByUsername(newUser.Username) != null)
            {
                throw new InvalidOperationException("Username already exists!");
            }

            // Hash password if necessary
            newUser.Password = BCrypt.Net.BCrypt.HashPassword(newUser.Password);

            Save(newUser); // Save the new user

            // create new user
            UserProfile newUserProfile = new(newUser.Id, null, null, null);
            _userProfileRepository.CreateUserProfile(newUserProfile);

            // create a user stats record
            _userStatsRepository.CreateStats(newUser.Id);
        }
    }
}