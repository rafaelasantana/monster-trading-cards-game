using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace mtcg.Controllers
{
    public class PackageCreator
    {
        public List<Card> CreatePackage(string jsonData)
        {
            // create list of cards
            List<Card> cards = [];

            // get cards from request
            JArray cardArray = JArray.Parse(jsonData);

            foreach(var cardData in cardArray)
            {
                // extract card properties
                string id = cardData["Id"]!.ToString();
                string name = cardData["Name"]!.ToString();
                double damage = Convert.ToDouble(cardData["Damage"]);

                // determine element type based on name
                Element elementType = GetElementTypeFromName(name);

                // create card based on the name
                Card card;
                if (name.Contains("Spell"))
                {
                    // create a spell card
                    SpellType spellType = GetSpellTypeFromName(name);
                    card = new SpellCard(id, name, damage, elementType, spellType);
                }
                else {
                    // create a monster card
                    MonsterType monsterType = GetMonsterTypeFromName(name);
                    card = new MonsterCard(id, name, damage, elementType, monsterType);
                }

                // add card to the package
                cards.Add(card);
            }
            return cards;
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

    }
}