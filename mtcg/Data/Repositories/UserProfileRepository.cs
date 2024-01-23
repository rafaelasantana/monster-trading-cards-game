using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class UserProfileRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "userProfiles";
        private readonly string _fields = "userId, name, bio, image";

        /// <summary>
        /// Retrieves the user profile associated with this user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UserProfile? GetUserProfile(int? userId)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"SELECT * FROM {_table} WHERE userId = @UserId;";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);

            UserProfile? userProfile = null;
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                userProfile = DataMapperService.MapToObject<UserProfile>(reader);
            }

            return userProfile;
        }

        /// <summary>
        /// Creates a new user profile record on the database
        /// </summary>
        /// <param name="userProfile"></param>
        public void CreateUserProfile(UserProfile userProfile)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO {_table} ({_fields}) VALUES (@UserId, @Name, @Bio, @Image);";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@UserId", userProfile.UserId);
            command.Parameters.AddWithValue("@Name", userProfile.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Bio", userProfile.Bio ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Image", userProfile.Image ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }


        public void UpdateUserProfile(int? userId, UserProfile updatedProfile)
        {
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
                using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
                command.Parameters.AddWithValue("@Name", updatedProfile.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Bio", updatedProfile.Bio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Image", updatedProfile.Image ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UserId", userId);

                command.ExecuteNonQuery();
            }
        }


    }
}