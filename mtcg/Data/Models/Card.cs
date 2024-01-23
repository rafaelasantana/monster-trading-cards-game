namespace MTCG.Data.Models
{
    public class Card
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public double? Damage { get; set; }
        public string? ElementType { get; set; }
        public string? CardType { get; set; }
        public int? PackageId { get; set; }
        public int? OwnerId { get; set; }

        public Card()
        {}

        public Card(string? id, string? name, double? damage, string? elementType, string? cardType)
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
        }
        public Card(string? id, string? name, double? damage, string? elementType, string? cardType, int? packageId, int? ownerId)
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
            PackageId = packageId;
            OwnerId = ownerId;
        }

        public void AttachToPackage(Package package)
        {
            PackageId = package.Id;
        }

    }

}