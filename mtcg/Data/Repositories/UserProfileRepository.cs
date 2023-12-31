using System.Data;
using Dapper;
using MTCG.Data.Models;

namespace MTCG.Data.Repositories
{
    public class UserProfileRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "userProfiles";
        private readonly string _fields = "userId, name, bio, image";

        public UserProfile? GetUserProfile(int? userId)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

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
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

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