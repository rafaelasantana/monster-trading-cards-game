using System;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class DeckRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _Table = "deckCards";
        private readonly string _Fields = "id, cardId, ownerId";

        public DeckRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Fetches all cards in this user's deck
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<Card> GetDeckByUserId(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = @"
                SELECT cards.*
                FROM deckCards
                INNER JOIN cards ON deckCards.cardId = cards.id
                WHERE deckCards.ownerId = @UserId;";
            var cards = connection.Query<Card>(query, new { UserId = userId }).ToList();

            Console.WriteLine($"Executed query, number of items: {cards.Count }");
            if (cards == null)
            {
                throw new InvalidOperationException("No cards found for the user.");
            }

            return cards;
        }

        /// <summary>
        /// Configures the deck for the user according to the requested card ids
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cardIds"></param>
        /// <returns></returns>
        public bool ConfigureDeck(int userId, string[] cardIds)
        {
            Console.WriteLine("In ConfigureDeck:");
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Begin transaction
            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear existing deck
                connection.Execute("DELETE FROM deckCards WHERE ownerId = @OwnerId", new { OwnerId = userId }, transaction);

                Console.WriteLine("Deleted current deck");
                // Add new cards to the deck
                foreach (var cardId in cardIds)
                {
                    // check if card belongs to the user
                    int count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM cards WHERE id = @CardId AND ownerId = @OwnerId",
                                                    new { CardId = cardId, OwnerId = userId }, transaction);
                    if (count == 0)
                    {
                        Console.WriteLine("Card did not belong to the user!");
                        transaction.Rollback();
                        return false; // Card does not belong to the user
                    }
                    connection.Execute("INSERT INTO deckCards (cardId, ownerId) VALUES (@CardId, @OwnerId)",
                                    new { CardId = cardId, OwnerId = userId }, transaction);
                    Console.WriteLine($"Inserted a card with id={ cardId } to the deck");
                }

                // Commit transaction
                transaction.Commit();
                Console.WriteLine("New deck is ready");
                return true;
            }
            catch
            {
                // Rollback transaction on error
                transaction.Rollback();
                return false;
            }
        }
    }
}