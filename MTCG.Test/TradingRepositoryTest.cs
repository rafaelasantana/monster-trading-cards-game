using Dapper;
using mtcg.Data.Models;
using mtcg.Data.Repositories;
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
            // Insert test user
            var testUserId = _dbConnection.ExecuteScalar<int>("INSERT INTO users (username, password) VALUES ('testUser', 'testPassword') RETURNING id");

            // Insert test card (in deck) and assign it to the test user
            var testCardInDeckId = Guid.NewGuid().ToString();
            _dbConnection.Execute("INSERT INTO cards (id, name, damage, ownerId) VALUES (@Id, 'testCardInDeck', 100, @OwnerId)", new { Id = testCardInDeckId, OwnerId = testUserId });
            _dbConnection.Execute("INSERT INTO deckCards (cardId, ownerId) VALUES (@CardId, @OwnerId)", new { CardId = testCardInDeckId, OwnerId = testUserId });

            // Insert another test card (not in deck)
            var testCardNotInDeckId = Guid.NewGuid().ToString();
            _dbConnection.Execute("INSERT INTO cards (id, name, damage, ownerId) VALUES (@Id, 'testCardNotInDeck', 150, @OwnerId)", new { Id = testCardNotInDeckId, OwnerId = testUserId });
        }

        [Test]
        public void CreateOffer_CardInUsersDeck_ShouldNotCreateOfferAndThrowException()
        {
            // Arrange
            var testUserId = _dbConnection.QueryFirstOrDefault<int>("SELECT id FROM users WHERE username = 'testUser'");
            var testCardId = _dbConnection.QueryFirstOrDefault<string>("SELECT id FROM cards WHERE name = 'testCardInDeck'");

            Assert.That(testUserId, Is.Not.EqualTo(0), "Test user should exist");
            Assert.That(testCardId, Is.Not.Null.Or.Empty, "Test card should exist");

            var offer = new TradingOffer
            {
                OwnerId = testUserId,
                CardId = testCardId,
                RequestedType = "spell",
                MinDamage = 50
            };

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => _tradingRepository.CreateOffer(offer));

            // Assert
            Assert.That(ex.Message, Is.EqualTo("The card is in the user's deck."));

            // Assert that no new offer was created in the database
            var offerInDb = _dbConnection.Query<TradingOffer>("SELECT * FROM tradings WHERE ownerId = @OwnerId AND cardId = @CardId", new { OwnerId = testUserId, CardId = testCardId }).FirstOrDefault();
            Assert.That(offerInDb, Is.Null);
        }


        [Test]
        public void CreateOffer_CardNotInUsersDeck_SuccessfullyCreatesOffer()
        {
            // Arrange
            var testUserId = _dbConnection.QueryFirstOrDefault<int>("SELECT id FROM users WHERE username = 'testUser'");
            var testCardNotInDeckId = _dbConnection.QueryFirstOrDefault<string>("SELECT id FROM cards WHERE name = 'testCardNotInDeck'");

            Assert.That(testUserId, Is.Not.EqualTo(0), "Test user should exist");
            Assert.That(testCardNotInDeckId, Is.Not.Null.Or.Empty, "Test card not in deck should exist");

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

        [TearDown]
        public void Cleanup()
        {
            _dbConnection.Execute("DELETE FROM tradings WHERE ownerId IN (SELECT id FROM users WHERE username = 'testUser')");
            _dbConnection.Execute("DELETE FROM deckCards WHERE ownerId IN (SELECT id FROM users WHERE username = 'testUser')");
            _dbConnection.Execute("DELETE FROM cards WHERE ownerId IN (SELECT id FROM users WHERE username = 'testUser')");
            _dbConnection.Execute("DELETE FROM users WHERE username = 'testUser'");
        }

    }
}
