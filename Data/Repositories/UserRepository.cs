using Dapper;

namespace mtcg
{
    public class UserRepository : Repository<User>
    {
        public UserRepository(DbConnectionManager dbConnectionManager) : base(dbConnectionManager)
        {
            _Table = "users";
            _Fields = "username, password";
            // InitializeTableAndFields();
        }

        /// <summary>
        /// Saves a new user to the database or updates an existing user record
        /// </summary>
        /// <param name="user"></param>
        public new void Save(User user)
        {
            Console.WriteLine("In Save user...");
            // TODO throw exceptions instead of only printing comments
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // check if user is not yet saved to the database
            if (user.Id == 0)
            {
                Console.WriteLine("Will save new user");
                // save new user
                SaveNew(user);
            }
            else {
                // update record with new data
                // int rowsAffected = connection.Execute($"UPDATE {_Table} SET username = @Username, password = @Password WHERE Id = @Id", user);
                // if (rowsAffected > 0) Console.WriteLine($"updated user with ID: {user.Id}");
                // else Console.WriteLine("update user failed");
                Console.WriteLine("Will update user");
                Update(user, user.Id.ToString());
            }
        }

        private void SaveNew(User user)
        {
            Console.WriteLine("Will save new user");
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
            // set hashed password
            user.Password = hashedPassword;

            // insert new record and return the generated Id
            int generatedId = connection.QueryFirstOrDefault<int>($"INSERT INTO {_Table} ({_Fields}) VALUES (@Username, @Password) RETURNING Id", user);
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
        public User? GetByUsername(string username)
        {
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // build query
            string query = $"SELECT {_Fields} FROM {_Table} WHERE username = @Username";

            // execute query and retrieve result
            var result = connection.QueryFirstOrDefault<User>(query, new { Username = username});

            return result;
        }
    }
}