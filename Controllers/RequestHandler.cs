using System.Net;
using System.Text;
using mtcg.Data.Models;
using mtcg.Data.Repositories;
using mtcg.Controllers;
using Newtonsoft.Json;

namespace mtcg.Controllers
{
    public class RequestHandler
    {
        private readonly HttpListenerContext _context;
        private readonly UserRepository _userRepository;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly PackageRepository _packageRepository;
        private readonly TransactionRepository _transactionRepository;
        private readonly CardRepository _cardRepository;
        private readonly DeckRepository _deckRepository;
        private readonly UserStatsRepository _userStatsRepository;
        private readonly TradingRepository _tradingRepository;
        private readonly SessionManager _sessionManager;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            _context = context;
            _userRepository = new UserRepository(dbConnectionManager);
            _userProfileRepository = new UserProfileRepository(dbConnectionManager);
            _packageRepository = new PackageRepository(dbConnectionManager);
            _transactionRepository = new TransactionRepository(dbConnectionManager, _userRepository, _packageRepository);
            _cardRepository = new CardRepository(dbConnectionManager);
            _deckRepository = new DeckRepository(dbConnectionManager);
            _userStatsRepository = new UserStatsRepository(dbConnectionManager);
            _tradingRepository = new TradingRepository(dbConnectionManager);
            _sessionManager = new SessionManager();
        }

