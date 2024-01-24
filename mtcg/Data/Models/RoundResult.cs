namespace MTCG.Data.Models
{
    public class RoundResult
    {
        public int? RoundNumber { get; set; }
        public string? Player1CardId { get; set; }
        public string? Player2CardId { get; set; }
        public string? WinningCardId { get; set; }
        public string? Details { get; set; }

        public RoundResult()
        {}
        public RoundResult(int roundNumber, string player1CardId, string player2CardId, string winningCardId, string details)
        {
            RoundNumber = roundNumber;
            Player1CardId = player1CardId;
            Player2CardId = player2CardId;
            WinningCardId = winningCardId;
            Details = details;
        }

        public void PrintRoundResult()
        {
            // public int? RoundNumber { get; set; }
            // public string? Player1CardId { get; set; }
            // public string? Player2CardId { get; set; }
            // public string? WinningCardId { get; set; }
            // public string? Details { get; set; }

            Console.WriteLine($"RoundNumber: {RoundNumber}");
            Console.WriteLine($"Player1CardId: {Player1CardId}");
            Console.WriteLine($"Player2CardId: {Player2CardId}");
            Console.WriteLine($"WinningCardId: {WinningCardId}");
            Console.WriteLine($"Details: {Details}");
        }
    }
}