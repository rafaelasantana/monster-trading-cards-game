using MTCG.Data.Repositories;
using MTCG.Data.Models;
using System.Diagnostics;
using System.Data;
using NUnit.Framework;
using MTCG.Data.Services;
using Npgsql;

namespace MTCG.Test
{
    public class BattleServiceTests
    {
        private DbConnectionManager _dBConnectionManager;
        private DeckRepository _deckRepository;
        private BattleRepository _battleRepository;
        private UserStatsRepository _userStatsRepository;
        private BattleLogsRepository _battleLogsRepository;
        private BattleService _battleService;
        private UserRepository _userRepository;
        private UserProfileRepository _userProfileRepository;
        private UserService _userService;


        [SetUp]
        public void Setup()
        {
            _dBConnectionManager = new DbConnectionManager("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");
            _battleRepository = new BattleRepository(_dBConnectionManager);
            _deckRepository = new DeckRepository(_dBConnectionManager);
            _battleLogsRepository = new BattleLogsRepository(_dBConnectionManager);
            _userStatsRepository = new UserStatsRepository(_dBConnectionManager);
            _battleService = new BattleService(_deckRepository, _battleRepository, _userStatsRepository, _battleLogsRepository);
            _userRepository = new UserRepository(_dBConnectionManager);
            _userProfileRepository = new UserProfileRepository(_dBConnectionManager);
            _userService = new UserService(_userRepository, _userStatsRepository, _userProfileRepository);
        }

        /// <summary>
        /// Helper method to create a random test deck associated with this user on the database
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userId"></param>
        private void InsertTestDeck(int userId)
        {
            using var connection = _dBConnectionManager.GetConnection();
            connection.Open();

            Random rnd = new();
            string[] elementTypes = ["Normal", "Water", "Fire"];
            string[] cardTypes = ["Monster", "Spell"];

            // Insert 4 cards into the 'cards' table
            for (int i = 0; i < 4; i++) //
            {
                string cardId = Guid.NewGuid().ToString();
                string cardName = $"TestCard{i}";
                double damage = 10.0 * (i + 1);
                // Randomly select elementType and cardType
                string elementType = elementTypes[rnd.Next(elementTypes.Length)];
                string cardType = cardTypes[rnd.Next(cardTypes.Length)];

                using var cardCommand = new NpgsqlCommand(
                    "INSERT INTO cards (id, name, damage, elementType, cardType, ownerId) VALUES (@Id, @Name, @Damage, @ElementType, @CardType, @OwnerId)",
                    connection);
                cardCommand.Parameters.AddWithValue("@Id", cardId);
                cardCommand.Parameters.AddWithValue("@Name", cardName);
                cardCommand.Parameters.AddWithValue("@Damage", damage);
                cardCommand.Parameters.AddWithValue("@ElementType", elementType);
                cardCommand.Parameters.AddWithValue("@CardType", cardType);
                cardCommand.Parameters.AddWithValue("@OwnerId", userId);
                cardCommand.ExecuteNonQuery();

                // Associate the card with the user's deck in 'deckCards' table
                using var deckCommand = new NpgsqlCommand(
                    "INSERT INTO deckCards (cardId, ownerId) VALUES (@CardId, @OwnerId)",
                    connection);
                deckCommand.Parameters.AddWithValue("@CardId", cardId);
                deckCommand.Parameters.AddWithValue("@OwnerId", userId);
                deckCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Helper to register and login an user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public User RegisterAndLoginUser(string username, string password)
        {
            User? player = new() { Username = username, Password = password };
            _userService.RegisterUser(player);
            _userService.LoginUser(username, password);
            if (player == null) throw new InvalidOperationException("Could not register and login test user.");
            return player;
        }

        /// <summary>
        /// Helper to print scoreboard
        /// </summary>
        /// <param name="scoreboardData"></param>
        public void PrintScoreboardData(IEnumerable<Scoreboard>? scoreboardData)
        {
            if (scoreboardData != null && scoreboardData.Any())
            {
                Console.WriteLine("\n *** SCOREBOARD ***\n");
                foreach (var scoreboard in scoreboardData)
                {
                    Console.WriteLine($"Username: {scoreboard.Username}, Elo Rating: {scoreboard.EloRating}, Wins: {scoreboard.Wins}, Losses: {scoreboard.Losses}\n");
                }
            }
            else
            {
                Console.WriteLine("No scoreboard data available.");
            }
        }

        /// <summary>
        /// Tries to request a battle without an user Id
        /// </summary>
        [Test]
        public void TestRequestBattle_NullPlayerId_ShouldFailAndThrowException()
        {
            // Arrange
            int? playerId = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _battleService.RequestBattle(playerId));
            Assert.That(ex.ParamName, Is.EqualTo("playerId"), "The ArgumentNullException should have the correct parameter name.");
        }

        /// <summary>
        /// Try to request a battle with playerId = null, should throw exception
        /// </summary>
        [Test]
        public void TestRequestBattle_InvalidDeck_ShouldFailAndThrowException()
        {
            // Arrange
                // Register and login the player
            User testPlayer = RegisterAndLoginUser("testPlayer", "testPlayerPassword");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _battleService.RequestBattle(testPlayer.Id.GetValueOrDefault()));
            Assert.That(ex.Message, Is.EqualTo("Player does not have a valid deck."), "An InvalidOperationException with the correct message should be thrown when a player with an invalid deck requests a battle.");
        }

