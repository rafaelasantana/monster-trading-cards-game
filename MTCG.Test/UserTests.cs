using Dapper;
using Microsoft.EntityFrameworkCore;
using MTCG.Data.Models;
using MTCG.Data.Repositories;
using MTCG.Data.Services;
using Npgsql;
using System.Data;

namespace MTCG.Test
{
    public class UserTests
    {
        private IDbConnection _dbConnection;
        private UserRepository _userRepository;
        private UserStatsRepository _userStatsRepository;
        private UserProfileRepository _userProfileRepository;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _dbConnection = new NpgsqlConnection("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");
            _userStatsRepository = new UserStatsRepository(new DbConnectionManager(_dbConnection));
            _userProfileRepository = new UserProfileRepository(new DbConnectionManager(_dbConnection));
            _userRepository = new UserRepository(new DbConnectionManager(_dbConnection), _userStatsRepository, _userProfileRepository);
            _userService = new UserService(_userRepository, _userStatsRepository, _userProfileRepository);
        }

        /// <summary>
        /// Saves a new user to the database
        /// </summary>
        [Test]
        public void Save_NewUser_ShouldAddUserToDatabase()
        {
            // Arrange
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "testPassword" };

            // Act
            _userRepository.Save(newUser);

            // Assert
            var userInDb = _dbConnection.Query<User>($"SELECT * FROM users WHERE username = @Username", new { newUser.Username }).FirstOrDefault();
            Assert.That(userInDb, Is.Not.Null, "User should be added to the database");
            Assert.That(userInDb.Username, Is.EqualTo(newUser.Username), "Username should match");
        }

        /// <summary>
        /// Updates an existing user on the database
        /// </summary>
        [Test]
        public void Update_ExistingUser_ShouldUpdateUserDetails()
        {
            // Arrange - Insert a user
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "originalPassword", Coins = 100 };
            _dbConnection.Execute($"INSERT INTO users (username, password, coins) VALUES (@Username, @Password, @Coins)", newUser);

            // Retrieve the inserted user
            var insertedUser = _dbConnection.QueryFirstOrDefault<User>("SELECT * FROM users WHERE username = @Username", new { newUser.Username });

            // Check if insertedUser is not null
            if (insertedUser != null)
            {
                // Modify details and Act
                insertedUser.Password = "updatedPassword";
                insertedUser.Coins = 200;
                _userRepository.Save(insertedUser);

                // Assert
                var updatedUser = _dbConnection.QueryFirstOrDefault<User>("SELECT * FROM users WHERE id = @Id", new { insertedUser.Id });
                Assert.That(updatedUser, Is.Not.Null, "User should exist in the database after update");
                Assert.That(updatedUser.Password, Is.EqualTo(insertedUser.Password), "Password should be updated");
                Assert.That(updatedUser.Coins, Is.EqualTo(insertedUser.Coins), "Coins should be updated");
            }
            else
            {
                Assert.Fail("Inserted user was not found in the database");
            }
        }

        /// <summary>
        /// Validates user credentials and logs user in
        /// </summary>
        [Test]
        public void LoginUser_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange - Insert a user with a hashed password
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = BCrypt.Net.BCrypt.HashPassword("validPassword") };
            _dbConnection.Execute($"INSERT INTO users (username, password) VALUES (@Username, @Password)", newUser);

            // Act
            User loginUser = _userService.LoginUser(newUser.Username, "validPassword");

            // Assert
            Assert.That(loginUser, Is.Not.Null, "User should not be null");
            Assert.That(loginUser.Username, Is.EqualTo(newUser.Username), "Usernames should match");
        }

        /// <summary>
        /// Tries to login with an invalid password, should throw exception and respond with error
        /// </summary>
        [Test]
        public void LoginUser_WithInvalidCredentials_ShouldThrowException()
        {
            // Arrange - Create and register a user with a unique username
            var validUsername = "testUser_" + Guid.NewGuid().ToString();
            var validUser = new User { Username = validUsername, Password = "validPassword" };
            _userService.RegisterUser(validUser);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _userService.LoginUser(validUsername, "invalidPassword"));
            Assert.That(ex.Message, Is.EqualTo("Invalid username or password."));
        }

        /// <summary>
        /// Registers a new user and saves user data to the database
        /// </summary>
        [Test]
        public void RegisterUser_WithNewUser_ShouldCreateUserAndRelatedData()
        {
            // Arrange
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "testPassword" };

            // Act
            _userService.RegisterUser(newUser);

            // Assert - Check if the user is added to the database
            var userInDb = _dbConnection.Query<User>($"SELECT * FROM users WHERE username = @Username", new { newUser.Username }).FirstOrDefault();
            Assert.That(userInDb, Is.Not.Null, "User should be added to the database");
            Assert.That(userInDb.Username, Is.EqualTo(newUser.Username), "Username should match");

            // Assert - Check if the user profile is created
            var userProfileInDb = _dbConnection.Query<UserProfile>($"SELECT * FROM userprofiles WHERE userid = @UserId", new { UserId = userInDb.Id }).FirstOrDefault();
            Assert.That(userProfileInDb, Is.Not.Null, "User profile should be created for the new user");

            // Assert - Check if the user stats are created
            var userStatsInDb = _dbConnection.Query<UserStats>($"SELECT * FROM userstats WHERE userid = @UserId", new { UserId = userInDb.Id }).FirstOrDefault();
            Assert.That(userStatsInDb, Is.Not.Null, "User stats should be created for the new user");
        }

        /// <summary>
        /// Tries to register an username that already exists, should throw exception and respond with error
        /// </summary>
        [Test]
        public void RegisterUser_WithExistingUsername_ShouldThrowException()
        {
            // Arrange - Create and register a user with a unique username
            var existingUsername = "testUser_" + Guid.NewGuid().ToString();
            var existingUser = new User { Username = existingUsername, Password = "password" };
            _userService.RegisterUser(existingUser);

            // Attempt to register a new user with the same username
            var newUserWithSameUsername = new User { Username = existingUsername, Password = "newPassword" };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _userService.RegisterUser(newUserWithSameUsername));
            Assert.That(ex.Message, Is.EqualTo("Username already exists!"));
        }

        /// <summary>
        /// Deletes created users, user stats and user profiles from the database
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            // Cleanup all test users and related data
            _dbConnection.Execute("DELETE FROM userprofiles WHERE userid IN (SELECT id FROM users WHERE username LIKE 'testUser_%')");
            _dbConnection.Execute("DELETE FROM userstats WHERE userid IN (SELECT id FROM users WHERE username LIKE 'testUser_%')");
            _dbConnection.Execute("DELETE FROM users WHERE username LIKE 'testUser_%'");
        }


    }
}