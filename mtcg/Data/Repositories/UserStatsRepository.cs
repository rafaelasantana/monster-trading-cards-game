using MTCG.Data.Models;
using MTCG.Data.Services;
using System.Data;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class UserStatsRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "userStats";

        /// <summary>
        /// Returns the stats for this user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UserStats? GetStatsByUserId(int? userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"SELECT * FROM {_table} WHERE userId = @UserId";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);

            UserStats? userStats = null;
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                userStats = DataMapperService.MapToObject<UserStats>(reader);
            }
            return userStats;
        }


        /// <summary>
        /// Updates the user stats
        /// </summary>
        /// <param name="stats"></param>
        public void UpdateStats(UserStats stats)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $@"
                UPDATE {_table}
                SET eloRating = @EloRating,
                    wins = @Wins,
                    losses = @Losses,
                    totalGamesPlayed = @TotalGamesPlayed
                WHERE userId = @UserId";

            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@EloRating", stats.EloRating!);
            command.Parameters.AddWithValue("@Wins", stats.Wins!);
            command.Parameters.AddWithValue("@Losses", stats.Losses!);
            command.Parameters.AddWithValue("@TotalGamesPlayed", stats.TotalGamesPlayed!);
            command.Parameters.AddWithValue("@UserId", stats.UserId!);

            command.ExecuteNonQuery();
        }


        /// <summary>
        /// Creates a new stats record with the user id and default values
        /// </summary>
        /// <param name="userId"></param>
        public void CreateStats(int? userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO {_table} (userId) VALUES (@UserId)";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@UserId", userId ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }


        /// <summary>
        /// Returns the scoreboard
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Scoreboard>? GetScoreboardData()
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = "SELECT username, elorating, wins, losses FROM scoreboard;";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);

            var scoreboards = new List<Scoreboard>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var scoreboard = DataMapperService.MapToObject<Scoreboard>(reader);
                scoreboards.Add(scoreboard);
            }

            return scoreboards;
        }

    }
}