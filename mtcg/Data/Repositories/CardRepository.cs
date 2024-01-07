using System.Data;
using Dapper;
using MTCG.Data.Models;

namespace MTCG.Data.Repositories
{
    public class CardRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "cards";
        private readonly string _fields = "id, name, damage, elementType, cardType, packageId, ownerId";

        /// <summary>
        /// Saves a new card to the database or updates an existing card
        /// </summary>
        /// <param name="card"></param>
        public void Save(Card card)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // check if card exists on the database
            int count = connection.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {_table} WHERE id = @Id", new { card.Id });
            if (count == 0)
            {
                // save new card
                var query = $"INSERT INTO {_table} ({_fields}) VALUES (@Id, @Name, @Damage, @ElementType::ElementType, @CardType::CardType, @PackageId, @OwnerId)";
                connection.Execute(query, card);
            }
            else
            {
                // update card
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
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"UPDATE {_table} SET name=@Name, damage=@Damage, elementType=@ElementType::ElementType, cardType=@CardType::CardType, packageId=@PackageId, ownerId=@OwnerId WHERE id=@Id";
            int rowsAffected = connection.Execute(query, card);

            if (rowsAffected == 0)
                throw new InvalidOperationException("Update failed: No card found with the given ID.");
        }

        /// <summary>
        /// Lists all cards belonging to this user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<Card> GetCardsByUserId(int? userId)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = "SELECT * FROM cards WHERE ownerId = @UserId";
            return connection.Query<Card>(query, new { UserId = userId }).ToList();
        }
    }
}
