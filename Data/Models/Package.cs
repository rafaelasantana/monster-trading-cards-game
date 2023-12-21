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
            Console.WriteLine(json);
            // initialize list of cards
            Cards = [];

            JArray cardArray = JArray.Parse(json);
            foreach (var cardData in cardArray)
            {
                string id = cardData["Id"].ToString();
                string name = cardData["Name"].ToString();
                double damage = Convert.ToDouble(cardData["Damage"]);

                string elementType = GetElementTypeFromName(name);

                Console.WriteLine("Parsed card.");

                Card card;

                if (name.Contains("Spell"))
                {
                    // create a spell card
                    // string spellType = GetSpellTypeFromName(name);
                    card = new Card(id, name, damage, elementType, "Spell");
                    Console.WriteLine("created new spell card");
                }
                else {
                    // card = new MonsterCard(id, name, damage, (Element)Enum.Parse(typeof(Element), elementType), GetMonsterTypeFromName(name));
                    // create a monster card
                    // string monsterType = GetMonsterTypeFromName(name);
                    card = new Card(id, name, damage, elementType, "Monster");
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
        private static string GetElementTypeFromName(string name)
        {
            if (name.Contains("Water"))
            {
                return "Water";
            }
            else if (name.Contains("Fire"))
            {
                return "Fire";
            }
            return "Normal";
        }

        /// <summary>
        /// Returns the spell type referenced in the name or the normal spell
        /// </summary>
        /// <param name="name"></param>
        /// <returns>string</returns>
        private static string GetSpellTypeFromName(string name)
        {
            if (name.Contains("Fire"))
            {
                return "Fire";
            }
            else if (name.Contains("Water"))
            {
                return "Water";
            }
            return "Normal";
        }

        /// <summary>
        /// Returns the monster type referenced in the name or the normal monster type
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Monster Type</returns>
        private string GetMonsterTypeFromName(string name)
        {
            switch (name)
            {
                case string n when n.Contains("Goblin"):
                    return "Goblin";
                case string n when n.Contains("Troll"):
                    return "Troll";
                case string n when n.Contains("Knight"):
                    return "Knight";
                case string n when n.Contains("Kraken"):
                    return "Kraken";
                case string n when n.Contains("FireElf"):
                    return "FireElf";
                case string n when n.Contains("Dragon"):
                    return "Dragon";
                case string n when n.Contains("Ork"):
                    return "Ork";
                case string n when n.Contains("Wizard"):
                    return "Wizard";

                // Default to Normal if no specific MonsterType is identified
                default:
                    return "Normal";
            }
        }

        public void PrintCards()
        {
            foreach (var card in Cards)
            {
                Console.WriteLine($"Created Card - Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, ElementType: {card.ElementType}, CardType: {card.CardType}");
                Console.WriteLine();
            }
        }

        public List<Card> GetCards()
        {
            return Cards;
        }

    }
}