        /// <summary>
        /// User with a valid deck requests battle, a new pending battle should be created
        /// </summary>
        [Test]
        public void TestRequestBattle_SuccessfulPendingBattleCreation()
        {
            // Arrange
                // Register ad login the player
            User testPlayer = RegisterAndLoginUser("testPlayer", "testPlayerPassword");
                // Set valid deck for the user
            InsertTestDeck(testPlayer.Id.GetValueOrDefault());

            // Act
            var result = _battleService.RequestBattle(testPlayer.Id.GetValueOrDefault());

            // Assert
            Assert.That(result, Is.Not.Null, "Battle result should not be null.");
            Assert.That(result.Status, Is.EqualTo(BattleStatus.Pending), "Newly created battle should have 'Pending' status.");
        }

        /// <summary>
        /// Second player requests a battle, should enter a pending battle, battle should be conducted and results printed
        /// </summary>
        [Test]
        public void TestRequestBattle_SuccessfulJoinExistingBattleAndConductBattle()
        {
            // Arrange
                // Register and login the first player
            User firstPlayer = RegisterAndLoginUser("firstPlayer", "firstPlayerPassword");
                // First player requests battle with a valid deck
            InsertTestDeck(firstPlayer.Id.GetValueOrDefault());
            _battleService.RequestBattle(firstPlayer.Id.GetValueOrDefault());

                // Register the second player with a valid deck
            User secondPlayer = RegisterAndLoginUser("secondPlayer", "secondPlayerPassword");
                // Set a valid deck for the second player
            InsertTestDeck(secondPlayer.Id.GetValueOrDefault());


            // Act
            var resultSecondPlayer = _battleService.RequestBattle(secondPlayer.Id.GetValueOrDefault());

            // Assert
            Assert.That(resultSecondPlayer, Is.Not.Null, "Battle result should not be null.");
            Assert.That(resultSecondPlayer.Status, Is.EqualTo(BattleStatus.Ongoing), "Player should join a pending battle, battle should have 'Ongoing' status.");
        }

        /// <summary>
        /// Tries to conduct a battle with null id, should fail
        /// </summary>
        [Test]
        public void TestConductBattle_BattleDoesNotExist()
        {
            // Arrange
            int? battleId = null; // Simulate a null battle ID

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _battleService.ConductBattle(battleId));
            Assert.That(ex.Message, Is.EqualTo("Battle must have two players, or battle does not exist."));
        }

