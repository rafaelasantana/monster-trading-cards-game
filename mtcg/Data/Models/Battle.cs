namespace MTCG.Data.Models
{
    public class Battle
    {
        public int? Id { get; set; }
        public int? Player1Id { get; set; }
        public int? Player2Id { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? WinnerId { get; set; }

        public Battle()
        {}
    }
}