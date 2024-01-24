using System.Data;
using MTCG.Data.Models;
using MTCG.Data.Services;
using Npgsql;

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
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // Prepare the query to update the package
            var query = $"UPDATE packages SET price = @Price, ownerId = @OwnerId WHERE Id = @Id";

            // Execute the update query
            using var updateCommand = new NpgsqlCommand(query, connection as NpgsqlConnection);
            updateCommand.Parameters.AddWithValue("@Price", package.Price!);
            updateCommand.Parameters.AddWithValue("@OwnerId", package.OwnerId ?? (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("@Id", package.Id!);

            int rowsAffected = updateCommand.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Update failed: No package found with ID {package.Id}.");
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
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var insertCommand = new NpgsqlCommand($"INSERT INTO {_table} (price, ownerId) VALUES (@Price, @OwnerId) RETURNING Id", connection as NpgsqlConnection);
            insertCommand.Parameters.AddWithValue("@Price", package.Price!);
            insertCommand.Parameters.AddWithValue("@OwnerId", package.OwnerId ?? (object)DBNull.Value);

            var result = insertCommand.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int generatedId) && generatedId > 0)
            {
                package.Id = generatedId;
                AttachCards(package);
            }
            else
            {
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
            using var connection = _dbConnectionManager.GetConnection();
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
                    using var countCommand = new NpgsqlCommand("SELECT COUNT(*) FROM cards WHERE id = @Id", connection as NpgsqlConnection);
                    countCommand.Parameters.AddWithValue("@Id", card.Id!);

                    var countResult = countCommand.ExecuteScalar();
                    int count = countResult != null ? Convert.ToInt32(countResult) : 0;
                    if (count > 0) return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Returns an available package (not owned by any user)
        /// </summary>
        /// <returns></returns>
        public Package? GetNextAvailablePackage()
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = "SELECT * FROM packages WHERE OwnerId IS NULL LIMIT 1;";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);

            Package? package = null;
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                package = DataMapperService.MapToObject<Package>(reader);
            }

            return package;
        }


        /// <summary>
        /// Assigns a package to the user, updating the package and cards records, or throws an exception
        /// </summary>
        /// <param name="package"></param>
        /// <param name="user"></param>
        public void AssignPackageToUser(Package package, User user)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            try
            {
                // Update the OwnerId of the package
                var packageUpdateQuery = "UPDATE packages SET OwnerId = @OwnerId WHERE Id = @PackageId";
                using var packageUpdateCommand = new NpgsqlCommand(packageUpdateQuery, connection as NpgsqlConnection);
                packageUpdateCommand.Parameters.AddWithValue("@OwnerId", user.Id!);
                packageUpdateCommand.Parameters.AddWithValue("@PackageId", package.Id!);
                packageUpdateCommand.ExecuteNonQuery();

                // Update the owner of all cards in the package
                var cardUpdateQuery = "UPDATE cards SET ownerId = @OwnerId WHERE PackageId = @PackageId";
                using var cardUpdateCommand = new NpgsqlCommand(cardUpdateQuery, connection as NpgsqlConnection);
                cardUpdateCommand.Parameters.AddWithValue("@OwnerId", user.Id!);
                cardUpdateCommand.Parameters.AddWithValue("@PackageId", package.Id!);
                cardUpdateCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("There was an error assigning the package to the user: " + ex.Message);
            }
        }

    }
}