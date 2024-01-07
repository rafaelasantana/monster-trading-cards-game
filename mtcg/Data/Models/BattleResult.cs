namespace MTCG.Data.Models
{
    public enum BattleStatus
    {
        Pending,
        Ongoing,
        Completed
    }

    public class BattleResult
    {
        public int? BattleId { get; set; }
        public BattleStatus Status { get; set; }
        public int? WinnerId { get; set; }
        public int? LoserId { get; set; }
        public List<RoundResult> Rounds { get; private set; }
        public string? Summary { get; set; }

        public BattleResult()
        {
            Rounds = new List<RoundResult>();
        }

        public void LogRound(RoundResult roundResult)
        {
            Rounds.Add(roundResult);
        }
    }
}
