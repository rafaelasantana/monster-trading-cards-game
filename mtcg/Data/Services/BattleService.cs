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
        private readonly BattleLogsRepository _battleLogsRepository;
        private static readonly object _battleRequestLock = new object();

        public BattleService(
            DeckRepository deckRepository,
            BattleRepository battleRepository,
            UserStatsRepository userStatsRepository,
            BattleLogsRepository battleLogsRepository)
        {
            _deckRepository = deckRepository;
            _battleRepository = battleRepository;
            _userStatsRepository = userStatsRepository;
            _battleLogsRepository = battleLogsRepository;
        }

        /// <summary>
        /// Handles player request to enter a battle
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
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

            lock (_battleRequestLock)
            {
                // Check for an existing pending battle and attempt to join
                var pendingBattle = _battleRepository.GetPendingBattle();
                if (pendingBattle != null && pendingBattle.Player1Id != playerId)
                {
                    // Join the pending battle and conduct battle
                    _battleRepository.SetPlayerForBattle(pendingBattle.Id, playerId);
                    return ConductBattle(pendingBattle.Id);
                }
                else
                {
                    // No pending battle, create a new one
                    int? newBattleId = _battleRepository.CreatePendingBattle(playerId);
                    if (newBattleId == null) throw new InvalidOperationException("Could not create a pending battle.");
                    // Return a result indicating a pending status since no opponent yet
                    return new BattleResult { Status = BattleStatus.Pending, BattleId = newBattleId };
                }
            }
        }


        /// <summary>
        /// Conducts the battle and returns the result
        /// </summary>
        /// <param name="battleId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private BattleResult ConductBattle(int? battleId)
        {
            // Retrieve battle details
            Battle? battle = _battleRepository.GetBattleById(battleId);
            // Ensure that both player IDs are not null
            if (battle == null || battle.Player1Id == null || battle.Player2Id == null)
            {
                throw new InvalidOperationException("Battle must have two players, or battle does not exist.");
            }

            // Update battle status to ongoing
            _battleRepository.UpdateBattleStatus(battleId, "ongoing");
            // Create new battle result object
            BattleResult battleResult = new BattleResult
            {
                BattleId = battleId!.Value,
                Status = BattleStatus.Ongoing,
                Summary = "\n________ BATTLE SUMMARY ________\n"
            };
            // Start battle
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
                    break;
                }
                else if (!deckPlayer2.Any())
                {
                    battleResult.Summary += "\n *** BATTLE IS OVER *** \n \nPlayer 2's deck is empty, Player 1 WINS!\n";
                    battleIsOngoing = false;
                    battleResult.WinnerId = battle.Player1Id;
                    battleResult.LoserId = battle.Player2Id;
                    break;
                }

                // Conduct round
                RoundResult roundResult = ConductRound(deckPlayer1, deckPlayer2, battle.Player1Id.Value, battle.Player2Id.Value);
                roundResult.RoundNumber = ++roundCounter;
                // Log round for battle result
                battleResult.LogRound(roundResult);
                // Save new battle log record
                _battleLogsRepository.LogBattleRound(battleId.Value, roundCounter, roundResult.Player1CardId!, roundResult.Player2CardId!, roundResult.Details!);
                // Add round details to summary
                battleResult.Summary += roundResult.Details;
            }
            // Update battle result if it was a draw (100 rounds)
            if (battleIsOngoing)
            {
                // set winner id as null
                battleResult.WinnerId = null;
                // add result to the summary
                battleResult.Summary += "\n *** BATTLE IS OVER *** \n\n Maximum rounds achieved: it's a TIE!\n";
            }

            if (battleId.HasValue && battle.Player1Id.HasValue && battle.Player2Id.HasValue)
            {
                // Update battle record
                _battleRepository.UpdateBattleOutcome(battleId.Value, battleResult.WinnerId);
                // Calculate elo ratings and update user stats
                UpdatePlayerStats(battle.Player1Id.Value, battle.Player2Id.Value, battleResult.WinnerId);
                battleResult.PrintBattleResult();
                return battleResult;
            }
            else
            {
                // Handle the case where battleId is null
                throw new InvalidOperationException("Battle ID cannot be null when updating battle outcome.");
            }
        }

        /// <summary>
        /// Conducts a battle round between player 1 and player 2
        /// </summary>
        /// <param name="deckPlayer1"></param>
        /// <param name="deckPlayer2"></param>
        /// <param name="player1Id"></param>
        /// <param name="player2Id"></param>
        /// <returns>The round result</returns>
        private RoundResult ConductRound(List<Card> deckPlayer1, List<Card> deckPlayer2, int player1Id, int player2Id)
        {
            var random = new Random();
            // Select a random card from each player's deck
            Card? cardPlayer1 = deckPlayer1[random.Next(deckPlayer1.Count)];
            Card? cardPlayer2 = deckPlayer2[random.Next(deckPlayer2.Count)];

            // Ensure cards are valid
            if (cardPlayer1 == null || cardPlayer2 == null) throw new InvalidOperationException("Could not conduct round with an invalid card.");

            // Create a new round result object
            var roundResult = new RoundResult
            {
                Player1CardId = cardPlayer1.Id,
                Player2CardId = cardPlayer2.Id,
                Details = ""
            };

            roundResult.Details += $"\nPlayer 1 plays with {cardPlayer1.Name}, Player 2 plays with {cardPlayer2.Name}\n";

            // Apply game logic to determine the winner of the round
            Card? winnerCard = DecideWinner(cardPlayer1!, cardPlayer2!);

            if (winnerCard == null)
            {
                // It's a tie, no cards are moved
                roundResult.WinningCardId = "None";
                roundResult.Details += "\nIt's a TIE!\n";
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
                    _deckRepository.TransferCardToUsersDeck(cardPlayer1.Id!, player2Id);
                }
                else
                {
                    roundResult.Details += $"\nWinner: {cardPlayer1.Name}\n";
                    roundResult.Details += "\nPlayer 2 loses the card to Player 1\n";
                    _deckRepository.TransferCardToUsersDeck(cardPlayer2.Id!, player1Id);
                }
            }
            return roundResult;
        }

        /// <summary>
        /// Decides the winner card based on game rules
        /// </summary>
        /// <param name="card1"></param>
        /// <param name="card2"></param>
        /// <returns></returns>
        private Card? DecideWinner(Card card1, Card card2)
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
            }
            else
            {
                // Apply elemental effectiveness
                double damageCard1 = ApplyElementalEffectiveness(card1!, card2!);
                double damageCard2 = ApplyElementalEffectiveness(card2!, card1!);

                // Decide the winner based on the effective damage
                if (damageCard1 > damageCard2)
                {
                    return card1;
                }
                else if (damageCard2 > damageCard1)
                {
                    return card2;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the special rules applies to the cards and if so, returns the winner
        /// </summary>
        /// <param name="card1"></param>
        /// <param name="card2"></param>
        /// <param name="winner"></param>
        /// <returns></returns>
        private bool SpecialRuleApplies(Card card1, Card card2, out Card? winner)
        {
            winner = null;

            // Goblins are too afraid of Dragons to attack
            if (card1?.Name?.Contains("Goblin") == true && card2?.Name?.Contains("Dragon") == true)
            {
                winner = card2;
                return true;
            }
            else if (card2?.Name?.Contains("Goblin") == true && card1?.Name?.Contains("Dragon") == true)
            {
                winner = card1;
                return true;
            }

            // Wizard can control Orks so they are not able to damage them
            if (card1?.Name?.Contains("Wizard") == true && card2?.Name?.Contains("Ork") == true)
            {
                winner = card1;
                return true;
            }
            else if (card2?.Name?.Contains("Wizard") == true && card1?.Name?.Contains("Ork") == true)
            {
                winner = card2;
                return true;
            }

            // The armor of Knights is so heavy that WaterSpells make them drown instantly
            if (card1?.Name?.Contains("Knight") == true && card2?.ElementType?.Contains("Water") == true && card2?.CardType?.Contains("Spell") == true)
            {
                winner = card2;
                return true;
            }
            else if (card2?.Name?.Contains("Knight") == true && card1?.ElementType?.Contains("Water") == true && card1?.CardType?.Contains("Spell") == true)
            {
                winner = card1;
                return true;
            }

            // The Kraken is immune against spells
            if (card1?.Name?.Contains("Kraken") == true && card2?.CardType?.Contains("Spell") == true)
            {
                winner = card1;
                return true;
            }
            else if (card2?.Name?.Contains("Kraken") == true && card1?.CardType?.Contains("Spell") == true)
            {
                winner = card2;
                return true;
            }

            // The FireElves know Dragons since they were little and can evade their attacks
            if (card1?.Name?.Contains("FireElf") == true && card2?.Name?.Contains("Dragon") == true)
            {
                winner = card1;
                return true;
            }
            else if (card2?.Name?.Contains("FireElf") == true && card1?.Name?.Contains("Dragon") == true)
            {
                winner = card2;
                return true;
            }

            return winner != null;
        }

        /// <summary>
        /// Applies elemental effectiveness on the defending card
        /// </summary>
        /// <param name="attackingCard"></param>
        /// <param name="defendingCard"></param>
        /// <returns>The damage on defending card after elemental effectiveness application </returns>
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

        /// <summary>
        /// Updates the Elo rating for both players
        /// </summary>
        /// <param name="player1Id"></param>
        /// <param name="player2Id"></param>
        /// <param name="winnerId"></param>
        public void UpdatePlayerStats(int player1Id, int player2Id, int? winnerId)
        {
            int K = 30; // K-factor

            // Retrieve current Elo ratings
            UserStats? player1Stats = _userStatsRepository.GetStatsByUserId(player1Id);
            UserStats? player2Stats = _userStatsRepository.GetStatsByUserId(player2Id);

            // Ensure User Stats are valid
            if (player1Stats == null || player2Stats == null) throw new InvalidOperationException("Could not update player stats with an invalid record.");

            // Check if both players have Elo values and if not, set it to starting value of 100
            if (!player1Stats.EloRating.HasValue) player1Stats.EloRating = 100;
            if (!player2Stats.EloRating.HasValue) player2Stats.EloRating = 100;

            // Calculate expected scores
            double player1Expected = 1 / (1 + Math.Pow(10, (player2Stats.EloRating.Value - player1Stats.EloRating.Value) / 400.0));
            double player2Expected = 1 / (1 + Math.Pow(10, (player1Stats.EloRating.Value - player2Stats.EloRating.Value) / 400.0));

            // Determine actual scores based on the winner
            double player1ActualScore, player2ActualScore;
            if (winnerId.HasValue)
            {
                // Check which player won
                if (player1Id == winnerId.Value)
                {
                    player1ActualScore = 1;
                    player2ActualScore = 0;
                    // update wins and losses
                    player1Stats.Wins += 1;
                    player2Stats.Losses += 1;

                }
                else
                {
                    player1ActualScore = 0;
                    player2ActualScore = 1;
                    // update wins and losses
                    player1Stats.Losses += 1;
                    player2Stats.Wins += 1;

                }
            }
            else
            {
                // In case of a draw
                player1ActualScore = 0.5;
                player2ActualScore = 0.5;
            }

            // Calculate new Elo ratings
            int newPlayer1Rating = player1Stats.EloRating.Value + (int)(K * (player1ActualScore - player1Expected));
            int newPlayer2Rating = player2Stats.EloRating.Value + (int)(K * (player2ActualScore - player2Expected));

            // Set new Elo ratings
            player1Stats.EloRating = newPlayer1Rating;
            player2Stats.EloRating = newPlayer2Rating;

            // Increase number of games played for both
            player1Stats.TotalGamesPlayed += 1;
            player2Stats.TotalGamesPlayed += 1;

            // Update user stats in the database
            _userStatsRepository.UpdateStats(player1Stats);
            _userStatsRepository.UpdateStats(player2Stats);
        }

    }

}
