namespace MTCG.Data.Models
{
    public class Transaction
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public int? PackageId { get; set; }
        public int? Price { get; set; }

        public Transaction()
        {}
        public Transaction(int? userId, int? packageId, int? price)
        {
            UserId = userId;
            PackageId = packageId;
            Price = price;
        }
    }
}