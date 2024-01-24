using MTCG.Data.Repositories;
using MTCG.Data.Models;
using System.Diagnostics;
using System.Data;

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
            Debug.WriteLine("In request battle...");
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
                Console.WriteLine("Will add player2 to pending battle");
                // Join the pending battle
                _battleRepository.SetPlayerForBattle(pendingBattle.Id, playerId);
                return ConductBattle(pendingBattle.Id);
            }
            else
            {
                Console.WriteLine("Will create new pending battle for player1");
                // No pending battle, create a new one
                var newBattleId = _battleRepository.CreatePendingBattle(playerId);
                // Return a result indicating a pending status since no opponent yet
                return new BattleResult { Status = BattleStatus.Pending, BattleId = newBattleId };
            }
        }

        private BattleResult ConductBattle(int? battleId)
        {
            Console.WriteLine("In conduct battle");
            Battle battle = _battleRepository.GetBattleById(battleId);
            Console.WriteLine("Got battle in ConductBattle");
            // Ensure that both player IDs are not null
            if (!battle.Player1Id.HasValue || !battle.Player2Id.HasValue)
            {
                throw new InvalidOperationException("Battle must have two players.");
            }
            // Update battle status to ongoing
            _battleRepository.UpdateBattleStatus(battleId, "ongoing");
            Console.WriteLine("updated battle status, Battle in ongoing...");

            BattleResult battleResult = new BattleResult
            {
                BattleId = battleId.Value,
                Status = BattleStatus.Ongoing,
                Summary = "\n________ BATTLE SUMMARY ________\n"
            };

            bool battleIsOngoing = true;
            int roundCounter = 1;
            while (roundCounter <= 100 && battleIsOngoing)
            {
                // Retrieve decks for both players
                List<Card> deckPlayer1 = _deckRepository.GetDeckByUserId(battle.Player1Id);
                List<Card> deckPlayer2 = _deckRepository.GetDeckByUserId(battle.Player2Id);

                // Add number of cards in each deck to the battle summary
                battleResult.Summary += $"\n---- ROUND {roundCounter} ----\n";
                battleResult.Summary += $"\nNumber of cards in Player 1's deck: {deckPlayer1.Count}\n";
                battleResult.Summary += $"\nNumber of cards in Player 2's deck: {deckPlayer2.Count}\n";

                // Check if either deck is empty
                if (!deckPlayer1.Any())
                {
                    battleResult.Summary += "\n *** BATTLE IS OVER *** \n \nPlayer 1's deck is empty, Player 2 WINS!\n";
                    battleIsOngoing = false;
                    battleResult.WinnerId = battle.Player2Id;
                    battleResult.LoserId = battle.Player1Id;
                    // Update battle status
                    _battleRepository.UpdateBattleStatus(battleId, "completed");
                    break;
                }
                else if (!deckPlayer2.Any())
                {
                    battleResult.Summary += "\n *** BATTLE IS OVER *** \n \nPlayer 2's deck is empty, Player 1 WINS!\n";
                    battleIsOngoing = false;
                    battleResult.WinnerId = battle.Player1Id;
                    battleResult.LoserId = battle.Player2Id;
                    // Update battle status
                    _battleRepository.UpdateBattleStatus(battleId, "completed");
                    break;
                }

                // Conduct round
                RoundResult roundResult = ConductRound(deckPlayer1, deckPlayer2, battle.Player1Id.Value, battle.Player2Id.Value);
                roundResult.RoundNumber = ++roundCounter;
                // Log round
                battleResult.LogRound(roundResult);
                // Add round details to summary
                battleResult.Summary += roundResult.Details;
            }
            // Update battle status if it reached 100 rounds
            if (battleIsOngoing)
            {
                _battleRepository.UpdateBattleStatus(battleId, "completed");
                battleResult.Summary += "\n *** BATTLE IS OVER *** \n\n Maximum rounds achieved: it's a TIE!\n";
            }
            // TODO update battle record with endtime, winnerid
            // TODO update scoreboard, elo
            Console.WriteLine("Battle is finished, results:");
            battleResult.PrintBattleResult();
            return battleResult;
        }


        private RoundResult ConductRound(List<Card> deckPlayer1, List<Card> deckPlayer2, int player1Id, int player2Id)
        {
            var random = new Random();
            // Select a random card from each player's deck
            Card cardPlayer1 = deckPlayer1[random.Next(deckPlayer1.Count)];
            Card cardPlayer2 = deckPlayer2[random.Next(deckPlayer2.Count)];

            // Create a new round result object
            var roundResult = new RoundResult
            {
                Player1CardId = cardPlayer1.Id,
                Player2CardId = cardPlayer2.Id,
                Details = ""
            };

            roundResult.Details += $"\nPlayer 1 plays with {cardPlayer1.Name}, Player 2 plays with {cardPlayer2.Name}\n";

            // Apply game logic to determine the winner of the round
            Card winnerCard = DecideWinner(cardPlayer1, cardPlayer2);

            if (winnerCard == null)
            {
                // It's a tie, no cards are moved
                roundResult.WinningCardId = "None";
                roundResult.Details += "It's a tie!\n";
            }
            else
            {
                roundResult.WinningCardId = winnerCard.Id;
                // Determine the loser card
                Card loserCard = winnerCard == cardPlayer1 ? cardPlayer2 : cardPlayer1;
                int loserId = winnerCard == cardPlayer1 ? player2Id : player1Id;

                // Transfer loser's card to winner's deck
                if (loserCard == cardPlayer1)
                {
                    roundResult.Details += $"\nWinner: {cardPlayer2.Name}\n";
                    roundResult.Details += "\nPlayer 1 loses the card to Player 2\n";
                    _deckRepository.TransferCardToUsersDeck(cardPlayer1.Id, player2Id);
                }
                else
                {
                    roundResult.Details += $"\nWinner: {cardPlayer1.Name}\n";
                    roundResult.Details += "\nPlayer 2 loses the card to Player 1\n";
                    _deckRepository.TransferCardToUsersDeck(cardPlayer2.Id, player1Id);
                }
            }
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
