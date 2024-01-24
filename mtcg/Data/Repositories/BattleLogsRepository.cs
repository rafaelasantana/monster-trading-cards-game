using Npgsql;
using System.Data;

namespace MTCG.Data.Repositories
{
    public class BattleLogsRepository
    {
        private readonly IDbConnectionManager _dbConnectionManager;
        private readonly string _battleLogsTable = "battleLogs";

        public BattleLogsRepository(IDbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        public void LogBattleRound(int battleId, int roundNumber, string player1CardId, string player2CardId, string roundResult)
        {
            using var connection = _dbConnectionManager.GetConnection() as NpgsqlConnection;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            string insertQuery = $"INSERT INTO {_battleLogsTable} (battleId, roundNumber, player1CardId, player2CardId, roundResult) VALUES (@BattleId, @RoundNumber, @Player1CardId, @Player2CardId, @RoundResult)";

            using var command = new NpgsqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@BattleId", battleId);
            command.Parameters.AddWithValue("@RoundNumber", roundNumber);
            command.Parameters.AddWithValue("@Player1CardId", player1CardId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Player2CardId", player2CardId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RoundResult", roundResult ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }
    }
}