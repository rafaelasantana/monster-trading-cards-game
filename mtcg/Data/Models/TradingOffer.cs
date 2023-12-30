namespace mtcg.Data.Models
{
    public class TradingOffer
    {
        public string? Id { get; set; }
        public int? OwnerId { get; set; }
        public string? CardId { get; set; }
        public string? RequestedType { get; set; }
        public int? MinDamage { get; set; }
        public string? Status { get; set; }

        public TradingOffer()
        {}
    }
}