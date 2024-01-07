using MTCG.Data.Repositories;
using MTCG.Data.Models;

namespace MTCG.Data.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;
        private readonly UserStatsRepository _userStatsRepository;
        private readonly UserProfileRepository _userProfileRepository;

        public UserService(UserRepository userRepository, UserStatsRepository userStatsRepository, UserProfileRepository userProfileRepository)
        {
            _userRepository = userRepository;
            _userStatsRepository = userStatsRepository;
            _userProfileRepository = userProfileRepository;
        }

        /// <summary>
        /// Creates a new user with user profile and user stats records
        /// </summary>
        /// <param name="newUser"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterUser(User newUser)
        {
            if (newUser == null || string.IsNullOrWhiteSpace(newUser.Username))
                throw new ArgumentException("Invalid user data");

            if (_userRepository.GetByUsername(newUser.Username) != null)
                throw new InvalidOperationException("Username already exists!");

            _userRepository.Save(newUser);

            UserProfile newUserProfile = new(newUser.Id, null, null, null);
            _userProfileRepository.CreateUserProfile(newUserProfile);
            _userStatsRepository.CreateStats(newUser.Id);
        }

        /// <summary>
        /// Checks credentials for a registered user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public User LoginUser(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                throw new InvalidOperationException("Invalid username or password.");

            return user;
        }
    }

}