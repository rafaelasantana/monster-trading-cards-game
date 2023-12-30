using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using MTCG.Data.Models;

namespace MTCG.Data.Repositories
{
    public class BattleRepository
    {
        private readonly IDbConnectionManager _dbConnectionManager;
        private readonly string _battlesTable = "battles";
        private readonly string _battleLogsTable = "battleLogs";
        private readonly string _battleFields = "id, player1Id, player2Id, status, startTime, endTime, winnerId";
        private readonly string _battleLogFields = "id, battleId, roundNumber, player1CardId, player2CardId, roundResult, createdAt";

        public BattleRepository(IDbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        // Retrieves the first pending battle that doesn't have a second player set
        public Battle GetPendingBattle()
        {
            using (var connection = _dbConnectionManager.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                string query = $"SELECT * FROM {_battlesTable} WHERE player2Id IS NULL AND status = 'pending' LIMIT 1";

                var pendingBattle = connection.QueryFirstOrDefault<Battle>(query);
                return pendingBattle;
            }
        }


        public int CreatePendingBattle(int? playerId)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO {_battlesTable} (player1Id, status) VALUES (@PlayerId, 'pending') RETURNING id";

            // Execute the query and return the new battle's ID
            int newBattleId = connection.QuerySingle<int>(query, new { PlayerId = playerId });
            return newBattleId;
        }

        // Sets a player as player2 for a battle with a given ID and updates the battle status
        public void SetPlayerForBattle(int battleId, int playerId)
        {
            using (var connection = _dbConnectionManager.GetConnection())
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                string updateQuery = $"UPDATE {_battlesTable} SET player2Id = @PlayerId, status = 'ongoing' WHERE id = @BattleId AND player2Id IS NULL";

                int rowsAffected = connection.Execute(updateQuery, new { PlayerId = playerId, BattleId = battleId });

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("Could not set player for battle, or battle does not exist.");
                }
            }
        }

        // New method to save the outcome of a battle
        public void SaveBattleOutcome(BattleResult battleResult)
        {
            // Logic to save the battle result to the database
        }

        // New method to log a battle round
        public void LogBattleRound(int battleId, RoundResult roundResult)
        {
            // Logic to log the round details to the database
        }

        // New method to retrieve a deck for a user
        public List<Card> GetDeckForUser(int userId)
        {
            // Logic to retrieve the user's deck from the database
        }

    }
}