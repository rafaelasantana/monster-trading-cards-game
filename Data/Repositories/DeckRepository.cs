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
            Console.WriteLine("In GetDeckByUserId");
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"SELECT * FROM { _Table } WHERE ownerId = @UserId";
            var cards = connection.Query<Card>(query, new { UserId = userId }).ToList();

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