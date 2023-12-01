using System.Net;
using Newtonsoft.Json;

namespace mtcg
{
    public class RequestHandler
    {
        private readonly HttpListenerContext context;
        private readonly UserRepository userRepository;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            this.context = context;
            userRepository = new UserRepository(dbConnectionManager);
        }

        /// <summary>
        /// Handles incoming requests
        /// </summary>
        public void HandleRequest()
        {
            try
            {
                if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/users")
                {
                    HandleUserRegistration();
                }
                else
                {
                    // Default response
                    string responseString = "Hello, this is the server!";
                    SendResponse(responseString, HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                string errorResponse = $"Error: {ex.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }

            context.Response.Close();
        }

        /// <summary>
        /// Registers a new user with a hashed password, or sends an error response if the user already exists
        /// </summary>
        private void HandleUserRegistration()
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream);
                string json = reader.ReadToEnd();
                User newUser = ParseUserFromJson(json);

                // check if username is taken
                if (userRepository.UserExists(newUser.Username))
                {
                    string errorResponse = "Username already exists!";
                    // send error response
                    SendResponse(errorResponse, HttpStatusCode.BadRequest);
                }
                else
                {
                    // hash password
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.Password);
                    // set hashed password
                    newUser.Password = hashedPassword;
                    // save new user
                    userRepository.Save(newUser);

                    PrintAllUsers();

                    string successResponse = "User registered successfully!";
                    // send success response
                    SendResponse(successResponse, HttpStatusCode.OK);
                }
            }
            catch (Exception e)
            {
                string errorResponse = $"Error: {e.Message}";
                // send error response
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Sends an HTTP response
        /// </summary>
        /// <param name="responseString"></param>
        /// <param name="statusCode"></param>
        private void SendResponse(string responseString, HttpStatusCode statusCode)
        {
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /// <summary>
        /// Parses an User from JSON format
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static User ParseUserFromJson(string json)
        {
            try
            {
                User newUser = JsonConvert.DeserializeObject<User>(json);
                return newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing user from JSON: {ex.Message}");
                throw;
            }
        }

        // prints all users on the database
        private void PrintAllUsers()
        {
            Console.WriteLine("Registered Users:");

            var users = userRepository.GetAll();

            foreach (var user in users)
            {
                Console.WriteLine($"Username: {user.Username}, Password: {user.Password}");
            }
        }
    }
}