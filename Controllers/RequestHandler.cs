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
                else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/sessions")
                {
                    HandleUserLogin();
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

                // check if username is already taken
                if (userRepository.GetByUsername(newUser.Username) != null)
                {
                    string errorResponse = "Username already exists!";
                    // send error response
                    SendResponse(errorResponse, HttpStatusCode.BadRequest);
                }
                else
                {
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
        /// logs a registered user in, or sends an error response
        /// </summary>
        private void HandleUserLogin() {
            using var reader = new StreamReader(context.Request.InputStream);
            string json = reader.ReadToEnd();
            User loginUser = ParseUserFromJson(json);

            // get user's data from the database
            User registeredUser = userRepository.GetByUsername(loginUser.Username);

            // check if user exists and password matches
            if (registeredUser != null && BCrypt.Net.BCrypt.Verify(loginUser.Password, registeredUser.Password))
            {
                string successResponse = "Login successful!";
                // send success response
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            else
            {
                string errorResponse = "Invalid username or password.";
                // send error response
                SendResponse(errorResponse, HttpStatusCode.Unauthorized);
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