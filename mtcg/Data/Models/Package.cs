using Newtonsoft.Json.Linq;

namespace MTCG.Data.Models
{
    public class Package
    {
        public int? Id { get; set; }
        public int? Price { get; set; }
        public int? OwnerId  { get; set; }
        private List<Card>? Cards { get; set; }

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
        }

        private void AddCardsFromJson(string json)
        {
            Cards = new List<Card>();

            JArray cardArray = JArray.Parse(json);
            foreach (var cardData in cardArray)
            {
                if (cardData["Id"] == null)
                {
                    throw new InvalidOperationException("Card data missing 'Id' field.");
                }
                string id = cardData["Id"]!.ToString();

                if (cardData["Name"] == null)
                {
                    throw new InvalidOperationException("Card data missing 'Name' field.");
                }
                string name = cardData["Name"]!.ToString();

                if (cardData["Damage"] == null)
                {
                    throw new InvalidOperationException("Card data missing 'Damage' field.");
                }

                if (!double.TryParse(cardData["Damage"]!.ToString(), out double damage))
                {
                    throw new FormatException($"Invalid format for 'Damage' field in card '{id}'.");
                }

                string elementType = GetElementTypeFromName(name);
                Card card = name.Contains("Spell") ?
                            new Card(id, name, damage, elementType, "Spell") :
                            new Card(id, name, damage, elementType, "Monster");

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
            if (Cards == null)
            {
                Console.WriteLine("No cards available.");
                return;
            }
            foreach (var card in Cards)
            {
                Console.WriteLine($"Created Card - Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, ElementType: {card.ElementType}, CardType: {card.CardType}");
            }
        }

        /// <summary>
        /// Returns all cards in this package as a list
        /// </summary>
        /// <returns></returns>
        public List<Card>? GetCards()
        {
            return Cards;
        }

    }
}