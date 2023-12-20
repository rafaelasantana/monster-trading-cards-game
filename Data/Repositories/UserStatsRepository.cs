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
        private readonly string _Table = "userStats";
        private readonly string _Fields = "userId, eloRating, wins, losses, totalGamesPlayed";

        public UserStatsRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        public UserStats GetStatsByUserId(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"SELECT * FROM { _Table } WHERE userId = @UserId";
            return connection.Query<UserStats>(query, new { UserId = userId }).FirstOrDefault();
        }

        public void UpdateStats(UserStats stats)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $@"
                UPDATE { _Table }
                SET eloRating = @EloRating,
                    wins = @Wins,
                    losses = @Losses,
                    totalGamesPlayed = @TotalGamesPlayed
                WHERE userId = @UserId";

            connection.Execute(query, stats);
        }

        public void CreateStats(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"INSERT INTO { _Table } (userId) VALUES (@UserId)";
            connection.Execute(query, new { UserId = userId });
        }
    }
}