using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class CardRepository : Repository<Card>
    {
        public CardRepository(DbConnectionManager dbConnectionManager) : base(dbConnectionManager)
        {
            _Table = "cards";
            _Fields = "id, name, damage, element_type, package_id";
        }

        public new void Save(Card card)
        {   // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            var existingCard = connection.QueryFirstOrDefault<Card>($"SELECT * FROM {_Table} WHERE id = @Id", new { card.Id });

            // check if card exists on the database
            if (existingCard == null)
            {
                // insert new record
                var query = $"INSERT INTO {_Table} ({_Fields}) VALUES (@Id, @Name, @Damage, @ElementType, @PackageId)";
                connection.Execute(query, card);
            }
            else
            {
                // todo: throw error message, card already exists
                Console.WriteLine("Error saving card: card already exists");
            }
        }

    }
}