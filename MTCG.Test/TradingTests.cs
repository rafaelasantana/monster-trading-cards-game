using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Repositories;
using Npgsql;

namespace MTCG.Test
{
    public class TradingTests
    {
        private DbConnectionManager _dBConnectionManager;
        private TradingRepository _tradingRepository;

        [SetUp]
        public void Setup()
        {

            _dBConnectionManager = new DbConnectionManager("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");
            _tradingRepository = new TradingRepository(_dBConnectionManager);

            // Set up test data
            SetupTestData();
        }

        /// <summary>
        /// Creates test data
        /// </summary>
        private void SetupTestData()
        {
            // Create test users and cards
            var testUserId1 = CreateTestUser("testUser1", "testPassword1");
            var testUserId2 = CreateTestUser("testUser2", "testPassword2");

            // Create cards for users
            CreateTestCard("testCardInDeck1", 100, testUserId1, true, "Monster"); // In deck
            CreateTestCard("testCardNotInDeck1", 150, testUserId1, false, "Spell"); // Not in deck
            CreateTestCard("testCardNotInDeck2", 200, testUserId2, false, "Monster"); // Not in deck, for other user
        }

        /// <summary>
        /// Helper method to create a card
        /// </summary>
        /// <param name="name"></param>
        /// <param name="damage"></param>
        /// <param name="ownerId"></param>
        /// <param name="inDeck"></param>
        /// <param name="cardType"></param>
        private void CreateTestCard(string name, double damage, int ownerId, bool inDeck, string cardType)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var cardId = Guid.NewGuid().ToString();
            using var command = new NpgsqlCommand(
                "INSERT INTO cards (id, name, damage, ownerId, cardType) VALUES (@Id, @Name, @Damage, @OwnerId, @CardType::CardType)",
                connection);
            command.Parameters.AddWithValue("@Id", cardId);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Damage", damage);
            command.Parameters.AddWithValue("@OwnerId", ownerId);
            command.Parameters.AddWithValue("@CardType", cardType);
            command.ExecuteNonQuery();

            if (inDeck)
            {
                using var deckCommand = new NpgsqlCommand(
                    "INSERT INTO deckCards (cardId, ownerId) VALUES (@CardId, @OwnerId)",
                    connection);
                deckCommand.Parameters.AddWithValue("@CardId", cardId);
                deckCommand.Parameters.AddWithValue("@OwnerId", ownerId);
                deckCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Helper method to create a trading offer
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="cardId"></param>
        /// <param name="requestedType"></param>
        /// <param name="minDamage"></param>
        /// <returns></returns>
        private string CreateTestTradingOffer(int ownerId, string cardId, string requestedType, int minDamage)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var tradingId = Guid.NewGuid().ToString();
            using var command = new NpgsqlCommand(
                "INSERT INTO tradings (id, ownerId, cardId, requestedType, minDamage) VALUES (@Id, @OwnerId, @CardId, @RequestedType, @MinDamage)",
                connection);
            command.Parameters.AddWithValue("@Id", tradingId);
            command.Parameters.AddWithValue("@OwnerId", ownerId);
            command.Parameters.AddWithValue("@CardId", cardId);
            command.Parameters.AddWithValue("@RequestedType", requestedType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MinDamage", minDamage);

            command.ExecuteNonQuery();
            return tradingId;
        }

        /// <summary>
        /// Helper method to create an user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The created user id or 0 (invalid user id)</returns>
        private int CreateTestUser(string username, string password)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = new NpgsqlCommand(
                "INSERT INTO users (username, password) VALUES (@Username, @Password) RETURNING id",
                connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);
            return (int?)command.ExecuteScalar() ?? 0;
        }

        /// <summary>
        /// Retrieves the scalar value from a query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private T? GetScalarValue<T>(string query)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = new NpgsqlCommand(query, connection);
            var result = command.ExecuteScalar();

            // Check if result is DBNull or null
            if (result == DBNull.Value || result == null)
            {
                // Handle the case where T is a value type and cannot be null
                if (Nullable.GetUnderlyingType(typeof(T)) == null && typeof(T).IsValueType)
                {
                    throw new InvalidOperationException("Query returned null, but a non-nullable value type was expected.");
                }

                // Return default value for nullable types or reference types
                return default;
            }

            // Safely cast the result to T
            return (T)result;
        }

        /// <summary>
        /// Checks if an offer exists on the database
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="cardId"></param>
        /// <returns></returns>
        private bool CheckIfOfferExists(int ownerId, string? cardId)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            if (string.IsNullOrEmpty(cardId)) return false;

