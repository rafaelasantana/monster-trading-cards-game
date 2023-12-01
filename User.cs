using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace mtcg
{
    public class User
    {
        public int Id {get; set;}
        public string Username { get; set; }
        public string Password { get; set; }

        // Parameterless default constructor
        public User()
        {
        }
        public User(string username, string password)
        {
            // initialize Id as 0 before User is saved to the database
            Id = 0;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }
    }
}