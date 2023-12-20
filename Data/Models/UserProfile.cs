using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mtcg.Data.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Image { get; set; }

        public UserProfile()
        {}

        public UserProfile(int userId, string name, string bio, string image)
        {
            UserId = userId;
            Name = name;
            Bio = bio;
            Image = image;
        }
    }
}