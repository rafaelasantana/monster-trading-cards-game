using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class PackageRepository : Repository<Package>
    {
        private readonly CardRepository cardRepository;
        public PackageRepository(DbConnectionManager dbConnectionManager) : base(dbConnectionManager)
        {
            _Table = "packages";
            _Fields = "price, owner";
            cardRepository = new CardRepository(dbConnectionManager);
        }

        /// <summary>
        /// Saves a new package with all new cards, updates an existing package, or throws an exception
        /// </summary>
        /// <param name="package"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public new void Save(Package package)
        {
            // check if it's a new package (not yet saved to the database)
            if (package.Id == 0)
            {
                // check if all cards in the package are new and if so, create new package record on the database
                if (!HasExistingCard(package)) SaveNew(package);
                // throw exception - all cards must be unique
                else throw new InvalidOperationException("Cannot save a new package with existing cards.");
            }
            else
            {
                // update package
                Update(package, package.Id.ToString());
            }
        }

        /// <summary>
        /// Creates a new package record on the database and attaches the cards to this package
        /// </summary>
        /// <param name="package"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void SaveNew(Package package)
        {
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // insert new package and return the generated Id
            int generatedId = connection.QueryFirstOrDefault<int>($"INSERT INTO {_Table} ({_Fields}) VALUES (@Price, @OwnerId) RETURNING Id", package);

            // check if insert is successful
            if (generatedId > 0)
            {
                // save generated Id to the object
                package.Id = generatedId;

                // attach cards to this package
                AttachCards(package);
            }
            else {
                // Throw an exception if the package couldn't be inserted
                throw new InvalidOperationException("Failed to insert the new package.");
            }
        }

        /// <summary>
        /// Attaches each card to this package's Id and saves it to the database
        /// </summary>
        /// <param name="package"></param>
        private void AttachCards(Package package)
        {
            // attach cards to this package
            List<Card> cards = package.GetCards();
            foreach(Card card in cards)
            {
                card.AttachToPackage(package);
                cardRepository.Save(card);
            }
        }

        /// <summary>
        /// Returns true if any card on this package already exists on the database
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        private bool HasExistingCard(Package package)
        {
            // open database connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();

            // get this package's cards
            List<Card> cards = package.GetCards();

            // check if any cards already exist on the database
            foreach(Card card in cards)
            {
                // check if there is a card with this Id on the database
                int count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM cards WHERE id = @Id", new { card.Id });
                if (count > 0) return true;
            }
            return false;
        }
    }
}