        /// <summary>
        /// Tries to conduct a battle with only one player, should fail
        /// </summary>
        [Test]
        public void TestConductBattle_OneUserInBattle_ShouldThrowException()
        {
            // Arrange
                // Register and login player
            User testPlayer = RegisterAndLoginUser("testPlayer", "testPassword");
                // Set valid deck for the user
            InsertTestDeck(testPlayer.Id.GetValueOrDefault());
                // User requests battle
            BattleResult result = _battleService.RequestBattle(testPlayer.Id.GetValueOrDefault());


            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _battleService.ConductBattle(result.BattleId));
            Assert.That(ex.Message, Is.EqualTo("Battle must have two players, or battle does not exist."));
        }

        /// <summary>
        /// Ensures player stats are updated after the battle
        /// </summary>
        [Test]
        public void TestUpdatePlayerStats_ShouldUpdateStatsAfterBatle()
        {
            // Arrange
                // Register ad login the first player
            User firstPlayer = RegisterAndLoginUser("firstPlayer", "firstPlayerPassword");
                // Set a valid deck for the first player
            InsertTestDeck(firstPlayer.Id.GetValueOrDefault());
                // Get player stats before the battle
            UserStats? firstPlayerStatsBeforeBattle = _userStatsRepository.GetStatsByUserId(firstPlayer.Id.GetValueOrDefault());
                // First player requests battle
            _battleService.RequestBattle(firstPlayer.Id.GetValueOrDefault());

                // Register and login the second player with a valid deck
            User secondPlayer = RegisterAndLoginUser("secondPlayer", "secondPlayerPassword");
                // Set a valid deck for the second player
            InsertTestDeck(secondPlayer.Id.GetValueOrDefault());
                // Get player stats before battle
            UserStats? secondPlayerStatsBeforeBattle = _userStatsRepository.GetStatsByUserId(secondPlayer.Id.GetValueOrDefault());

            // Act
                // Conduct battle
            BattleResult battleResult = _battleService.RequestBattle(secondPlayer.Id.GetValueOrDefault());
                // Get both player's stats after the battle
            UserStats? firstPlayerStatsAfterBattle = _userStatsRepository.GetStatsByUserId(firstPlayer.Id.GetValueOrDefault());
            UserStats? secondPlayerStatsAfterBattle = _userStatsRepository.GetStatsByUserId(secondPlayer.Id.GetValueOrDefault());

            // Print scoreboard after battle
            PrintScoreboardData(_userStatsRepository.GetScoreboardData());

            // Assert
            Assert.Multiple(() =>
            {
                // In case of a draw, ELO does not change
                if (battleResult.WinnerId == null)
                {
                    // Assert Elo rating has not changed
                    Assert.That(firstPlayerStatsAfterBattle?.EloRating, Is.EqualTo(firstPlayerStatsBeforeBattle?.EloRating), "First player's Elo rating should not have changed.");
                    Assert.That(secondPlayerStatsAfterBattle?.EloRating, Is.EqualTo(secondPlayerStatsBeforeBattle?.EloRating), "Second player's Elo rating should not have changed.");
                }
                else
                {
                    // Assert Elo rating has changed
                    Assert.That(firstPlayerStatsAfterBattle?.EloRating, Is.Not.EqualTo(firstPlayerStatsBeforeBattle?.EloRating), "First player's Elo rating should have changed.");
                    Assert.That(secondPlayerStatsAfterBattle?.EloRating, Is.Not.EqualTo(secondPlayerStatsBeforeBattle?.EloRating), "Second player's Elo rating should have changed.");
                }
                // Assert total games played increased by 1
                Assert.That(firstPlayerStatsAfterBattle?.TotalGamesPlayed, Is.EqualTo(firstPlayerStatsBeforeBattle?.TotalGamesPlayed + 1), "First player's total games played should have increased by 1.");
                Assert.That(secondPlayerStatsAfterBattle?.TotalGamesPlayed, Is.EqualTo(secondPlayerStatsBeforeBattle?.TotalGamesPlayed + 1), "Second player's total games played should have increased by 1.");
            });
        }

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