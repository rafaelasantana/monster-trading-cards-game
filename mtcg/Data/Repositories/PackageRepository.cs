using System.Data;
using Dapper;
using MTCG.Data.Models;

namespace MTCG.Data.Repositories
{
    public class PackageRepository(IDbConnectionManager dbConnectionManager)
    {
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "packages";
        private readonly CardRepository _cardRepository = new(dbConnectionManager);

        /// <summary>
        /// Saves a new package with all new cards, updates an existing package, or throws an exception
        /// </summary>
        /// <param name="package"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Save(Package package)
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
                Update(package);
            }
        }

        /// <summary>
        /// Updates an existing package or throws an exception
        /// </summary>
        /// <param name="package"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Update(Package package)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Prepare the query to update the package
            var query = $"UPDATE packages SET price = @Price, ownerId = @OwnerId WHERE Id = @Id";

            // Execute the update query
            var rowsAffected = connection.Execute(query, new { package.Price, package.OwnerId, package.Id });

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("Update failed: No package found with the given ID.");
            }

            // Update the cards if necessary
            var cards = package.GetCards();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    card.PackageId = package.Id;
                    _cardRepository.Update(card);
                }
            }
        }


        /// <summary>
        /// Creates a new package record on the database and attaches the cards to this package
        /// </summary>
        /// <param name="package"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void SaveNew(Package package)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // insert new package and return the generated Id
            int generatedId = connection.QueryFirstOrDefault<int>($"INSERT INTO {_table} (price, ownerId) VALUES (@Price, @OwnerId) RETURNING Id", package);

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
            var cards = package.GetCards();
            // attach cards to this package
            if (cards != null)
            {
                foreach(Card card in cards)
                {
                    card.AttachToPackage(package);
                    _cardRepository.Save(card);
                }
            }
            else return;

        }

        /// <summary>
        /// Returns true if any card on this package already exists on the database
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        private bool HasExistingCard(Package package)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // get this package's cards
            var cards = package.GetCards();
            if (cards != null)
            {
                // check if any cards already exist on the database
                foreach(Card card in cards)
                {
                    // check if there is a card with this Id on the database
                    int count = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM cards WHERE id = @Id", new { card.Id });
                    if (count > 0) return true;
                }
                return false;
            }
            else return false;
        }

        /// <summary>
        /// Returns an available package (not owned by any user)
        /// </summary>
        /// <returns></returns>
        public Package? GetNextAvailablePackage()
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Select a package with no owner yet
            var query = "SELECT * FROM packages WHERE OwnerId IS NULL LIMIT 1;";
            Package? package = connection.QueryFirstOrDefault<Package>(query);

            return package;
        }

        /// <summary>
        /// Assigns a package to the user, updating the package and cards records, or throws an exception
        /// </summary>
        /// <param name="package"></param>
        /// <param name="user"></param>
        public void AssignPackageToUser(Package package, User user)
        {
            // open connection
            var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            try
            {
                // Update the OwnerId of the package
                var packageUpdateQuery = "UPDATE packages SET OwnerId = @OwnerId WHERE Id = @PackageId";
                connection.Execute(packageUpdateQuery, new { OwnerId = user.Id, PackageId = package.Id });
                // Update the owner of all cards in the package
                var cardUpdateQuery = "UPDATE cards SET ownerId = @OwnerId WHERE PackageId = @PackageId";
                connection.Execute(cardUpdateQuery, new { OwnerId = user.Id, PackageId = package.Id });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("There was an error assigning the package to the user: " + ex.Message);
            }
        }
    }
}