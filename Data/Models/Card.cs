namespace mtcg
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

    public abstract class Card(string id, string name, double damage, Element elementType)
    {
        public string Id { get; set; } = id;
        public string Name { get; set; } = name;
        public double Damage { get; set; } = damage;
        public Element ElementType { get; set; } = elementType;

        public void PrintCard()
        {
            Console.WriteLine($"Card ID: {Id}, Name: {Name}, Damage: {Damage}, Element: {ElementType}");
        }

        public double CalculateDamageAgainst(Card opponent)
        {
            return 0;
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