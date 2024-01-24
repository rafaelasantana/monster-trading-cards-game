using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class BattleRepository
    {
        private readonly IDbConnectionManager _dbConnectionManager;
        private NpgsqlConnection _connection;
        private readonly string _battlesTable = "battles";
        private readonly string _battleLogsTable = "battleLogs";
        private readonly string _battleFields = "id, player1Id, player2Id, status, startTime, endTime, winnerId";
        private readonly string _battleLogFields = "id, battleId, roundNumber, player1CardId, player2CardId, roundResult, createdAt";

        public BattleRepository(IDbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
            _connection = _dbConnectionManager.GetConnection() as NpgsqlConnection;
        }

        public Battle? GetPendingBattle()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            string query = $"SELECT * FROM {_battlesTable} WHERE player2Id IS NULL AND status = 'pending' LIMIT 1";
            Battle? pendingBattle = null;

            using var command = new NpgsqlCommand(query, _connection as NpgsqlConnection);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                pendingBattle = DataMapperService.MapToObject<Battle>(reader);
            }

            return pendingBattle;
        }


        public int CreatePendingBattle(int? playerId)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            var query = $"INSERT INTO {_battlesTable} (player1Id, status) VALUES (@PlayerId, 'pending') RETURNING id";
            int newBattleId = 0;

            using var command = new NpgsqlCommand(query, _connection as NpgsqlConnection);

            // Adding parameters to the command
            command.Parameters.AddWithValue("@PlayerId", playerId ?? (object)DBNull.Value);

            // Executing the command and getting the new battle ID
            var result = command.ExecuteScalar();
            if (result != null)
            {
                newBattleId = Convert.ToInt32(result);
            }

            return newBattleId;
        }


        // Sets a player as player2 for a battle with a given ID and updates the battle status
        public void SetPlayerForBattle(int? battleId, int? playerId)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            string updateQuery = $"UPDATE {_battlesTable} SET player2Id = @PlayerId, status = 'ongoing' WHERE id = @BattleId AND player2Id IS NULL";
            using var command = new NpgsqlCommand(updateQuery, _connection as NpgsqlConnection);

            // Adding parameters to the command
            command.Parameters.AddWithValue("@PlayerId", playerId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BattleId", battleId ?? (object)DBNull.Value);

            // Executing the command
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("Could not set player for battle, or battle does not exist.");
            }
        }

        public Battle GetBattleById(int? battleId)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            string query = $"SELECT * FROM {_battlesTable} WHERE id = @BattleId";

            using var command = new NpgsqlCommand(query, _connection as NpgsqlConnection);
            // Adding the parameter to the command
            command.Parameters.AddWithValue("@BattleId", battleId ?? (object)DBNull.Value);

            // Executing the command
            using var reader = command.ExecuteReader();
            Battle battle = null;
            if (reader.Read())
            {
                battle = DataMapperService.MapToObject<Battle>(reader);
            }
            return battle;
        }

        public void UpdateBattleStatus(int? battleId, string newStatus)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            string updateQuery = $"UPDATE {_battlesTable} SET status = @NewStatus WHERE id = @BattleId";

            using var command = new NpgsqlCommand(updateQuery, _connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@NewStatus", newStatus);
            command.Parameters.AddWithValue("@BattleId", battleId);

            command.ExecuteNonQuery();
        }


        // // New method to save the outcome of a battle
        // public void SaveBattleOutcome(BattleResult battleResult)
        // {
        //     // Logic to save the battle result to the database
        // }

        // // New method to log a battle round
        // public void LogBattleRound(int battleId, RoundResult roundResult)
        // {
        //     // Logic to log the round details to the database
        // }

        // // New method to retrieve a deck for a user
        // public List<Card> GetDeckForUser(int userId)
        // {
        //     // Logic to retrieve the user's deck from the database
        // }

    }
}