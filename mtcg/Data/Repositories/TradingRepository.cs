using System.Data;
using Dapper;
using MTCG.Data.Models;

namespace MTCG.Data.Repositories
{
    public class TradingRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "tradings";

        /// <summary>
        /// Returns all open offers for trading
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExtendedTradingOffer> GetAllOffers()
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = @"
                SELECT t.id as Id, t.ownerId as OwnerId, t.cardId as CardId,
                    c.name as CardName, c.damage as Damage, c.elementType::TEXT as ElementType,
                    t.requestedType as RequestedType, t.minDamage as MinDamage, t.status as Status
                FROM tradings t
                INNER JOIN cards c ON t.cardId = c.id
                WHERE t.status = 'open'";

            return connection.Query<ExtendedTradingOffer>(query);
        }

        /// <summary>
        /// Returns the offer with this id
        /// </summary>
        /// <param name="offerId"></param>
        /// <returns></returns>
        public TradingOffer? GetOfferById(int offerId)
        {
            Console.WriteLine("In GetOfferById");
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection.QueryFirstOrDefault<TradingOffer>(
                $"SELECT * FROM {_table} WHERE id = @Id;",
                new { id = offerId });
        }

        /// <summary>
        /// Checks if the offer meets all requirements and pushes it to the trading store
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool CreateOffer(TradingOffer offer)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            try
            {
                // check if offer meets all requirements
                CheckOffer(offer);

                // If all checks pass, proceed with creating the offer
                var query = $"INSERT INTO {_table} (id, ownerId, cardId, requestedType, minDamage) VALUES (@Id, @OwnerId, @CardId, @RequestedType, @MinDamage);";
                connection.Execute(query, offer);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

        }

        /// <summary>
        /// Checks if the card has all requirements to be pushed to the trading store
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public void CheckOffer(TradingOffer offer)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

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

        /// <summary>
        /// Executes trade if all conditions are met
        /// </summary>
        /// <param name="tradingId"></param>
        /// <param name="userId"></param>
        /// <param name="userCardId"></param>
        /// <returns></returns>
        public bool ExecuteTrade(string? tradingId, int? userId, string? userCardId)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                // Retrieve the trading offer
                var offer = connection.QueryFirstOrDefault<TradingOffer>(
                    "SELECT * FROM tradings WHERE id = @TradingId AND status = 'open'",
                    new { TradingId = tradingId }, transaction) ?? throw new InvalidOperationException("Trading offer not found or not open.");

                // Check if the user is trying to trade with oneself
                if (offer.OwnerId == userId)
                {
                    throw new InvalidOperationException("Trading with oneself is not allowed.");
                }

                // Validate that the user's card is eligible for trade
                var userCard = connection.QueryFirstOrDefault<Card>(
                    "SELECT * FROM cards WHERE id = @UserCardId AND ownerId = @UserId",
                    new { UserCardId = userCardId, UserId = userId }, transaction) ?? throw new InvalidOperationException("User's card not found or does not belong to the user.");

                // Check if the card is in the user's deck
                var isInDeck = connection.QueryFirstOrDefault<bool>(
                    "SELECT COUNT(1) FROM deckCards WHERE cardId = @UserCardId AND ownerId = @UserId",
                    new { UserCardId = userCardId, UserId = userId }, transaction);

                if (isInDeck) throw new InvalidOperationException("User's card is in the deck and cannot be traded.");

                // Validate trade conditions (requested card type, minimum damage)
                if ((offer.RequestedType != null &&
                    (userCard.CardType == null || !userCard.CardType.Equals(offer.RequestedType, StringComparison.OrdinalIgnoreCase))) ||
                    (userCard.Damage < offer.MinDamage))
                {
                    throw new InvalidOperationException("User's card does not meet the trade requirements.");
                }

                // Perform the trade
                connection.Execute(
                    "UPDATE cards SET ownerId = @OwnerId WHERE id = @CardId",
                    new { OwnerId = userId, CardId = offer.CardId }, transaction);
                connection.Execute(
                    "UPDATE cards SET ownerId = @OwnerId WHERE id = @CardId",
                    new { OwnerId = offer.OwnerId, CardId = userCardId }, transaction);

                // Update the trading offer status
                connection.Execute(
                    "UPDATE tradings SET status = 'closed' WHERE id = @TradingId",
                    new { TradingId = tradingId }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Deletes an offer belonging to the user or throws an exception
        /// </summary>
        /// <param name="tradingId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool DeleteOffer(string? tradingId, int? userId)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Check if the offer exists and belongs to the user
            var offer = connection.QueryFirstOrDefault<TradingOffer>(
                "SELECT * FROM tradings WHERE id = @TradingId AND ownerId = @OwnerId",
                new { TradingId = tradingId, OwnerId = userId }) ?? throw new InvalidOperationException("Trading offer not found or does not belong to the user.");

            // Delete the offer
            connection.Execute("DELETE FROM tradings WHERE id = @TradingId", new { TradingId = tradingId });
            return true;
        }
    }
}