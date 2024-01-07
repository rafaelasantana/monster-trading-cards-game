using Dapper;
using MTCG.Data.Models;
using MTCG.Data.Repositories;
using Npgsql;
using System.Data;

namespace MTCG.Test
{
    public class TradingRepositoryTest
    {
        private IDbConnection _dbConnection;
        private TradingRepository _tradingRepository;

        [SetUp]
        public void Setup()
        {
            _dbConnection = new NpgsqlConnection("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");
            _tradingRepository = new TradingRepository(new DbConnectionManager(_dbConnection));

            // Set up test data
            SetupTestData();
        }

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


        private void CreateTestCard(string name, double damage, int ownerId, bool inDeck, string cardType)
        {
            var cardId = Guid.NewGuid().ToString();
            _dbConnection.Execute(
                "INSERT INTO cards (id, name, damage, ownerId, cardType) VALUES (@Id, @Name, @Damage, @OwnerId, @CardType::CardType)",
                new { Id = cardId, Name = name, Damage = damage, OwnerId = ownerId, CardType = cardType });

            if (inDeck)
            {
                _dbConnection.Execute(
                    "INSERT INTO deckCards (cardId, ownerId) VALUES (@CardId, @OwnerId)",
                    new { CardId = cardId, OwnerId = ownerId });
            }
        }


        private string CreateTestTradingOffer(int ownerId, string cardId, string requestedType, int minDamage)
        {
            var tradingId = Guid.NewGuid().ToString();
            _dbConnection.Execute(
                "INSERT INTO tradings (id, ownerId, cardId, requestedType, minDamage) VALUES (@Id, @OwnerId, @CardId, @RequestedType, @MinDamage)",
                new { Id = tradingId, OwnerId = ownerId, CardId = cardId, RequestedType = requestedType, MinDamage = minDamage });
            return tradingId;
        }


        private int CreateTestUser(string username, string password)
        {
            return _dbConnection.ExecuteScalar<int>(
                "INSERT INTO users (username, password) VALUES (@Username, @Password) RETURNING id",
                new { Username = username, Password = password });
        }

        [Test]
        public void CreateOffer_CardInUsersDeck_ShouldNotCreateOfferAndThrowException()
        {
            // Arrange
            var testUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardInDeck1'");

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
            var offerInDb = _dbConnection.Query<TradingOffer>("SELECT * FROM tradings WHERE ownerId = @OwnerId AND cardId = @CardId", new { OwnerId = testUserId, CardId = testCardId }).FirstOrDefault();
            Assert.That(offerInDb, Is.Null);
        }



        [Test]
        public void CreateOffer_CardNotInUsersDeck_SuccessfullyCreatesOffer()
        {
            // Arrange
            var testUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardNotInDeckId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");

            var offer = new TradingOffer
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = testUserId,
                CardId = testCardNotInDeckId,
                RequestedType = "monster",
                MinDamage = 50
            };

            // Act
            bool result = _tradingRepository.CreateOffer(offer);

            // Assert
            Assert.That(result, Is.True, "Offer should be created successfully");

            var offerInDb = _dbConnection.Query<TradingOffer>("SELECT * FROM tradings WHERE cardId = @CardId", new { CardId = testCardNotInDeckId }).FirstOrDefault();
            Assert.That(offerInDb, Is.Not.Null, "Offer should be in the database");
            Assert.That(offerInDb.OwnerId, Is.EqualTo(testUserId), "Offer should belong to the correct user");
        }

        [Test]
        public void ExecuteTrade_TradeWithOneself_ShouldFail()
        {
            // Arrange
            var testUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var testCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");

            // Check if testCardId is null
            if (testCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(testUserId, testCardId, "spell", 50); // Example: assuming these are the offer's conditions

                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, testUserId, testCardId));
                Assert.That(ex.Message, Is.EqualTo("Trading with oneself is not allowed."));
            }
        }


        [Test]
        public void ExecuteTrade_ValidTradeConditions_ShouldBeSuccessful()
        {
            // Arrange
            var offerUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 150);

                var tradeUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser2'");
                CreateTestCard("HighDamageCard", 200, tradeUserId, false, "Monster");
                var tradeCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'HighDamageCard'");

                // Act
                bool result = _tradingRepository.ExecuteTrade(tradingId, tradeUserId, tradeCardId);

                // Assert
                Assert.That(result, Is.True);
            }
        }

        [Test]
        public void ExecuteTrade_InvalidTradeConditions_ShouldFail()
        {
            // Arrange
            var offerUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 100); // example: monster, 100 damage

                // Create a card with insufficient damage
                var tradeUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser2'");
                var lowDamageCardName = "LowDamageCard";
                CreateTestCard(lowDamageCardName, 30, tradeUserId, false, "Monster"); // example: Monster type, 30 damage
                var lowDamageCardId = _dbConnection.ExecuteScalar<string>($"SELECT id FROM cards WHERE name = '{lowDamageCardName}'");

                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, tradeUserId, lowDamageCardId));
                Assert.That(ex.Message, Is.EqualTo("User's card does not meet the trade requirements."));

            }
        }

        [Test]
        public void ExecuteTrade_CardInUsersDeck_ShouldFail()
        {
            // Arrange
            var offerUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser1'");
            var offerCardId = _dbConnection.ExecuteScalar<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck1'");
            if (offerCardId == null)
            {
                Assert.Fail("Test card 'testCardNotInDeck1' not found in the database.");
            }
            else
            {
                var tradingId = CreateTestTradingOffer(offerUserId, offerCardId, "monster", 100); // example: monster, 100 damage

                var tradeUserId = _dbConnection.ExecuteScalar<int>("SELECT id FROM users WHERE username = 'testUser2'");
                var inDeckCardName = "InDeckCard";
                CreateTestCard(inDeckCardName, 200, tradeUserId, true, "Monster"); // example: Monster type, 200 damage
                var inDeckCardId = _dbConnection.ExecuteScalar<string>($"SELECT id FROM cards WHERE name = '{inDeckCardName}'");

                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.ExecuteTrade(tradingId, tradeUserId, inDeckCardId));
                Assert.That(ex.Message, Is.EqualTo("User's card is in the deck and cannot be traded."));

            }
        }

        [TearDown]
        public void Cleanup()
        {
            _dbConnection.Execute("DELETE FROM tradings");
            _dbConnection.Execute("DELETE FROM deckCards");
            _dbConnection.Execute("DELETE FROM cards");
            _dbConnection.Execute("DELETE FROM users");
        }

    }
}
