using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class TradingRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _Table = "tradings";

        public TradingRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Returns all open offers for trading
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExtendedTradingOffer> GetAllOffers()
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = @"
                SELECT t.id as Id, t.ownerId as OwnerId, t.cardId as CardId,
                    c.name as CardName, c.damage as Damage, c.elementType as ElementType,
                    t.requestedType as RequestedType, t.minDamage as MinDamage, t.status as Status
                FROM tradings t
                INNER JOIN cards c ON t.cardId = c.id
                WHERE t.status = 'open'";

            return connection.Query<ExtendedTradingOffer>(query);
        }


        public TradingOffer GetOfferById(int offerId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            return connection.QueryFirstOrDefault<TradingOffer>(
                $"SELECT * FROM {_Table} WHERE id = @Id;",
                new { OfferId = offerId });
        }

        /// <summary>
        /// Checks if the offer meets all requirements and pushes it to the trading store
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool CreateOffer(TradingOffer offer)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            try
            {
                // check if offer meets all requirements
                CheckOffer(offer);

                // If all checks pass, proceed with creating the offer
                var query = $"INSERT INTO {_Table} (id, ownerId, cardId, requestedType, minDamage) VALUES (@Id, @OwnerId, @CardId, @RequestedType, @MinDamage);";
                connection.Execute(query, offer);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// Checks if the card has all requirements to be pushed to the trading store
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public void CheckOffer(TradingOffer offer)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Check if the card belongs to the user
            bool cardBelongsToUser = connection.QueryFirstOrDefault<bool>(
                "SELECT COUNT(1) > 0 FROM cards WHERE ownerId = @OwnerId AND id = @CardId",
                new { OwnerId = offer.OwnerId, CardId = offer.CardId });

            if (!cardBelongsToUser)
            {
                throw new InvalidOperationException("The card does not belong to the user.");
            }

            // Check if the card is in the user's deckopen
            bool cardInDeck = connection.QueryFirstOrDefault<bool>(
                "SELECT COUNT(1) > 0 FROM deckCards WHERE ownerId = @OwnerId AND cardId = @CardId",
                new { OwnerId = offer.OwnerId, CardId = offer.CardId });

            if (cardInDeck)
            {
                throw new InvalidOperationException("The card is in the user's deck.");
            }

            // Check if the card is already in trading
            bool cardInTrading = connection.QueryFirstOrDefault<bool>(
                "SELECT COUNT(1) > 0 FROM tradings WHERE cardId = @CardId AND status = 'open'",
                new { CardId = offer.CardId });

            if (cardInTrading)
            {
                throw new InvalidOperationException("The card is already in trading.");
            }
        }


        public void UpdateOfferStatus(int offerId, string status)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = $"UPDATE {_Table} SET status = @Status, updatedAt = CURRENT_TIMESTAMP WHERE id = @Id;";
            connection.Execute(query, new { OfferId = offerId, Status = status });
        }
    }
}