using MTCG.Data.Models;
using MTCG.Data.Repositories;
using MTCG.Data.Services;
using Npgsql;
using System.Data;

namespace MTCG.Test
{
    public class UserServiceTests
    {
        private DbConnectionManager _dBConnectionManager;
        private UserRepository _userRepository;
        private UserStatsRepository _userStatsRepository;
        private UserProfileRepository _userProfileRepository;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {

            _dBConnectionManager = new DbConnectionManager("Host=localhost;Port=5434;Database=mtcg-testdb;Username=mtcg-test-user;Password=mtcgpassword;");
            _userStatsRepository = new UserStatsRepository(_dBConnectionManager);
            _userProfileRepository = new UserProfileRepository(_dBConnectionManager);
            _userRepository = new UserRepository(_dBConnectionManager);
            _userService = new UserService(_userRepository, _userStatsRepository, _userProfileRepository);
        }

        /// <summary>
        /// Saves a new user to the database
        /// </summary>s
        [Test]
        public void Save_NewUser_ShouldAddUserToDatabase()
        {
            // Arrange
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "testPassword" };

            // Act
            _userService.RegisterUser(newUser);

            // Assert
            var userInDb = GetUserByUsername(newUser.Username);
            Assert.That(userInDb, Is.Not.Null, "User should be added to the database");
            Assert.That(userInDb.Username, Is.EqualTo(newUser.Username), "Username should match");
        }

        // Helper method to get user by username
        private User? GetUserByUsername(string username)
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = new NpgsqlCommand("SELECT * FROM users WHERE username = @Username", connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = command.ExecuteReader();
            User? user = null;

            if (reader.Read())
            {
                user = DataMapperService.MapToObject<User>(reader);
            }

            return user;
        }

        /// <summary>
        /// Updates an existing user on the database
        /// </summary>
        [Test]
        public void Update_ExistingUser_ShouldUpdateUserDetails()
        {
            // Arrange - Insert a user
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "originalPassword", Coins = 100 };
            _userService.RegisterUser(newUser);

            // Retrieve the inserted user
            var insertedUser = GetUserByUsername(newUser.Username);

            // Check if insertedUser is not null
            if (insertedUser != null)
            {
                // Modify details and Act
                insertedUser.Password = "updatedPassword";
                insertedUser.Coins = 200;
                _userRepository.Save(insertedUser);

                // Assert
                var updatedUser = GetUserByUsername(newUser.Username);
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
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "validPassword" };
            _userService.RegisterUser(newUser);

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
        /// Registers a new user, should create user profile and user stats
        /// </summary>
        [Test]
        public void RegisterUser_WithNewUser_ShouldCreateUserAndRelatedData()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            // Arrange
            var newUser = new User { Username = "testUser_" + Guid.NewGuid().ToString(), Password = "testPassword" };

            try
            {
                // Act
                _userService.RegisterUser(newUser);

                // Assert - Check if the user is added to the database
                using var queryUsersCommand = new NpgsqlCommand("SELECT * FROM users WHERE username = @Username", connection);
                queryUsersCommand.Parameters.AddWithValue("@Username", newUser.Username);
                User? userInDb = null;

                using (var queryUsersReader = queryUsersCommand.ExecuteReader())
                {
                    if (queryUsersReader.Read())
                    {
                        userInDb = DataMapperService.MapToObject<User>(queryUsersReader);
                    }
                }

                Assert.That(userInDb, Is.Not.Null, "User should be added to the database");
                Assert.That(userInDb.Username, Is.EqualTo(newUser.Username), "Username should match");


                // Assert - Check if the user profile is created
                using var userProfileCommand = new NpgsqlCommand("SELECT * FROM userprofiles WHERE userid = @UserId", connection);
                userProfileCommand.Parameters.AddWithValue("@UserId", userInDb.Id!);
                UserProfile? userProfileInDb = null;

                using (var userProfileReader = userProfileCommand.ExecuteReader())
                {
                    if (userProfileReader.Read())
                    {
                        userProfileInDb = DataMapperService.MapToObject<UserProfile>(userProfileReader);
                    }
                }

                Assert.That(userProfileInDb, Is.Not.Null, "User profile should be created for the new user");

                // Assert - Check if the user stats are created
                using var userStatsCommand = new NpgsqlCommand("SELECT * FROM userstats WHERE userid = @UserId", connection);
                userStatsCommand.Parameters.AddWithValue("@UserId", userInDb.Id!);
                UserStats? userStatsInDb = null;

                using (var userStatsReader = userStatsCommand.ExecuteReader())
                {
                    if (userStatsReader.Read())
                    {
                        userStatsInDb = DataMapperService.MapToObject<UserStats>(userStatsReader);
                    }
                }

                Assert.That(userStatsInDb, Is.Not.Null, "User stats should be created for the new user");
            }
            finally
            {
                // Ensure the readers are closed and dispose of resources
                connection.Close();
            }
        }

        /// <summary>
        /// Tries to register an username that already exists, should throw exception and respond with error
        /// </summary>
        [Test]
        public void RegisterUser_WithExistingUsername_ShouldThrowException()
        {
            using var connection = _dBConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

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