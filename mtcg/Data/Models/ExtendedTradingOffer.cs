namespace MTCG.Data.Models
{
    public class ExtendedTradingOffer
    {
        public string? Id { get; set; }
        public int? OwnerId { get; set; }
        public string? CardId { get; set; }
        public string? CardName { get; set; }
        public double? Damage { get; set; }
        public string? ElementType { get; set; }
        public string? RequestedType { get; set; }
        public int? MinDamage { get; set; }
        public string? Status { get; set; }

        public ExtendedTradingOffer()
        {}
    }
}