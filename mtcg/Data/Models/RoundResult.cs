namespace MTCG.Data.Models
{
    public class RoundResult
    {
        public int RoundNumber { get; set; }
        public int Player1CardId { get; set; }
        public int Player2CardId { get; set; }
        public int WinningCardId { get; set; }
        public string Details { get; set; }

        public RoundResult(int roundNumber, int player1CardId, int player2CardId, int winningCardId, string details)
        {
            RoundNumber = roundNumber;
            Player1CardId = player1CardId;
            Player2CardId = player2CardId;
            WinningCardId = winningCardId;
            Details = details;
        }
    }
}