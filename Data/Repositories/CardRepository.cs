using System;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class CardRepository
    {
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _Table = "cards";
        private readonly string _Fields = "id, name, damage, elementType, cardType, packageId, ownerId";

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
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var cardType = card is MonsterCard ? "Monster" : "Spell";

            var cardToSave = new {
                card.Id,
                card.Name,
                card.Damage,
                ElementType = card.ElementType.ToString(),
                cardType,
                card.PackageId,
                card.OwnerId
            };

            int count = connection.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {_Table} WHERE id = @Id", new { card.Id });
            if (count == 0)
            {
                var query = $"INSERT INTO {_Table} ({_Fields}) VALUES (@Id, @Name, @Damage, @ElementType::ElementType, @cardType::CardType, @PackageId, @OwnerId)";
                connection.Execute(query, cardToSave);
            }
            else
            {
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

            var cardType = card is MonsterCard ? "Monster" : "Spell";

            var cardToUpdate = new {
                card.Id,
                card.Name,
                card.Damage,
                ElementType = card.ElementType.ToString(),
                cardType,
                card.PackageId,
                card.OwnerId
            };

            var query = $"UPDATE {_Table} SET name=@Name, damage=@Damage, elementType=@ElementType::ElementType, cardType=@CardType::CardType, packageId=@PackageId, ownerId=@OwnerId WHERE id=@Id";
            int rowsAffected = connection.Execute(query, cardToUpdate);

            if (rowsAffected == 0)
                throw new InvalidOperationException("Update failed: No card found with the given ID.");
        }

        /// <summary>
        /// Lists all cards belonging to this user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<Card> GetCardsByUserId(int userId)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var query = "SELECT * FROM cards WHERE ownerId = @UserId";
            return connection.Query<Card>(query, new { UserId = userId }).ToList();
        }
    }
}
