using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTCG.Data.Repositories
{
    public class BattleRepository
    {
        private readonly IDbConnectionManager _dbConnectionManager;
        private readonly string _battlesTable = "battles";
        private readonly string _battleLogsTable = "battleLogs";
        private readonly string _battleFields = "id, player1Id, player2Id, status, startTime, endTime, winnerId";
        private readonly string _battleLogFields = "id, battleId, roundNumber, player1CardId, player2CardId, roundResult, createdAt";

        public BattlesRepository(IDbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        public int RequestBattle(int playerId)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Check for existing pending battle
            var pendingBattleId = connection.QueryFirstOrDefault<int>(
                $"SELECT id FROM {_battlesTable} WHERE player2Id IS NULL AND status = 'pending'");

            if (pendingBattleId > 0)
            {
                // Update the pending battle with the current player as player2
                connection.Execute($"UPDATE {_battlesTable} SET player2Id = @PlayerId, status = 'ongoing' WHERE id = @BattleId",
                    new { PlayerId = playerId, BattleId = pendingBattleId });
                return pendingBattleId;
            }
            else
            {
                // Create a new pending battle
                return CreatePendingBattle(playerId);
            }
        }

        public int CreatePendingBattle(int playerId)
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
    }
}