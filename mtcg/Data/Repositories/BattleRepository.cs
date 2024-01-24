using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class BattleRepository
    {
        private readonly IDbConnectionManager _dbConnectionManager;
        private readonly string _battlesTable = "battles";

        public BattleRepository(IDbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Retrieves a pending battle
        /// </summary>
        /// <returns></returns>
        public Battle? GetPendingBattle()
        {
            try
            {
                using var connection = _dbConnectionManager.GetConnection() as NpgsqlConnection;
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                string query = $"SELECT * FROM {_battlesTable} WHERE player2Id IS NULL AND status = 'pending' LIMIT 1";
                Battle? pendingBattle = null;

                using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    pendingBattle = DataMapperService.MapToObject<Battle>(reader);
                }

                return pendingBattle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPendingBattle: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Creates a new battle with this player as player 1, sets the battle status to pending
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public int CreatePendingBattle(int? playerId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO {_battlesTable} (player1Id, status) VALUES (@PlayerId, 'pending') RETURNING id";
            int newBattleId = 0;

            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);

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


        /// <summary>
        /// Sets a player as player2 for a battle with a given ID and updates the battle status
        /// </summary>
        /// <param name="battleId"></param>
        /// <param name="playerId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetPlayerForBattle(int? battleId, int? playerId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            string updateQuery = $"UPDATE {_battlesTable} SET player2Id = @PlayerId, status = 'ongoing' WHERE id = @BattleId AND player2Id IS NULL";
            using var command = new NpgsqlCommand(updateQuery, connection as NpgsqlConnection);

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

        /// <summary>
        /// Retrieves the battle record with this id
        /// </summary>
        /// <param name="battleId"></param>
        /// <returns></returns>
        public Battle GetBattleById(int? battleId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            string query = $"SELECT * FROM {_battlesTable} WHERE id = @BattleId";

            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
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

        /// <summary>
        /// Updates the battle status
        /// </summary>
        /// <param name="battleId"></param>
        /// <param name="newStatus"></param>
        public void UpdateBattleStatus(int? battleId, string newStatus)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            string updateQuery = $"UPDATE {_battlesTable} SET status = @NewStatus WHERE id = @BattleId";

            using var command = new NpgsqlCommand(updateQuery, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@NewStatus", newStatus);
            command.Parameters.AddWithValue("@BattleId", battleId);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates the battle record with the winner id, sets the status to 'completed' and sets the end time
        /// </summary>
        /// <param name="battleId"></param>
        /// <param name="winnerId"></param>
        /// <exception cref="InvalidOperationException"></exception>

        public void UpdateBattleOutcome(int battleId, int? winnerId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            string updateQuery = $"UPDATE {_battlesTable} SET endTime = @EndTime, winnerId = @WinnerId, status = 'completed' WHERE id = @BattleId";

            using var command = new NpgsqlCommand(updateQuery, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@BattleId", battleId);

            if (winnerId.HasValue)
            {
                command.Parameters.AddWithValue("@WinnerId", winnerId.Value);
            }
            else
            {
                // It was a tie, there's no winner
                command.Parameters.AddWithValue("@WinnerId", DBNull.Value);
            }

            command.Parameters.AddWithValue("@EndTime", DateTime.UtcNow);

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("Failed to update battle outcome, or battle does not exist.");
            }
        }
    }
}