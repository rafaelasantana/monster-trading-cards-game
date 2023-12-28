using System.Data;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class UserProfileRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _table = "userProfiles";
        private readonly string _fields = "userId, name, bio, image";

        public UserProfileRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        public UserProfile? GetUserProfile(int? userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            return connection.QueryFirstOrDefault<UserProfile>(
                $"SELECT * FROM { _table } WHERE userId = @UserId;",
                new { UserId = userId });
        }

        public void CreateUserProfile(UserProfile userProfile)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO { _table } ({ _fields }) VALUES (@UserId, @Name, @Bio, @Image);";
            connection.Execute(query, userProfile);
        }

        public void UpdateUserProfile(int? userId, UserProfile updatedProfile)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Check if the profile already exists
            var existingProfile = GetUserProfile(userId);
            if (existingProfile == null)
            {
                // Create a new profile if it doesn't exist
                CreateUserProfile(new UserProfile(userId, updatedProfile.Name, updatedProfile.Bio, updatedProfile.Image));
            }
            else
            {
                // Update existing profile
                var query = $"UPDATE {_table} SET name = @Name, bio = @Bio, image = @Image WHERE userId = @UserId;";
                connection.Execute(query, new { updatedProfile.Name, updatedProfile.Bio, updatedProfile.Image, UserId = userId });
            }
        }

    }
}