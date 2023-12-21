using Dapper;
using mtcg.Data.Models;
using System;
using System.Data;
using System.Linq;

namespace mtcg.Data.Repositories
{
    public class UserStatsRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _table = "userStats";
        private readonly string _fields = "userId, eloRating, wins, losses, totalGamesPlayed";

        public UserStatsRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Returns the stats for this user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UserStats GetStatsByUserId(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"SELECT * FROM { _table } WHERE userId = @UserId";
            return connection.Query<UserStats>(query, new { UserId = userId }).FirstOrDefault();
        }

        /// <summary>
        /// Updates the user stats
        /// </summary>
        /// <param name="stats"></param>
        public void UpdateStats(UserStats stats)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

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
        public void CreateStats(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"INSERT INTO { _table } (userId) VALUES (@UserId)";
            connection.Execute(query, new { UserId = userId });
        }

        /// <summary>
        /// Returns the scoreboard
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Scoreboard> GetScoreboardData()
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            return connection.Query<Scoreboard>("SELECT username, elorating FROM scoreboard;");
        }
    }
}