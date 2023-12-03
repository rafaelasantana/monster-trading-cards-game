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
            _Fields = "id, name, damage, elementType, packageId";
            // InitializeTableAndFields();
        }

        public new void Save(Card card)
        {   // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // check if card exists on the database
            int count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM cards WHERE id = @Id", new { card.Id });
            if (count == 0)
            {
                // insert new record
                var query = $"INSERT INTO {_Table} ({_Fields}) VALUES (@Id, @Name, @Damage, @ElementType, @PackageId)";
                connection.Execute(query, card);
            }
            else
            {
                // update existing card
                Update(card, card.Id);
            }
        }

    }
}