            using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM tradings WHERE ownerId = @OwnerId AND cardId = @CardId",
                connection);
            command.Parameters.AddWithValue("@OwnerId", ownerId);
            command.Parameters.AddWithValue("@CardId", cardId);
            var result = command.ExecuteScalar();
            return result != null && (long)result > 0;
        }

        /// <summary>
        /// Tries to create a trading with a card that is in the user's deck, should throw exception
        /// </summary>
        [Test]
        public void CreateOffer_CardInUsersDeck_ShouldNotCreateOfferAndThrowException()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            // Arrange
            var testUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardInDeck1'");

            var offer = new TradingOffer
            {
                OwnerId = testUserId,
                CardId = testCardId,
                RequestedType = "spell",
                MinDamage = 50
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.CreateOffer(offer));
            Assert.That(ex.Message, Is.EqualTo("The card is in the user's deck."));

            // Assert that no new offer was created in the database
            var offerExists = CheckIfOfferExists(testUserId, testCardId);
            Assert.That(offerExists, Is.False);
        }

        /// <summary>
        /// Successfully creates an offer
        /// </summary>
        [Test]
        public void CreateOffer_CardNotInUsersDeck_SuccessfullyCreatesOffer()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Arrange
            var testUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardNotInDeckId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");

            var offer = new TradingOffer
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = testUserId,
                CardId = testCardNotInDeckId,
                RequestedType = "monster",
                MinDamage = 50
            };

            // Act
            _tradingRepository.CreateOffer(offer);

            // Assert
            var offerInDb = CheckIfOfferExists(testUserId, testCardNotInDeckId);
            Assert.That(offerInDb, Is.True, "Offer should be created successfully and be in the database");
        }

        /// <summary>
        /// Tries to trade with oneself, should fail
        /// </summary>
        [Test]
        public void ExecuteTrade_TradeWithOneself_ShouldFail()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Arrange
            var testUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");

            var tradingId = CreateTestTradingOffer(testUserId, testCardId!, "spell", 50);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, testUserId, testCardId));
            Assert.That(ex.Message, Is.EqualTo("Trading with oneself is not allowed."));
        }

        /// <summary>
        /// Executes a trade meeting all requirements
        /// </summary>
        [Test]
        public void ExecuteTrade_ValidTradeConditions_ShouldBeSuccessful()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Arrange
            var offerUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 150);

                var tradeUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser2'");
                CreateTestCard("HighDamageCard", 200, tradeUserId, false, "Monster");
                var tradeCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'HighDamageCard'");

                // Act
                bool result = _tradingRepository.ExecuteTrade(tradingId, tradeUserId, tradeCardId);

                // Assert
                Assert.That(result, Is.True);
            }
        }

        /// <summary>
        /// Tries to trade without meeting all requirements, should fail
        /// </summary>
        [Test]
        public void ExecuteTrade_InvalidTradeConditions_ShouldFail()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Arrange
            var offerUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 100); // example: monster, 100 damage

                // Create a card with insufficient damage
                var tradeUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser2'");
                var lowDamageCardName = "LowDamageCard";
                CreateTestCard(lowDamageCardName, 30, tradeUserId, false, "Monster"); // example: Monster type, 30 damage
                var lowDamageCardId = GetScalarValue<string>($"SELECT id FROM cards WHERE name = '{lowDamageCardName}'");

                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, tradeUserId, lowDamageCardId));
                Assert.That(ex.Message, Is.EqualTo("User's card does not meet the trade requirements."));
            }
        }

        /// <summary>
        /// Tries to trade a card that is in the user's deck, should fail
        /// </summary>
        [Test]
        public void ExecuteTrade_CardInUsersDeck_ShouldFail()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Arrange
            var offerUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = GetScalarValue<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 100);

                var tradeUserId = GetScalarValue<int>("SELECT id FROM users WHERE username = 'testUser2'");
                var inDeckCardName = "InDeckCard";
                CreateTestCard(inDeckCardName, 200, tradeUserId, true, "Monster");
                var inDeckCardId = GetScalarValue<string>($"SELECT id FROM cards WHERE name = '{inDeckCardName}'");

                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, tradeUserId, inDeckCardId));
                Assert.That(ex.Message, Is.EqualTo("User's card is in the deck and cannot be traded."));
            }
        }

        /// <summary>
        /// Deletes test data
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            // Call your clear_all_tables function here
            ClearAllTables();
        }

        private void ClearAllTables()
        {
            using var connection = _dBConnectionManager.GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand("SELECT clear_all_tables()", connection);
            command.ExecuteNonQuery();
        }

    }
}
