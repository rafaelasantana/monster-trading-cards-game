namespace mtcg
{
    public abstract class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Damage { get; set; }
        public Element ElementType { get; set; }
    }

    public class MonsterCard : Card
    {
    }

    public class SpellCard : Card
    {

    }

    public enum Element {
        Water,
        Fire,
        Normal
    }
}