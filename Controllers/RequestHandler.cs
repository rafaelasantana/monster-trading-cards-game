using System.Net;
using mtcg.Controllers;
using mtcg.Data.Models;
using mtcg.Data.Repositories;
using Newtonsoft.Json;

namespace mtcg
{
    public class RequestHandler
    {
        private readonly HttpListenerContext Context;
        private readonly UserRepository UserRepository;
        private readonly PackageRepository PackageRepository;
        private readonly SessionManager SessionManager;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            Context = context;
            UserRepository = new UserRepository(dbConnectionManager);
            PackageRepository = new PackageRepository(dbConnectionManager);
            SessionManager = new SessionManager();
        }

        /// <summary>
        /// Handles incoming requests
        /// </summary>
        public void HandleRequest()
        {
            try
            {
                using var reader = new StreamReader(Context.Request.InputStream);
                string json = reader.ReadToEnd();

                if (Context.Request.HttpMethod == "POST")
                {
                    switch (Context.Request.Url.AbsolutePath)
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

            Context.Response.Close();
        }


        /// <summary>
        /// Registers a new user with a hashed password, or sends an error response if the user already exists
        /// </summary>
        private void HandleUserRegistration(string json)
        {
            try
            {
                // create new user based on json data
                User? newUser = ParseUserFromJson(json);

                // check if username is already taken
                if (UserRepository.GetByUsername(newUser.Username) != null)
                {
                    // send error response
                    string errorResponse = "Username already exists!";
                    SendResponse(errorResponse, HttpStatusCode.BadRequest);
                }
                else
                {
                    // save new user
                    UserRepository.Save(newUser);

                    // send success response
                    string successResponse = "User registered successfully!";
                    SendResponse(successResponse, HttpStatusCode.OK);
                }
            }
            catch (Exception e)
            {
                // send error response
                string errorResponse = $"Error: {e.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// logs a registered user in, or sends an error response
        /// </summary>
        private void HandleUserLogin(string json) {
            User? loginUser = ParseUserFromJson(json);

            // get user's data from the database
            User? registeredUser = UserRepository.GetByUsername(loginUser.Username);

            // check if user exists and password matches
            if (registeredUser != null && BCrypt.Net.BCrypt.Verify(loginUser.Password, registeredUser.Password))
            {
                // create a token for this session
                SessionManager.CreateSessionToken(registeredUser.Username);
                // send success response
                string successResponse = "Login successful!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            else
            {
                // send error response
                string errorResponse = "Invalid username or password.";
                SendResponse(errorResponse, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Checks for admin access, creates a new package with unique cards or sends an error response
        /// </summary>
        /// <param name="json"></param>
        private void HandlePackageCreation(string json)
        {
            // extract token from header
            string? token = ExtractAuthTokenFromHeader();

            // check if the token belongs to the admin
            if (!SessionManager.IsAdmin(token))
            {
                string errorResponse = "Unauthorized: Admin access required.";
                SendResponse(errorResponse, HttpStatusCode.Unauthorized);
                return;
            }

            try
            {
                // create new package based on json data
                Package package = new(json);
                // save package to the database
                PackageRepository.Save(package);

                string successResponse = "Package created successfully!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                string errorResponse = $"Error: {e.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        private string? ExtractAuthTokenFromHeader()
        {
            string? authToken = null;

            // extract the Authorization header
            string? authHeader = Context.Request.Headers["Authorization"];

            // check if Authorization header is present in the request
            if(!string.IsNullOrEmpty(authHeader))
            {
                // split header to get the token part
                string[] headerParts = authHeader.Split(' ');

                // check if the header has the expected format
                if (headerParts.Length == 2 && headerParts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    // extract auth token
                    authToken = headerParts[1];
                }
            }
            return authToken;
        }

        /// <summary>
        /// Sends an HTTP response
        /// </summary>
        /// <param name="responseString"></param>
        /// <param name="statusCode"></param>
        private void SendResponse(string responseString, HttpStatusCode statusCode)
        {
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            Context.Response.StatusCode = (int)statusCode;
            Context.Response.ContentLength64 = responseBytes.Length;
            Context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /// <summary>
        /// Returns an User parsed from JSON format or null
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static User? ParseUserFromJson(string json)
        {
            try
            {
                User? newUser = JsonConvert.DeserializeObject<User>(json);
                return newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing user from JSON: {ex.Message}");
                throw;
            }
        }
    }
}