using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

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
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = @"SELECT t.id as Id, t.ownerId as OwnerId, t.cardId as CardId,
                        c.name as CardName, c.damage as Damage, c.elementType::TEXT as ElementType,
                        t.requestedType as RequestedType, t.minDamage as MinDamage, t.status as Status
                        FROM tradings t
                        INNER JOIN cards c ON t.cardId = c.id
                        WHERE t.status = 'open'";

            var offers = new List<ExtendedTradingOffer>();
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var offer = DataMapperService.MapToObject<ExtendedTradingOffer>(reader);
                offers.Add(offer);
            }

            return offers;
        }


        /// <summary>
        /// Returns the offer with this id
        /// </summary>
        /// <param name="offerId"></param>
        /// <returns></returns>
        public TradingOffer? GetOfferById(int offerId)
        {
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"SELECT * FROM {_table} WHERE id = @Id;";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@Id", offerId);

            TradingOffer? offer = null;
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                offer = DataMapperService.MapToObject<TradingOffer>(reader);
            }

            return offer;
        }


        /// <summary>
        /// Checks if the offer meets all requirements and pushes it to the trading store
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool CreateOffer(TradingOffer offer)
        {
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

                using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
                command.Parameters.AddWithValue("@Id", offer.Id!);
                command.Parameters.AddWithValue("@OwnerId", offer.OwnerId!);
                command.Parameters.AddWithValue("@CardId", offer.CardId!);
                command.Parameters.AddWithValue("@RequestedType", offer.RequestedType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MinDamage", offer.MinDamage!);

                command.ExecuteNonQuery();
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
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Check if the card belongs to the user
            var cardBelongsToUserQuery = "SELECT COUNT(*) FROM cards WHERE ownerId = @OwnerId AND id = @CardId";
            using var cardBelongsToUserCommand = new NpgsqlCommand(cardBelongsToUserQuery, connection as NpgsqlConnection);
            cardBelongsToUserCommand.Parameters.AddWithValue("@OwnerId", offer.OwnerId!);
            cardBelongsToUserCommand.Parameters.AddWithValue("@CardId", offer.CardId!);
            var cardBelongsToUserResult = cardBelongsToUserCommand.ExecuteScalar();
            bool cardBelongsToUser = cardBelongsToUserResult != null && Convert.ToInt64(cardBelongsToUserResult) > 0;
            if (!cardBelongsToUser)
            {
                throw new InvalidOperationException("The card does not belong to the user.");
            }

            // Check if the card is in the user's deck
            var cardInDeckQuery = "SELECT COUNT(*) FROM deckCards WHERE ownerId = @OwnerId AND cardId = @CardId";
            using var cardInDeckCommand = new NpgsqlCommand(cardInDeckQuery, connection as NpgsqlConnection);
            cardInDeckCommand.Parameters.AddWithValue("@OwnerId", offer.OwnerId!);
            cardInDeckCommand.Parameters.AddWithValue("@CardId", offer.CardId!);
            var cardInDeckResult = cardInDeckCommand.ExecuteScalar();
            bool cardInDeck = cardInDeckResult != null && Convert.ToInt64(cardInDeckResult) > 0;
            if (cardInDeck)
            {
                throw new InvalidOperationException("The card is in the user's deck.");
            }

            // Check if the card is already in trading
            var cardInTradingQuery = "SELECT COUNT(*) FROM tradings WHERE cardId = @CardId AND status = 'open'";
            using var cardInTradingCommand = new NpgsqlCommand(cardInTradingQuery, connection as NpgsqlConnection);
            cardInTradingCommand.Parameters.AddWithValue("@CardId", offer.CardId!);
            var cardInTradingResult = cardInTradingCommand.ExecuteScalar();
            bool cardInTrading = cardInTradingResult != null && Convert.ToInt64(cardInTradingResult) > 0;
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
                var offerQuery = $"SELECT * FROM {_table} WHERE id = @TradingId AND status = 'open'";
                using var offerCommand = new NpgsqlCommand(offerQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                offerCommand.Parameters.AddWithValue("@TradingId", tradingId!);
                TradingOffer? offer = null;
                using (var offerReader = offerCommand.ExecuteReader())
                {
                    if (offerReader.Read())
                    {
                        offer = DataMapperService.MapToObject<TradingOffer>(offerReader);
                    }
                }
                if (offer == null) throw new InvalidOperationException("Trading offer not found or not open.");

                if (offer.OwnerId == userId)
                {
                    throw new InvalidOperationException("Trading with oneself is not allowed.");
                }

                // Validate that the user's card is eligible for trade
                var userCardQuery = "SELECT * FROM cards WHERE id = @UserCardId AND ownerId = @UserId";
                using var userCardCommand = new NpgsqlCommand(userCardQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                userCardCommand.Parameters.AddWithValue("@UserCardId", userCardId!);
                userCardCommand.Parameters.AddWithValue("@UserId", userId!);
                Card? userCard = null;
                using (var userCardReader = userCardCommand.ExecuteReader())
                {
                    if (userCardReader.Read())
                    {
                        userCard = DataMapperService.MapToObject<Card>(userCardReader);
                    }
                }
                if (userCard == null) throw new InvalidOperationException("User's card not found or does not belong to the user.");

                // Check if the card is in the user's deck
                var isInDeckQuery = "SELECT COUNT(1) FROM deckCards WHERE cardId = @UserCardId AND ownerId = @UserId";
                using var isInDeckCommand = new NpgsqlCommand(isInDeckQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                isInDeckCommand.Parameters.AddWithValue("@UserCardId", userCardId!);
                isInDeckCommand.Parameters.AddWithValue("@UserId", userId!);
                var isInDeckResult = isInDeckCommand.ExecuteScalar();
                bool isInDeck = isInDeckResult != null && (long)isInDeckResult > 0;
                if (isInDeck) throw new InvalidOperationException("User's card is in the deck and cannot be traded.");

                if ((offer.RequestedType != null &&
                    (userCard.CardType == null || !userCard.CardType.Equals(offer.RequestedType, StringComparison.OrdinalIgnoreCase))) ||
                    (userCard.Damage < offer.MinDamage))
                {
                    throw new InvalidOperationException("User's card does not meet the trade requirements.");
                }

                // Perform the trade
                var updateCardOwnerQuery1 = "UPDATE cards SET ownerId = @OwnerId WHERE id = @CardId";
                using var updateCardOwnerCommand1 = new NpgsqlCommand(updateCardOwnerQuery1, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                updateCardOwnerCommand1.Parameters.AddWithValue("@OwnerId", userId!);
                updateCardOwnerCommand1.Parameters.AddWithValue("@CardId", offer.CardId!);
                updateCardOwnerCommand1.ExecuteNonQuery();

                var updateCardOwnerQuery2 = "UPDATE cards SET ownerId = @OwnerId WHERE id = @CardId";
                using var updateCardOwnerCommand2 = new NpgsqlCommand(updateCardOwnerQuery2, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                updateCardOwnerCommand2.Parameters.AddWithValue("@OwnerId", offer.OwnerId!);
                updateCardOwnerCommand2.Parameters.AddWithValue("@CardId", userCardId!);
                updateCardOwnerCommand2.ExecuteNonQuery();

                // Update the trading offer status
                var updateTradingStatusQuery = $"UPDATE {_table} SET status = 'closed' WHERE id = @TradingId";
                using var updateTradingStatusCommand = new NpgsqlCommand(updateTradingStatusQuery, connection as NpgsqlConnection, transaction as NpgsqlTransaction);
                updateTradingStatusCommand.Parameters.AddWithValue("@TradingId", tradingId!);
                updateTradingStatusCommand.ExecuteNonQuery();

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
            var offerQuery = "SELECT * FROM tradings WHERE id = @TradingId AND ownerId = @OwnerId";
            using (var offerCommand = new NpgsqlCommand(offerQuery, connection as NpgsqlConnection))
            {
                offerCommand.Parameters.AddWithValue("@TradingId", tradingId!);
                offerCommand.Parameters.AddWithValue("@OwnerId", userId!);

                using var reader = offerCommand.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Trading offer not found or does not belong to the user.");
                }
            }

            // Delete the offer
            var deleteQuery = "DELETE FROM tradings WHERE id = @TradingId";
            using var deleteCommand = new NpgsqlCommand(deleteQuery, connection as NpgsqlConnection);
            deleteCommand.Parameters.AddWithValue("@TradingId", tradingId!);
            deleteCommand.ExecuteNonQuery();

            return true;
        }


    }
}