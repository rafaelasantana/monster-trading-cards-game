namespace mtcg.Data.Models
{
    public class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Damage { get; set; }
        public string ElementType { get; set; }
        public string CardType { get; set; }
        public int? PackageId { get; set; }
        public int? OwnerId { get; set; }

        public Card()
        {}

        public Card(string id, string name, double damage, string elementType, string cardType)
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
        }

        public void AttachToPackage(Package package)
        {
            PackageId = package.Id;
        }

    }

}