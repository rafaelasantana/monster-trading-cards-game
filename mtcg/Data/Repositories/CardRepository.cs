using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

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
            string countQuery = $"SELECT COUNT(*) FROM {_table} WHERE id = @Id";
            using var countCommand = new NpgsqlCommand(countQuery, connection as NpgsqlConnection);
            countCommand.Parameters.AddWithValue("@Id", card.Id!);
            int count = Convert.ToInt32(countCommand.ExecuteScalar());

            if (count == 0)
            {
                // Save new card
                string insertQuery = $"INSERT INTO {_table} ({_fields}) VALUES (@Id, @Name, @Damage, @ElementType::ElementType, @CardType::CardType, @PackageId, @OwnerId)";
                using var insertCommand = new NpgsqlCommand(insertQuery, connection as NpgsqlConnection);
                // Adding parameters to the command object
                insertCommand.Parameters.AddWithValue("@Id", card.Id!);
                insertCommand.Parameters.AddWithValue("@Name", card.Name!);
                insertCommand.Parameters.AddWithValue("@Damage", card.Damage!);
                insertCommand.Parameters.AddWithValue("@ElementType", card.ElementType!);
                insertCommand.Parameters.AddWithValue("@CardType", card.CardType!);
                insertCommand.Parameters.AddWithValue("@PackageId", card.PackageId!);
                if (card.OwnerId.HasValue)
                {
                    insertCommand.Parameters.AddWithValue("@OwnerId", card.OwnerId.Value);
                }
                else
                {
                    insertCommand.Parameters.AddWithValue("@OwnerId", DBNull.Value);
                }

                insertCommand.ExecuteNonQuery();
            }
            else
            {
                // Update card
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

            var query = $"UPDATE {_table} SET name=@Name, damage=@Damage, elementType=@ElementType, cardType=@CardType, packageId=@PackageId, ownerId=@OwnerId WHERE id=@Id";

            using var updateCommand = new NpgsqlCommand(query, connection as NpgsqlConnection);

            // Adding parameters to the command object
            updateCommand.Parameters.AddWithValue("@Name", card.Name!);
            updateCommand.Parameters.AddWithValue("@Damage", card.Damage!);
            updateCommand.Parameters.AddWithValue("@ElementType", card.ElementType!);
            updateCommand.Parameters.AddWithValue("@CardType", card.CardType!);   ;
            updateCommand.Parameters.AddWithValue("@PackageId", card.PackageId!);

            // Handling the nullable ownerId
            if (card.OwnerId.HasValue)
            {
                updateCommand.Parameters.AddWithValue("@OwnerId", card.OwnerId.Value);
            }
            else
            {
                updateCommand.Parameters.AddWithValue("@OwnerId", DBNull.Value);
            }

            updateCommand.Parameters.AddWithValue("@Id", card.Id!);

            int rowsAffected = updateCommand.ExecuteNonQuery();

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
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);

            // Handling the nullable userId
            if (userId.HasValue)
            {
                command.Parameters.AddWithValue("@UserId", userId.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@UserId", DBNull.Value);
            }

            List<Card> cards = [];
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var card = DataMapperService.MapToObject<Card>(reader);
                cards.Add(card);
            }

            return cards;
        }

    }
}
