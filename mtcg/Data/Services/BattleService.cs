using MTCG.Data.Repositories;

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
                Console.WriteLine("Added player as P2 to the battle, will conduct battle...");
                return ConductBattle(pendingBattle.Id);
            }
            else
            {
                // No pending battle, create a new one
                var newBattleId = _battleRepository.CreatePendingBattle(playerId);
                Console.WriteLine("Created a new pending battle");
                // Return a result indicating a pending status since no opponent yet
                return new BattleResult { Status = BattleStatus.Pending, BattleId = newBattleId };
            }
        }

        private BattleResult ConductBattle(int battleId)
        {
            // Conduct the battle logic here
            // Return the result of the battle
        }
    }

}
