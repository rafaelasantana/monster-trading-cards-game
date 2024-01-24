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

        public void PrintBattleResult()
        {
        //             public int? BattleId { get; set; }
        // public BattleStatus Status { get; set; }
        // public int? WinnerId { get; set; }
        // public int? LoserId { get; set; }
        // public List<RoundResult> Rounds { get; private set; }
        // public string? Summary { get; set; }
            Console.WriteLine($"BattleId: {BattleId}");
            Console.WriteLine($"Status: {Status}");
            Console.WriteLine($"WinnerId: {WinnerId}");
            Console.WriteLine($"LoserId: {LoserId}");
            Console.WriteLine("Rounds:");
            foreach (var round in Rounds)
            {
                round.PrintRoundResult();
            }

            Console.WriteLine($"Summary: {Summary}");
        }
    }
}
