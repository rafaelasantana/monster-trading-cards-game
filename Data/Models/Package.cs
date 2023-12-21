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

            // Parse cards from json
            JArray cardArray = JArray.Parse(json);
            foreach (var cardData in cardArray)
            {
                string id = cardData["Id"].ToString();
                string name = cardData["Name"].ToString();
                double damage = Convert.ToDouble(cardData["Damage"]);

                string elementType = GetElementTypeFromName(name);

                // create card
                Card card;

                if (name.Contains("Spell"))
                {
                    // create a spell card
                    card = new Card(id, name, damage, elementType, "Spell");
                }
                else {
                    // create a monster card
                    card = new Card(id, name, damage, elementType, "Monster");
                }
                Cards.Add(card);
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
        /// Prints all cards in this package
        /// </summary>
        public void PrintCards()
        {
            foreach (var card in Cards)
            {
                Console.WriteLine($"Created Card - Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, ElementType: {card.ElementType}, CardType: {card.CardType}");
            }
        }

        /// <summary>
        /// Returns all cards in this package as a list
        /// </summary>
        /// <returns></returns>
        public List<Card> GetCards()
        {
            return Cards;
        }

    }
}