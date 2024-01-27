namespace MTCG.Data.Models
{
    public class Scoreboard
    {
        public string? Username { get; set; }
        public int? EloRating { get; set; }
        public int? Wins { get; set; }
        public int? Losses { get; set; }
        public Scoreboard()
        {}
    }
}