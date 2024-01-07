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
    }
}