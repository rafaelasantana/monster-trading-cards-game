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

        public int RequestBattle(int? playerId)
        {
            if (!playerId.HasValue)
            {
                throw new ArgumentNullException(nameof(playerId), "Player ID cannot be null.");
            }

            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var pendingBattle = connection.QueryFirstOrDefault<Battle>(
                $"SELECT id, player1Id FROM {_battlesTable} WHERE player2Id IS NULL AND status = 'pending'");

            if (pendingBattle != null && pendingBattle.Player1Id != playerId.Value)
            {
                // Update the pending battle with the current player as player2
                connection.Execute($"UPDATE {_battlesTable} SET player2Id = @PlayerId, status = 'ongoing' WHERE id = @BattleId",
                    new { PlayerId = playerId.Value, BattleId = pendingBattle.Id });
                return pendingBattle.Id;
            }
            else
            {
                // Create a new pending battle
                return CreatePendingBattle(playerId.Value);
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

        // Method to update user stats after a battle
        public void UpdateUserStatsAfterBattle(int userId, bool isWinner)
        {
            // Logic to update user stats based on the outcome of the battle
        }
    }
}