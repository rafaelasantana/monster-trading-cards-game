using System;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class CardRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _Table = "cards";
        private readonly string _Fields = "id, name, damage, elementType, isMonster, packageId, ownerId";

        public CardRepository(DbConnectionManager dbConnectionManager)
        {
            _dbConnectionManager = dbConnectionManager;
        }

        /// <summary>
        /// Saves a new card to the database or updates an existing card
        /// </summary>
        /// <param name="card"></param>
        public void Save(Card card)
        {
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Determine if the card is a MonsterCard
            int isMonster = card is MonsterCard ? 1 : 0;

            // Prepare the card for insertion or update
            var cardToSave = new {
                card.Id,
                card.Name,
                card.Damage,
                ElementType = (int)card.ElementType,
                isMonster,
                card.PackageId,
                card.OwnerId
            };

            // check if card exists on the database
            int count = connection.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {_Table} WHERE id = @Id", new { card.Id });
            if (count == 0)
            {
                // insert new record
                var query = $"INSERT INTO {_Table} ({_Fields}) VALUES (@Id, @Name, @Damage, @ElementType, @IsMonster, @PackageId, @OwnerId)";
                connection.Execute(query, cardToSave);
            }
            else
            {
                // update existing card
                Update(card);
            }
        }

        /// <summary>
        /// Updates an existing card on the database
        /// </summary>
        /// <param name="card"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Update(Card card)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // Determine if the card is a MonsterCard
            int isMonster = card is MonsterCard ? 1 : 0;

            // Prepare the card for updating
            var cardToUpdate = new {
                card.Id,
                card.Name,
                card.Damage,
                ElementType = (int)card.ElementType,
                isMonster,
                card.PackageId,
                card.OwnerId
            };

            // update existing card
            var query = $"UPDATE {_Table} SET name=@Name, damage=@Damage, elementType=@ElementType, isMonster=@IsMonster, packageId=@PackageId, ownerId=@OwnerId WHERE id=@Id";
            int rowsAffected = connection.Execute(query, cardToUpdate);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("Update failed: No card found with the given ID.");
            }
        }

        /// <summary>
        /// Fetches all cards that belong to the user and returns them as a list
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<Card> GetCardsByUserId(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = "SELECT * FROM cards WHERE ownerId = @UserId";
            var cards = connection.Query<Card>(query, new { UserId = userId }).ToList();

            if (cards == null)
            {
                throw new InvalidOperationException("No cards found for the user.");
            }

            return cards;
        }
    }
}
