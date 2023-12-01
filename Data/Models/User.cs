namespace mtcg
{
    public class User
    {
        public int Id {get; set;}
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Card> Stack { get; set; }

        // Parameterless default constructor
        public User()
        {
        }
        public User(string username, string password)
        {
            // initialize Id as 0, will be updated once user is saved to the database
            Id = 0;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }
    }
}