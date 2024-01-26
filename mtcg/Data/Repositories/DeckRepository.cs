using MTCG.Data.Models;
using MTCG.Data.Services;
using System.Data;
using System.Diagnostics;
using Npgsql;


namespace MTCG.Data.Repositories
{
    public class DeckRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "deckCards";

        /// <summary>
        /// Fetches all cards in this user's deck
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<Card> GetDeckByUserId(int? userId)
        {
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("User ID cannot be null");
            }

            var cards = new List<Card>();
            try
            {
                using var connection = _dbConnectionManager.GetConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                var query = @"
                    SELECT cards.*
                    FROM deckCards
                    INNER JOIN cards ON deckCards.cardId = cards.id
                    WHERE deckCards.ownerId = @UserId;";

                using (var command = new NpgsqlCommand(query, connection as NpgsqlConnection))
                {
                    command.Parameters.AddWithValue("@UserId", userId.Value);

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var card = DataMapperService.MapToObject<Card>(reader);
                        cards.Add(card);
                    }
                }
                return cards;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public bool ConfigureDeck(int? userId, string[] cardIds)
        {
            if (cardIds.Length != 4)
            {
                throw new ArgumentException("The deck must contain exactly 4 cards.");
            }

            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing deck
                var deleteQuery = $"DELETE FROM {_table} WHERE ownerId = @OwnerId";
                using var deleteCommand = new NpgsqlCommand(deleteQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                deleteCommand.Parameters.AddWithValue("@OwnerId", userId.HasValue ? userId.Value : DBNull.Value);
                deleteCommand.ExecuteNonQuery();
                foreach (var cardId in cardIds)
                {
                    // Check if card is in the store (trading)
                    var isTradingQuery = "SELECT COUNT(1) FROM tradings WHERE cardId = @CardId AND status = 'open'";
                    using var isTradingCommand = new NpgsqlCommand(isTradingQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    isTradingCommand.Parameters.AddWithValue("@CardId", cardId);
                    var isTradingResult = isTradingCommand.ExecuteScalar();
                    bool isTrading = isTradingResult != null && Convert.ToInt64(isTradingResult) > 0;

                    if (isTrading)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Card is open for trading and cannot be added to the deck.");
                    }

                    // Check if card belongs to the user
                    var countQuery = "SELECT COUNT(*) FROM cards WHERE id = @CardId AND ownerId = @OwnerId";
                    using var countCommand = new NpgsqlCommand(countQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    countCommand.Parameters.AddWithValue("@CardId", cardId);
                    countCommand.Parameters.AddWithValue("@OwnerId", userId.HasValue ? userId.Value : DBNull.Value);
                    var countResult = countCommand.ExecuteScalar();
                    int count = countResult != null ? Convert.ToInt32(countResult) : 0;
                    if (count == 0)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Card does not belong to the user.");
                    }

                    // Insert card into the deck
                    var insertQuery = $"INSERT INTO {_table} (cardId, ownerId) VALUES (@CardId, @OwnerId)";
                    using var insertCommand = new NpgsqlCommand(insertQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    insertCommand.Parameters.AddWithValue("@CardId", cardId);
                    insertCommand.Parameters.AddWithValue("@OwnerId", userId.HasValue ? userId.Value : DBNull.Value);
                    insertCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw the original exception
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw new InvalidOperationException("An error occurred while configuring the deck.");
            }
        }

        public void TransferCardToUsersDeck(string cardId, int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            var query = $"UPDATE {_table} SET ownerid = @OwnerId WHERE cardid = @CardId;";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@OwnerId", userId);
            command.Parameters.AddWithValue("@CardId", cardId);
            command.ExecuteNonQuery();
        }

    }
}