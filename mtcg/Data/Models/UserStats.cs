using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mtcg.Data.Models
{
    public class UserStats
    {
        public int? UserId { get; set; }
        public int? EloRating { get; set; }
        public int? Wins { get; set; }
        public int? Losses { get; set; }
        public int? TotalGamesPlayed { get; set; }

        public UserStats()
        {}

    }
}