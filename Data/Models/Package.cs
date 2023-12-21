using Newtonsoft.Json.Linq;

namespace mtcg.Data.Models
{
    public class Package
    {
        public int Id { get; set; }
        public int Price { get; set; }
        public int? OwnerId  { get; set; }
        private List<Card> Cards;

        public Package()
        {}
        /// <summary>
        /// Creates a package of cards based on json request
        /// </summary>
        /// <param name="json"></param>
        public Package(string json)
        {
            Console.WriteLine("Creating package from json...");
            // set Id to 0 (will be updated once saved to the database)
            Id = 0;
            // set OwnerId to null
            OwnerId = null;
            // set price to 5 coins
            Price = 5;

            // add cards from json request
            AddCardsFromJson(json);

            // print cards
            PrintCards();
        }

        private void AddCardsFromJson(string json)
        {
            Console.WriteLine("In AddCardsFromJson...");
            Console.WriteLine("received JSON:");
            Console.WriteLine(json);
            // initialize list of cards
            Cards = [];

            JArray cardArray = JArray.Parse(json);
            foreach (var cardData in cardArray)
            {
                Console.WriteLine("Checking json card data");
                string id = cardData["Id"].ToString();
                Console.WriteLine($"Id: { id }");
                string name = cardData["Name"].ToString();
                Console.WriteLine($"name: { name }");
                double damage = Convert.ToDouble(cardData["Damage"]);
                Console.WriteLine($"damage: { damage }");

                Element elementType = GetElementTypeFromName(name);

                Console.WriteLine("Parsed card.");

                Card card;

                if (name.Contains("Spell"))
                {
                    // card = new SpellCard(id, name, damage, (Element)Enum.Parse(typeof(Element), elementType), GetSpellTypeFromName(name));
                    // create a spell card
                    SpellType spellType = GetSpellTypeFromName(name);
                    card = new SpellCard(id, name, damage, elementType, spellType);
                    Console.WriteLine("created new spell card");
                }
                else {
                    // card = new MonsterCard(id, name, damage, (Element)Enum.Parse(typeof(Element), elementType), GetMonsterTypeFromName(name));
                    Console.WriteLine("created new monster card");
                    // create a monster card
                    MonsterType monsterType = GetMonsterTypeFromName(name);
                    card = new MonsterCard(id, name, damage, elementType, monsterType);
                    Console.WriteLine("created new monster card");
                }
                Cards.Add(card);
                Console.WriteLine("Added card to package");
            }
        }

        /// <summary>
        /// Returns the element type referenced in the name or the normal type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Element GetElementTypeFromName(string name)
        {
            if (name.Contains("Water"))
            {
                return Element.Water;
            }
            else if (name.Contains("Fire"))
            {
                return Element.Fire;
            }
            return Element.Normal;
        }

        /// <summary>
        /// Returns the spell type referenced in the name or the normal spell
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Spell Type</returns>
        private SpellType GetSpellTypeFromName(string name)
        {
            if (name.Contains("Fire"))
            {
                return SpellType.FireSpell;
            }
            else if (name.Contains("Water"))
            {
                return SpellType.WaterSpell;
            }
            return SpellType.NormalSpell;
        }

        /// <summary>
        /// Returns the monster type referenced in the name or the normal monster type
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Monster Type</returns>
        private MonsterType GetMonsterTypeFromName(string name)
        {
            switch (name)
            {
                case string n when n.Contains("Goblin"):
                    return MonsterType.Goblin;
                case string n when n.Contains("Troll"):
                    return MonsterType.Troll;
                case string n when n.Contains("Knight"):
                    return MonsterType.Knight;
                case string n when n.Contains("Kraken"):
                    return MonsterType.Kraken;
                case string n when n.Contains("FireElf"):
                    return MonsterType.FireElf;
                case string n when n.Contains("Dragon"):
                    return MonsterType.Dragon;
                case string n when n.Contains("Ork"):
                    return MonsterType.Ork;
                case string n when n.Contains("Wizard"):
                    return MonsterType.Wizard;

                // Default to Normal if no specific MonsterType is identified
                default:
                    return MonsterType.Normal;
            }
        }

        public void PrintCards()
        {
            Console.WriteLine($"Created Package with Id: { Id }, Price: { Price }, OwnerId: { OwnerId }");
            foreach (var card in Cards)
            {
                Console.WriteLine($"Created Card - Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, ElementType: {card.ElementType}");

                if (card is MonsterCard monsterCard)
                {
                    Console.WriteLine($"Monster Type: {monsterCard.MonsterType}");
                }
                else if (card is SpellCard spellCard)
                {
                    Console.WriteLine($"Spell Type: {spellCard.SpellType}");
                }

                Console.WriteLine();
            }
        }

        public List<Card> GetCards()
        {
            return Cards;
        }

    }
}