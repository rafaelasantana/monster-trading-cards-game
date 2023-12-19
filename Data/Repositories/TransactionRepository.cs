using Dapper;
using mtcg.Data.Models;

namespace mtcg.Data.Repositories
{
    public class TransactionRepository
    {
        private readonly UserRepository _userRepository;
        private readonly PackageRepository _packageRepository;
        private readonly DbConnectionManager _dbConnectionManager;
        private readonly string _Table = "transactions";
        private readonly string _Fields = "id, userId, packageId, price";
        public TransactionRepository(DbConnectionManager dbConnectionManager, UserRepository userRepository, PackageRepository packageRepository)
        {
            _dbConnectionManager = dbConnectionManager;
            _userRepository = userRepository;
            _packageRepository = packageRepository;
        }

        /// <summary>
        /// Saves a transaction with user id, package id and price to the database
        /// </summary>
        /// <param name="transaction"></param>
        public void Save(Transaction transaction)
        {
            // open connection
            using var connection = _dbConnectionManager.GetConnection();
            connection.Open();
            // Save the transaction to the database
            var query = $"INSERT INTO {_Table} (userId, packageId, price) VALUES (@UserId, @PackageId, @Price)";
            connection.Execute(query, transaction);
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
            connection.Open();

            try
            {
                // Check for available packages
                Package package = _packageRepository.GetNextAvailablePackage();
                if (package == null)
                {
                    errorMessage = "No packages available for purchase.";
                    return false;
                }

                // Check if user has enough coins
                if (user.Coins < package.Price)
                {
                    errorMessage = "Insufficient coins to purchase package.";
                    return false;
                }

                // Deduct coins and update user's coins
                user.Coins -= package.Price;
                _userRepository.Update(user);

                // Assign cards from the package to the user
                _packageRepository.AssignPackageToUser(package, user);

                // Create and save transaction
                Transaction transactionRecord = new(user.Id, package.Id, package.Price);
                this.Save(transactionRecord);

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