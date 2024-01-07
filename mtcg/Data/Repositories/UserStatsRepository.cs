using Dapper;
using MTCG.Data.Models;
using System.Data;

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
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"SELECT * FROM { _table } WHERE userId = @UserId";
            return connection.Query<UserStats>(query, new { UserId = userId }).FirstOrDefault();
        }

        /// <summary>
        /// Updates the user stats
        /// </summary>
        /// <param name="stats"></param>
        public void UpdateStats(UserStats stats)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $@"
                UPDATE { _table }
                SET eloRating = @EloRating,
                    wins = @Wins,
                    losses = @Losses,
                    totalGamesPlayed = @TotalGamesPlayed
                WHERE userId = @UserId";

            connection.Execute(query, stats);
        }

        /// <summary>
        /// Creates a new stats record with the user id and default values
        /// </summary>
        /// <param name="userId"></param>
        public void CreateStats(int? userId)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO { _table } (userId) VALUES (@UserId)";
            connection.Execute(query, new { UserId = userId });
        }

        /// <summary>
        /// Returns the scoreboard
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Scoreboard>? GetScoreboardData()
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection.Query<Scoreboard>("SELECT username, elorating FROM scoreboard;");
        }
    }
}