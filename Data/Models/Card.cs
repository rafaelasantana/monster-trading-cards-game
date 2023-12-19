namespace mtcg.Data.Models
{
    public enum Element
    {
        Water,
        Fire,
        Normal
    }
    public enum MonsterType
    {
        Goblin,
        Troll,
        Knight,
        Kraken,
        FireElf,
        Dragon,
        Ork,
        Wizard,
        Normal
    }
    public enum SpellType
    {
        FireSpell,
        WaterSpell,
        NormalSpell
    }

    public class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Damage { get; set; }
        public Element ElementType { get; set; }
        public int? PackageId { get; set; }
        public int? OwnerId { get; set; }

        public Card()
        {}

        public Card(string id, string name, double damage, Element elementType)
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            // set the owner ID to null to show card is not owned by anyone yet
            // OwnerId = null;
        }

        public void AttachToPackage(Package package)
        {
            PackageId = package.Id;
        }

    }

    public class MonsterCard : Card
    {
        public MonsterType MonsterType { get; set; }

        public MonsterCard(string id, string name, double damage, Element elementType, MonsterType monsterType)
            : base(id, name, damage, elementType)
        {
            MonsterType = monsterType;
        }
    }

    public class SpellCard : Card
    {
        public SpellType SpellType { get; set; }

        public SpellCard(string id, string name, double damage, Element elementType, SpellType spellType)
            : base(id, name, damage, elementType)
        {
            SpellType = spellType;
        }
    }

}