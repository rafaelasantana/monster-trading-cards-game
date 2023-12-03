using System.Net;
using mtcg.Data.Models;
using mtcg.Data.Repositories;
using Newtonsoft.Json;

namespace mtcg
{
    public class RequestHandler
    {
        private readonly HttpListenerContext context;
        private readonly UserRepository userRepository;
        private readonly PackageRepository packageRepository;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            this.context = context;
            userRepository = new UserRepository(dbConnectionManager);
            packageRepository = new PackageRepository(dbConnectionManager);
        }

        /// <summary>
        /// Handles incoming requests
        /// </summary>
        public void HandleRequest()
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream);
                string json = reader.ReadToEnd();

                if (context.Request.HttpMethod == "POST")
                {
                    switch (context.Request.Url.AbsolutePath)
                    {
                        case "/users":
                            HandleUserRegistration(json);
                            break;
                        case "/sessions":
                            HandleUserLogin(json);
                            break;
                        case "/packages":
                            HandlePackageCreation(json);
                            break;
                        default:
                            // Default response
                            SendResponse("Hello, this is the server!", HttpStatusCode.OK);
                            break;
                    }
                }
                else
                {
                    // Default response for non-POST requests
                    SendResponse("Hello, this is the server!", HttpStatusCode.OK);
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
        private void HandleUserRegistration(string json)
        {
            try
            {
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
        private void HandleUserLogin(string json) {
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

        private void HandlePackageCreation(string json)
        {
            // TODO check authentication token for admin?
            try
            {
                // create new package based on json data
                Package package = new(json);
                // save package to the database
                packageRepository.Save(package);

                string successResponse = "Package created successfully!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                string errorResponse = $"Error: {e.Message}";
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