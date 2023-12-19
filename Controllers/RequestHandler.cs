using System.Net;
using mtcg.Data.Models;
using mtcg.Data.Repositories;
using mtcg.Controllers;
using Newtonsoft.Json;

namespace mtcg.Controllers
{
    public class RequestHandler
    {
        private readonly HttpListenerContext Context;
        private readonly UserRepository UserRepository;
        private readonly PackageRepository PackageRepository;
        private readonly TransactionRepository TransactionRepository;
        private readonly CardRepository CardRepository;
        private readonly DeckRepository DeckRepository;
        private readonly SessionManager SessionManager;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            Context = context;
            UserRepository = new UserRepository(dbConnectionManager);
            PackageRepository = new PackageRepository(dbConnectionManager);
            TransactionRepository = new TransactionRepository(dbConnectionManager, UserRepository, PackageRepository);
            CardRepository = new CardRepository(dbConnectionManager);
            DeckRepository = new DeckRepository(dbConnectionManager);
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
                    HandlePOST(json);
                }
                else if (Context.Request.HttpMethod == "GET")
                {
                    HandleGET(json);
                }
                else if (Context.Request.HttpMethod == "PUT")
                {
                    HandlePUT(json);
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
        /// Handles POST requests
        /// </summary>
        /// <param name="json"></param>
        private void HandlePOST(string json)
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
                case "/transactions/packages":
                    HandlePackagePurchase();
                    break;
                default:
                    // Default response
                    SendResponse("Hello, this is the server!", HttpStatusCode.OK);
                    break;
            }
        }

        /// <summary>
        /// Handles GET requests
        /// </summary>
        /// <param name="json"></param>
        private void HandleGET(string json)
        {
            switch (Context.Request.Url.AbsolutePath)
            {
                case "/cards":
                    HandleGetCards();
                    break;
                case "/deck":
                    HandleGetDeck();
                    break;
                default:
                    // Default response
                    SendResponse("Hello, this is the server!", HttpStatusCode.OK);
                    break;
            }
        }

        /// <summary>
        /// Handles PUT requests
        /// </summary>
        /// <param name="json"></param>
        private void HandlePUT(string json)
        {
            switch (Context.Request.Url.AbsolutePath)
            {
                case "/deck":
                    HandleConfigureDeck(json);
                    break;
                default:
                    // Default response
                    SendResponse("Hello, this is the server!", HttpStatusCode.OK);
                    break;
            }
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

            try
            {
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
            catch (Exception e)
            {
                // send error response
                string errorResponse = $"Error: {e.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
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
                // send error response
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
                // send success response
                string successResponse = "Package created successfully!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                // send error response
                string errorResponse = $"Error: {e.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Executes the purchase of a package by an user, or sends an error response
        /// </summary>
        private void HandlePackagePurchase()
        {
            try
            {
                User user = ValidateTokenAndGetUser();

                if (TransactionRepository.PurchasePackage(user, out string errorMessage))
                {
                    SendResponse("Package purchased successfully!", HttpStatusCode.OK);
                }
                else
                {
                    SendResponse(errorMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets all cards for this user and returns them with the response
        /// </summary>
        private void HandleGetCards()
        {
            try
            {
                User user = ValidateTokenAndGetUser();
                var cards = CardRepository.GetCardsByUserId(user.Id);

                if (cards.Count == 0)
                {
                    SendResponse("You don't have any cards yet.", HttpStatusCode.OK);
                    return;
                }
                SendFormattedResponse(cards, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Gets all cards in this user's deck and returns them with the response
        /// </summary>
        private void HandleGetDeck()
        {
            try
            {
                User user = ValidateTokenAndGetUser();
                var deck = DeckRepository.GetDeckByUserId(user.Id);
                if (deck.Count == 0)
                {
                    SendResponse("Your deck is empty.", HttpStatusCode.OK);
                    return;
                }

                SendFormattedResponse(deck, HttpStatusCode.OK); // Sending the deck as a JSON response
            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        private void HandleConfigureDeck(string json)
        {
            try
            {
                // Validate the user
                User user = ValidateTokenAndGetUser();

                // Deserialize JSON payload
                var cardIds = JsonConvert.DeserializeObject<string[]>(json);
                if (cardIds == null || cardIds.Length != 4)
                {
                    SendResponse("Invalid card configuration.", HttpStatusCode.BadRequest);
                    return;
                }

                // Validate and configure the deck
                bool isDeckConfigured = DeckRepository.ConfigureDeck(user.Id, cardIds);
                if (isDeckConfigured)
                {
                    SendResponse("Deck successfully configured.", HttpStatusCode.OK);
                }
                else
                {
                    SendResponse("Failed to configure deck.", HttpStatusCode.BadRequest);
                }

            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Validates the token and returns the associated user, or throws an exception
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private User ValidateTokenAndGetUser()
        {
            string? token = ExtractAuthTokenFromHeader();
            string? username = SessionManager.GetUserFromToken(token);
            if (username == null)
            {
                throw new InvalidOperationException("Invalid or expired token.");
            }

            User? user = UserRepository.GetByUsername(username);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }
            return user;
        }

        /// <summary>
        /// Extracts the authorization token from the request header
        /// </summary>
        /// <returns></returns>
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
        /// Sends an HTTP response with formatted JSON data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        private void SendFormattedResponse(object data, HttpStatusCode statusCode)
        {
            string jsonResponse = JsonConvert.SerializeObject(data, Formatting.Indented);
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(jsonResponse);

            Context.Response.ContentType = "application/json";
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