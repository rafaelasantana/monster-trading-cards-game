using System;
using Dapper;
using MTCG.Data.Models;
using System.Data;
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
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = @"
                SELECT cards.*
                FROM deckCards
                INNER JOIN cards ON deckCards.cardId = cards.id
                WHERE deckCards.ownerId = @UserId;";

            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);

            if (userId.HasValue)
            {
                command.Parameters.AddWithValue("@UserId", userId.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@UserId", DBNull.Value);
            }

            var cards = new List<Card>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var card = new Card
                {
                    // Map properties from reader to Card
                };
                cards.Add(card);
            }

            if (cards.Count == 0)
            {
                throw new InvalidOperationException("No cards found for the user.");
            }

            return cards;
        }


        public bool ConfigureDeck(int? userId, string[] cardIds)
        {
            // Check if the number of cards is exactly four
            if (cardIds.Length != 4)
            {
                throw new ArgumentException("The deck must contain exactly 4 cards.");
            }

            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Begin transaction
            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing deck
                connection.Execute($"DELETE FROM { _table } WHERE ownerId = @OwnerId", new { OwnerId = userId }, transaction);

                // Add new cards to the deck
                foreach (var cardId in cardIds)
                {
                    // Check if card is in the store (trading)
                    var isTrading = connection.QueryFirstOrDefault<bool>(
                        "SELECT COUNT(1) > 0 FROM tradings WHERE cardId = @CardId AND status = 'open'",
                        new { CardId = cardId }, transaction);
                    if (isTrading)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Card is open for trading and cannot be added to the deck.");
                    }

                    // check if card belongs to the user
                    int count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM cards WHERE id = @CardId AND ownerId = @OwnerId",
                                                    new { CardId = cardId, OwnerId = userId }, transaction);
                    if (count == 0)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Card does not belong to the user.");
                    }
                    // Insert card to the deck
                    connection.Execute($"INSERT INTO { _table } (cardId, ownerId) VALUES (@CardId, @OwnerId)",
                                    new { CardId = cardId, OwnerId = userId }, transaction);
                }

                // Commit transaction
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                transaction.Rollback();
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}