        /// <summary>
        /// Handles incoming requests
        /// </summary>
        public void HandleRequest()
        {
            try
            {
                using var reader = new StreamReader(_context.Request.InputStream);
                string json = reader.ReadToEnd();

                if (_context.Request.HttpMethod == "POST")
                {
                    HandlePOST(json);
                }
                else if (_context.Request.HttpMethod == "GET")
                {
                    HandleGET();
                }
                else if (_context.Request.HttpMethod == "PUT")
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

            _context.Response.Close();
        }

        /// <summary>
        /// Handles POST requests
        /// </summary>
        /// <param name="json"></param>
        private void HandlePOST(string json)
        {
            switch (_context.Request.Url.AbsolutePath)
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
                case "/tradings":
                    HandleCreateTradingDeal(json);
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
        private void HandleGET()
        {
            string path = _context.Request.Url.AbsolutePath;
            if (path.StartsWith("/users/"))
            {
                HandleGetUserProfile();
            }
            else
            {
                switch (path)
                {
                    case "/cards":
                        HandleGetCards();
                        break;
                    case "/deck":
                        HandleGetDeck();
                        break;
                    case "/stats":
                        HandleGetUserStats();
                        break;
                    case "/scoreboard":
                        HandleGetScoreboard();
                        break;
                    case "/tradings":
                        HandleGetTradingDeals();
                        break;
                    default:
                        // Default response
                        SendResponse("Hello, this is the server!", HttpStatusCode.OK);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles PUT requests
        /// </summary>s
        /// <param name="json"></param>
        private void HandlePUT(string json)
        {
            string path = _context.Request.Url.AbsolutePath;
            if (path.StartsWith("/users/"))
            {
                HandlePutUserProfile(json);
            }
            else
            {
                switch (path)
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
        }

        /// <summary>
        /// Registers a new user, creates a user profile, and user stats, or sends an error response if the user already exists
        /// </summary>
        private void HandleUserRegistration(string json)
        {
            try
            {
                // create new user based on json data
                User? newUser = ParseUserFromJson(json);

                // check if username is already taken
                if (_userRepository.GetByUsername(newUser.Username) != null)
                {
                    // send error response
                    string errorResponse = "Username already exists!";
                    SendResponse(errorResponse, HttpStatusCode.BadRequest);
                }
                else
                {
                    // save new user
                    _userRepository.Save(newUser);

                    // create a user profile with for this user
                    UserProfile newUserProfile = new UserProfile(newUser.Id, null, null, null);
                    _userProfileRepository.CreateUserProfile(newUserProfile);

                    // create a user stats record
                    // UserStats newUserStats = new UserStats(newUser.Id);
                    _userStatsRepository.CreateStats(newUser.Id);
                    Console.WriteLine("Created new user stats");

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
                User? registeredUser = _userRepository.GetByUsername(loginUser.Username);

                // check if user exists and password matches
                if (registeredUser != null && BCrypt.Net.BCrypt.Verify(loginUser.Password, registeredUser.Password))
                {
                    // create a token for this session
                    _sessionManager.CreateSessionToken(registeredUser.Username);
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
            if (!_sessionManager.IsAdmin(token))
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
                _packageRepository.Save(package);
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

                if (_transactionRepository.PurchasePackage(user, out string errorMessage))
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
                var cards = _cardRepository.GetCardsByUserId(user.Id);

                if (cards.Count == 0)
                {
                    SendResponse("You don't have any cards yet.", HttpStatusCode.OK);
                    return;
                }
                SendFormattedJSONResponse(cards, HttpStatusCode.OK);
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
                var deck = _deckRepository.GetDeckByUserId(user.Id);
                if (deck.Count == 0)
                {
                    SendResponse("Your deck is empty.", HttpStatusCode.OK);
                    return;
                }

                // Check if format=plain is requested
                string format = _context.Request.QueryString["format"];
                if (format == "plain")
                {
                    string plainResponse = CreatePlainTextResponse(deck);
                    SendResponse(plainResponse, HttpStatusCode.OK);
                }
                else
                {
                    SendFormattedJSONResponse(deck, HttpStatusCode.OK);
                }

            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Configures the user's deck with the requested cards, or sends an error response
        /// </summary>
        /// <param name="json"></param>
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
                bool isDeckConfigured = _deckRepository.ConfigureDeck(user.Id, cardIds);
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
        /// Fetches the user profile data and returns it as a formatted JSON, or sends an error response
        /// </summary>
        private void HandleGetUserProfile()
        {
            try
            {
                // Validate the user tokens
                User user = ValidateTokenAndGetUser();

                // Extract username from the URL
                string urlUsername = _context.Request.Url.AbsolutePath.Split('/')[2]; // Assumes URL is /users/{username}

                // Check if the username matches the one from the token
                if (user.Username != urlUsername)
                {
                    throw new Exception("Access denied.");
                }

                // Fetch user profile data
                UserProfile userProfile = _userProfileRepository.GetUserProfile(user.Id);
                if (userProfile == null)
                {
                    SendResponse("User profile not found.", HttpStatusCode.NotFound);
                    return;
                }

                // Send the user profile data
                SendFormattedJSONResponse(userProfile, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Updates the user profile or sends an error message
        /// </summary>
        /// <param name="json"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        private void HandlePutUserProfile(string json)
        {
            try
            {
                // Validate the user tokens
                User user = ValidateTokenAndGetUser();

                // Extract username from the URL
                string urlUsername = _context.Request.Url.AbsolutePath.Split('/')[2]; // Assumes URL is /users/{username}

                // Check if the username matches the one from the token
                if (user.Username != urlUsername)
                {
                    throw new UnauthorizedAccessException("You are not authorized to update this profile.");
                }

                // Parse JSON payload to UserProfile object
                var updatedProfile = JsonConvert.DeserializeObject<UserProfile>(json);

                // Update user profile
                _userProfileRepository.UpdateUserProfile(user.Id, updatedProfile);

                // Send success response
                SendResponse("User profile updated successfully!", HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.BadRequest);
            }

        }

        /// <summary>
        /// Returns the stats for the user as a formatted JSON, or sends an error response
        /// </summary>
        private void HandleGetUserStats()
        {
            try
            {
                // Validate the user
                User user = ValidateTokenAndGetUser();

                // Fetch user stats from the repository
                UserStats stats = _userStatsRepository.GetStatsByUserId(user.Id);
                if (stats == null)
                {
                    SendResponse("Stats not found.", HttpStatusCode.NotFound);
                    return;
                }

                // Return user stats as formattted JSON
                SendFormattedJSONResponse(stats, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }

        }

        /// <summary>
        /// Returns the scoreboard as a formatted JSON, or sends an error response
        /// </summary>
        private void HandleGetScoreboard()
        {
            try
            {
                // Validate the token and get the user
                User user = ValidateTokenAndGetUser();

                // Query the database for the scoreboard
                var scoreboardData = _userStatsRepository.GetScoreboardData();

                // Send the JSON response
                SendFormattedJSONResponse(scoreboardData, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Returns the open tradings as a formatted JSON, or sends an error response
        /// </summary>
        private void HandleGetTradingDeals()
        {
            try
            {
                // Validate user token
                User user = ValidateTokenAndGetUser();

                // Get trading deals from the store
                var trades = _tradingRepository.GetAllOffers();

                // Check if there are open tradings
                if (trades.Count() == 0)
                {
                    SendResponse("There are no open tradings.", HttpStatusCode.OK);
                }
                // Return open trading deals as formatted JSON
                else SendFormattedJSONResponse(trades, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>
        /// Checks if the offer is valid and pushes it to the trading store, and sends an error response
        /// </summary>
        /// <param name="json"></param>
        private void HandleCreateTradingDeal(string json)
        {
            try
            {
                // Validate user token
                User user = ValidateTokenAndGetUser();

                // Deserialize JSON to dynamic object to access properties
                dynamic tradeOfferJson = JsonConvert.DeserializeObject<dynamic>(json);

                // Construct StoreCard object with expected properties
                var tradeOffer = new TradingOffer
                {
                    Id = tradeOfferJson.Id,
                    OwnerId = user.Id,
                    CardId = tradeOfferJson.CardToTrade, // Assuming this is the actual card ID to be traded
                    RequestedType = tradeOfferJson.Type,
                    MinDamage = tradeOfferJson.MinimumDamage
                };

                // Create trading offer
                _tradingRepository.CreateOffer(tradeOffer);

                SendResponse("Trading deal created successfully!", HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.BadRequest);
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
            string? username = _sessionManager.GetUserFromToken(token);
            if (username == null)
            {
                throw new InvalidOperationException("Invalid or expired token.");
            }

            User? user = _userRepository.GetByUsername(username);
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
            string? authHeader = _context.Request.Headers["Authorization"];

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
            _context.Response.StatusCode = (int)statusCode;
            _context.Response.ContentType = "text/plain";
            _context.Response.ContentLength64 = responseBytes.Length;
            _context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /// <summary>
        /// Sends an HTTP response with formatted JSON data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        private void SendFormattedJSONResponse(object data, HttpStatusCode statusCode)
        {
            string jsonResponse = JsonConvert.SerializeObject(data, Formatting.Indented);
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(jsonResponse);

            _context.Response.ContentType = "application/json";
            _context.Response.StatusCode = (int)statusCode;
            _context.Response.ContentLength64 = responseBytes.Length;
            _context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /// <summary>
        /// Returns the list of cards in plain text format
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        private string CreatePlainTextResponse(List<Card> cards)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in cards)
            {
                sb.AppendLine($"Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}");
            }
            return sb.ToString();
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