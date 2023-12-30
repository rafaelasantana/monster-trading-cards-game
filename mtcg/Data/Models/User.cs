using Newtonsoft.Json;

namespace MTCG.Data.Models
{
    public class User
    {
        public int? Id {get; set;}
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? Coins { get; set;}

        /// <summary>
        /// Parameterless default constructor
        /// </summary>
        public User()
        {
        }
        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public User(string? username, string? password)
        {
            Username = username;
            Password = password;
        }
    }
}