using Dapper;

namespace mtcg
{
    public class UserRepository : Repository<User>
    {
        public UserRepository(DbConnectionManager dbConnectionManager) : base(dbConnectionManager)
        {
            _Table = "users";
            _Fields = "username, password";
        }

        /// <summary>
        /// Saves a new user to the database or updates an existing user record
        /// </summary>
        /// <param name="user"></param>
        public new void Save(User user)
        {
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // check if user is not yet saved to the database
            if (user.Id == 0)
            {
                // insert new record and return the generated Id
                int generatedId = connection.QueryFirstOrDefault<int>($"INSERT INTO {_Table} ({_Fields}) VALUES (@Username, @Password) RETURNING Id", user);
                if (generatedId > 0)
                {
                    // save generated Id to the object
                    user.Id = generatedId;
                    Console.WriteLine($"Inserted new user with ID: {user.Id}");
                }
                else Console.WriteLine("Insert user failed");

            }
            else {
                // update record with new data
                int rowsAffected = connection.Execute($"UPDATE {_Table} SET username = @Username, password = @Password WHERE Id = @Id", user);
                if (rowsAffected > 0) Console.WriteLine($"updated user with ID: {user.Id}");
                else Console.WriteLine("update user failed");
            }
        }

        /// <summary>
        /// Checks if the username already exists on the database
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool UserExists(string username)
        {
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // build query to check if username exists
            string query = $"SELECT COUNT(*) FROM {_Table} WHERE username = @Username";
            var count = connection.ExecuteScalar<int>(query, new { Username = username });

            return count > 0;
        }
    }
}