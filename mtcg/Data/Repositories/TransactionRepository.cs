using System.Data;
using MTCG.Data.Models;
using Npgsql;

namespace MTCG.Data.Repositories
{
    public class TransactionRepository(IDbConnectionManager dbConnectionManager, UserRepository userRepository, PackageRepository packageRepository)
    {
        private readonly UserRepository _userRepository = userRepository;
        private readonly PackageRepository _packageRepository = packageRepository;
        private readonly IDbConnectionManager _dbConnectionManager = dbConnectionManager;
        private readonly string _table = "transactions";

        /// <summary>
        /// Saves a transaction with user id, package id and price to the database
        /// </summary>
        /// <param name="transaction"></param>
        public void Save(Transaction transaction)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var query = $"INSERT INTO {_table} (userId, packageId, price) VALUES (@UserId, @PackageId, @Price)";
            using var command = new NpgsqlCommand(query, connection as NpgsqlConnection);
            command.Parameters.AddWithValue("@UserId", transaction.UserId);
            command.Parameters.AddWithValue("@PackageId", transaction.PackageId);
            command.Parameters.AddWithValue("@Price", transaction.Price);
            command.ExecuteNonQuery();
        }


        /// <summary>
        /// Executes the purchase of a package by the user, or returns false with an error message
        /// </summary>
        /// <param name="user"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public bool PurchasePackage(User user, out string errorMessage)
        {
            using var connection = _dbConnectionManager.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            try
            {
                // Check for available packages
                Package? package = _packageRepository.GetNextAvailablePackage();
                if (package == null)
                {
                    errorMessage = "No packages available for purchase.";
                    return false;
                }

                if (user.Coins < package.Price)
                {
                    errorMessage = "Insufficient coins to purchase package.";
                    return false;
                }

                user.Coins -= package.Price;
                _userRepository.Update(user);

                _packageRepository.AssignPackageToUser(package, user);

                Transaction transactionRecord = new(user.Id, package.Id, package.Price);
                Save(transactionRecord);

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "An error occurred while processing the transaction: " + ex.Message;
                return false;
            }
        }
    }
}