using MTCG.Data.Repositories;
using MTCG.Data.Models;

namespace MTCG.Data.Services
{
    public class BattleService
    {
        private readonly DeckRepository _deckRepository;
        private readonly BattleRepository _battleRepository;
        private readonly UserStatsRepository _userStatsRepository;

        public BattleService(
            DeckRepository deckRepository,
            BattleRepository battleRepository,
            UserStatsRepository userStatsRepository)
        {
            _deckRepository = deckRepository;
            _battleRepository = battleRepository;
            _userStatsRepository = userStatsRepository;
        }

        public BattleResult RequestBattle(int? playerId)
        {
            if (!playerId.HasValue)
            {
                throw new ArgumentNullException(nameof(playerId), "Player ID cannot be null.");
            }

            // Validate the player's deck
            var playerDeck = _deckRepository.GetDeckByUserId(playerId);
            if (playerDeck == null || !playerDeck.Any())
                throw new InvalidOperationException("Player does not have a valid deck.");

            // Check for an existing pending battle and attempt to join
            var pendingBattle = _battleRepository.GetPendingBattle();
            if (pendingBattle != null && pendingBattle.Player1Id != playerId)
            {
                // Join the pending battle
                _battleRepository.SetPlayerForBattle(pendingBattle.Id, playerId);
                return ConductBattle(pendingBattle.Id);
            }
            else
            {
                // No pending battle, create a new one
                var newBattleId = _battleRepository.CreatePendingBattle(playerId);
                // Return a result indicating a pending status since no opponent yet
                return new BattleResult { Status = BattleStatus.Pending, BattleId = newBattleId };
            }
        }

        private BattleResult ConductBattle(int? battleId)
        {
            Battle battle = _battleRepository.GetBattleById(battleId);
            // Update battle status to ongoing
            _battleRepository.UpdateBattleStatus(battleId, "Ongoing");
            Console.WriteLine("Battle in ongoing...");

            // Retrieve decks for both players
            List<Card> deckPlayer1 = _deckRepository.GetDeckByUserId(battle.Player1Id);
            List<Card> deckPlayer2 = _deckRepository.GetDeckByUserId(battle.Player2Id);
            Console.WriteLine("Got decks from both users");

            BattleResult battleResult = new BattleResult
            {
                BattleId = battleId.Value,
                Status = BattleStatus.Ongoing
            };

            bool battleIsOngoing = true;
            int roundCounter = 0;
            while (battleIsOngoing)
            {
                Console.WriteLine("Will conduct round:");
                RoundResult roundResult = ConductRound(deckPlayer1, deckPlayer2);
                roundResult.RoundNumber = ++roundCounter;
                battleResult.LogRound(roundResult);
                battleIsOngoing = false;
            }
            return battleResult;
        }

        private RoundResult ConductRound(List<Card> deckPlayer1, List<Card> deckPlayer2)
        {
            // Select a random card from each player's deck
            var cardPlayer1 = deckPlayer1[new Random().Next(deckPlayer1.Count)];
            var cardPlayer2 = deckPlayer2[new Random().Next(deckPlayer2.Count)];
            Console.WriteLine("Got random card from both players");

            // Apply game logic to determine the winner of the round
            // For example, compare card damage and apply elemental effectiveness
            Card winner = DecideWinner(cardPlayer1, cardPlayer2);
            Console.WriteLine("decided winner");

            // Create a new round result object
            var roundResult = new RoundResult
            {
                Player1CardId = cardPlayer1.Id,
                Player2CardId = cardPlayer2.Id,
                WinningCardId = winner.Id
            };

            // Remove the defeated card from loser's deck
            // And apply other round effects as needed

            return roundResult;
        }

        private Card DecideWinner(Card card1, Card card2)
        {
            // Apply special rules first (e.g., Goblins are afraid of Dragons)
            if (SpecialRuleApplies(card1, card2, out var specialRuleWinner))
            {
                return specialRuleWinner;
            }

            // Check if both cards are monsters, in which case elemental effectiveness is not applied
            if (card1.CardType == "Monster" && card2.CardType == "Monster")
            {
                if (card1.Damage > card2.Damage) return card1;
                else if (card2.Damage > card1.Damage) return card2;
                else return null;
            }
            else
            {
                // Apply elemental effectiveness
                double damageCard1 = ApplyElementalEffectiveness(card1, card2);
                double damageCard2 = ApplyElementalEffectiveness(card2, card1);

                // Decide the winner based on the effective damage
                if (damageCard1 > damageCard2)
                {
                    return card1;
                }
                else if (damageCard2 > damageCard1)
                {
                    return card2;
                }
                else
                {
                    // In case of a tie, return null
                    return null;
                }
            }
        }

        /// <summary>
        /// Checks if the special rules applies to the cards and if so, returns the winner
        /// </summary>
        /// <param name="card1"></param>
        /// <param name="card2"></param>
        /// <param name="winner"></param>
        /// <returns></returns>
        private bool SpecialRuleApplies(Card card1, Card card2, out Card winner)
        {
            winner = null;

            // Goblins are too afraid of Dragons to attack
            if (card1.Name.Contains("Goblin") && card2.Name.Contains("Dragon"))
            {
                winner = card2;
                return true;
            }
            else if (card2.Name.Contains("Goblin") && card1.Name.Contains("Dragon"))
            {
                winner = card1;
                return true;
            }

            // Wizard can control Orks so they are not able to damage them
            if (card1.Name.Contains("Wizard") && card2.Name.Contains("Ork"))
            {
                winner = card1;
                return true;
            }
            else if (card2.Name.Contains("Wizard") && card1.Name.Contains("Ork"))
            {
                winner = card2;
                return true;
            }

            // The armor of Knights is so heavy that WaterSpells make them drown instantly
            if (card1.Name.Contains("Knight") && card2.ElementType == "Water" && card2.CardType == "Spell")
            {
                winner = card2;
                return true;
            }
            else if (card2.Name.Contains("Knight") && card1.ElementType == "Water" && card1.CardType == "Spell")
            {
                winner = card1;
                return true;
            }

            // The Kraken is immune against spells
            if (card1.Name.Contains("Kraken") && card2.CardType == "Spell")
            {
                winner = card1;
                return true;
            }
            else if (card2.Name.Contains("Kraken") && card1.CardType == "Spell")
            {
                winner = card2;
                return true;
            }

            // The FireElves know Dragons since they were little and can evade their attacks
            if (card1.Name.Contains("FireElf") && card2.Name.Contains("Dragon"))
            {
                winner = card1;
                return true;
            }
            else if (card2.Name.Contains("FireElf") && card1.Name.Contains("Dragon"))
            {
                winner = card2;
                return true;
            }

            return winner != null;
        }

        private double ApplyElementalEffectiveness(Card attackingCard, Card defendingCard)
        {
            double damage = attackingCard.Damage ?? 0;

            switch (attackingCard.ElementType)
            {
                case "Water":
                    if (defendingCard.ElementType == "Fire")
                        damage *= 2; // Water is effective against Fire
                    if (defendingCard.ElementType == "Normal")
                        damage /= 2; // Water is not effective against Normal
                    break;
                case "Fire":
                    if (defendingCard.ElementType == "Normal")
                        damage *= 2; // Fire is effective against Normal
                    if (defendingCard.ElementType == "Water")
                        damage /= 2; // Fire is not effective against Water
                    break;
                case "Normal":
                    if (defendingCard.ElementType == "Water")
                        damage *= 2; // Normal is effective against Water
                    if (defendingCard.ElementType == "Fire")
                        damage /= 2; // Normal is not effective against Fire
                    break;
            }

            return damage;
        }

    }

}
