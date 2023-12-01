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
        public string PasswordHash { get; set; }

        public User(string username, string passwordHash)
        {
            // initialize Id as 0 before User is saved to the database
            Id = 0;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        }
    }
}