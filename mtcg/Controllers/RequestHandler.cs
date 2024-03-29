using System.Net;
using System.Text;
using MTCG.Data.Models;
using MTCG.Data.Repositories;
using MTCG.Data.Services;
using Newtonsoft.Json;


namespace MTCG.Controllers
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
        private readonly BattleRepository _battleRepository;
        private readonly BattleLogsRepository _battleLogsRepository;
        private readonly BattleService _battleService;
        private readonly UserService _userService;
        private readonly SessionManager _sessionManager;

        public RequestHandler(HttpListenerContext context, DbConnectionManager dbConnectionManager)
        {
            _context = context;
            _userProfileRepository = new UserProfileRepository(dbConnectionManager);
            _packageRepository = new PackageRepository(dbConnectionManager);
            _cardRepository = new CardRepository(dbConnectionManager);
            _deckRepository = new DeckRepository(dbConnectionManager);
            _userStatsRepository = new UserStatsRepository(dbConnectionManager);
            _tradingRepository = new TradingRepository(dbConnectionManager);
            _userRepository = new UserRepository(dbConnectionManager);
            _transactionRepository = new TransactionRepository(dbConnectionManager, _userRepository, _packageRepository);
            _battleRepository = new BattleRepository(dbConnectionManager);
            _battleLogsRepository = new BattleLogsRepository(dbConnectionManager);
            _battleService = new BattleService(_deckRepository, _battleRepository, _userStatsRepository, _battleLogsRepository);
            _userService = new UserService(_userRepository, _userStatsRepository, _userProfileRepository);
            _sessionManager = new SessionManager();
        }

        /// <summary>
        /// Handles incoming requests
        /// </summary>
        public void HandleRequest()
        {
            try
            {
                if (_context == null) return;
                using var reader = new StreamReader(_context.Request.InputStream);
                string json = reader.ReadToEnd();
                string method = _context.Request.HttpMethod;

                if (method == "POST")
                {
                    HandlePOST(json);
                }
                else if (method == "GET")
                {
                    HandleGET();
                }
                else if (method == "PUT")
                {
                    HandlePUT(json);
                }
                else if (method == "DELETE")
                {
                    HandleDELETE();
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
            // Check for nulls in the object chain
            if (_context == null || _context.Request == null || _context.Request.Url == null)
            {
                throw new InvalidOperationException("Request context is not properly initialized.");
            }
            string path = _context.Request.Url.AbsolutePath;

            if (path.StartsWith("/tradings/"))
            {
                HandleCreateTradingRequest(json, path);
            }
            switch (path)
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
                case "/battles":
                    HandleBattles();
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
            // Check for nulls in the object chain
            if (_context == null || _context.Request == null || _context.Request.Url == null)
            {
                throw new InvalidOperationException("Request context is not properly initialized.");
            }

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
            // Check for nulls in the object chain
            if (_context == null || _context.Request == null || _context.Request.Url == null)
            {
                throw new InvalidOperationException("Request context is not properly initialized.");
            }

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
        /// Handles DELETE requests
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void HandleDELETE()
        {
            // Check for nulls in the object chain
            if (_context == null || _context.Request == null || _context.Request.Url == null)
            {
                throw new InvalidOperationException("Request context is not properly initialized.");
            }

            string path = _context.Request.Url.AbsolutePath;
            if (path.StartsWith("/tradings/"))
            {
                HandleDeleteTrading(path);
            }

        }
        /// <summary>
        /// Registers a new user, creates a user profile, and user stats, or sends an error response if the user already exists
        /// </summary>
        private void HandleUserRegistration(string json)
        {
            try
            {
                User? newUser = ParseUserFromJson(json);

                if (newUser == null || string.IsNullOrWhiteSpace(newUser.Username))
                {
                    SendResponse("Invalid user data", HttpStatusCode.BadRequest);
                    return;
                }

                _userService.RegisterUser(newUser);

                // Registration successful
                string successResponse = "User registered successfully!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
            catch (ArgumentException ex)
            {
                // Handle invalid argument exceptions
                SendResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                // Handle user already exists
                SendResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                string errorResponse = $"Error: {ex.Message}";
                SendResponse(errorResponse, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// logs a registered user in, or sends an error response
        /// </summary>
        private void HandleUserLogin(string json)
        {
            try
            {
                User? loginUser = ParseUserFromJson(json);

                if (loginUser == null || string.IsNullOrEmpty(loginUser.Username) || string.IsNullOrEmpty(loginUser.Password))
                {
                    SendResponse("Invalid login data", HttpStatusCode.BadRequest);
                    return;
                }

                User registeredUser = _userService.LoginUser(loginUser.Username, loginUser.Password);

                // Ensure that the username is not null
                if (string.IsNullOrEmpty(registeredUser.Username))
                {
                    throw new InvalidOperationException("Logged-in user must have a username.");
                }

                // Create a token for this session
                _sessionManager.CreateSessionToken(registeredUser.Username);

                // Send success response
                SendResponse("Login successful!", HttpStatusCode.OK);
            }
            catch (InvalidOperationException ex)
            {
                // Handle invalid credentials
                SendResponse(ex.Message, HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                SendResponse($"Error: {ex.Message}", HttpStatusCode.InternalServerError);
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
                List<Card> deck = _deckRepository.GetDeckByUserId(user.Id);
                if (deck.Count == 0)
                {
                    SendResponse("Your deck is empty.", HttpStatusCode.OK);
                    return;
                }

                // Check if format=plain is requested
                string? format = _context.Request.QueryString["format"];
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

                // Check for nulls in the object chain
                if (_context == null || _context.Request == null || _context.Request.Url == null || string.IsNullOrEmpty(_context.Request.Url.AbsolutePath))
                {
                    throw new Exception("Invalid request context.");
                }

                // Extract username from the URL
                string urlUsername = _context.Request.Url.AbsolutePath.Split('/')[2]; // Assumes URL is /users/{username}

                // Check if the username matches the one from the token
                if (user.Username != urlUsername)
                {
                    throw new Exception("Access denied.");
                }

                // Fetch user profile data
                UserProfile? userProfile = _userProfileRepository.GetUserProfile(user.Id);
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
                SendResponse(ex.Message, HttpStatusCode.BadRequest);
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

                if (_context == null || _context.Request == null || _context.Request.Url == null || string.IsNullOrEmpty(_context.Request.Url.AbsolutePath))
                {
                    throw new InvalidOperationException("Invalid request context.");
                }
                string[] urlParts = _context.Request.Url.AbsolutePath.Split('/');
                if (urlParts.Length < 3)
                {
                    throw new InvalidOperationException("Invalid URL format.");
                }
                string urlUsername = urlParts[2];

                if (user.Username != urlUsername)
                {
                    throw new UnauthorizedAccessException("You are not authorized to update this profile.");
                }

                // Parse JSON payload to UserProfile object
                var updatedProfile = JsonConvert.DeserializeObject<UserProfile>(json) ?? throw new InvalidOperationException("Failed to parse user profile data.");
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
                UserStats? stats = _userStatsRepository.GetStatsByUserId(user.Id);
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

                if (scoreboardData == null)
                {
                    SendResponse("Scoreboard data not found.", HttpStatusCode.NotFound);
                    return;
                }

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
                if (!trades.Any())
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
                User user = ValidateTokenAndGetUser();

                var tradeOfferJson = JsonConvert.DeserializeObject<TradeOfferJsonModel>(json);
                if (tradeOfferJson == null)
                {
                    SendResponse("Invalid JSON format.", HttpStatusCode.BadRequest);
                    return;
                }

                var tradeOffer = new TradingOffer
                {
                    Id = tradeOfferJson.Id ?? Guid.NewGuid().ToString(),
                    OwnerId = user.Id,
                    CardId = tradeOfferJson.CardToTrade,
                    RequestedType = tradeOfferJson.Type,
                    MinDamage = tradeOfferJson.MinimumDamage ?? 0
                };

                _tradingRepository.CreateOffer(tradeOffer);
                SendResponse("Trading deal created successfully!", HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Handles a trading request
        /// </summary>
        /// <param name="json"></param>
        /// <param name="path"></param>
        private void HandleCreateTradingRequest(string json, string path)
        {
            try
            {
                User user = ValidateTokenAndGetUser();
                string tradingId = path.Split('/')[2];

                // Directly use the json string as the card ID
                string userCardId = json.Trim(['\"']); // Removes surrounding quotes

                if (string.IsNullOrEmpty(userCardId))
                {
                    SendResponse("Invalid JSON format.", HttpStatusCode.BadRequest);
                    return;
                }

                bool isTradeSuccessful = _tradingRepository.ExecuteTrade(tradingId, user.Id, userCardId);

                if (isTradeSuccessful)
                {
                    SendResponse("Trade executed successfully!", HttpStatusCode.OK);
                }
                else
                {
                    SendResponse("Trade execution failed.", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Deletes a trading deal belonging to this user or sends an error response
        /// </summary>
        /// <param name="path"></param>
        public void HandleDeleteTrading(string path)
        {
            try
            {
                User user = ValidateTokenAndGetUser();
                string tradingId = path.Split('/')[2];
                bool isDeleted = _tradingRepository.DeleteOffer(tradingId, user.Id);

                if (isDeleted)
                {
                    SendResponse("Trading offer deleted successfully.", HttpStatusCode.OK);
                }
                else
                {
                    SendResponse("Failed to delete trading offer.", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Handles battles
        /// </summary>
        public void HandleBattles()
        {
            try
            {
                User user = ValidateTokenAndGetUser();
                BattleResult battleResult = _battleService.RequestBattle(user.Id);

                // Check the status of the battle and construct an appropriate response
                if (battleResult.Status == BattleStatus.Pending)
                {
                    SendResponse($"Battle request is pending. Battle ID: {battleResult.BattleId}", HttpStatusCode.Accepted);
                }
                else if (battleResult.Status == BattleStatus.Completed)
                {
                    string resultMessage = $"Battle completed. Winner ID: {battleResult.WinnerId}.";
                    if (battleResult.WinnerId == null) // no winner, it was a draw
                    {
                        resultMessage = "The battle ended in a draw.";
                    }
                    SendResponse(resultMessage, HttpStatusCode.OK);
                }
                else if (battleResult.Status == BattleStatus.Ongoing)
                {
                    SendResponse($"Battle is ongoing. Battle ID: {battleResult.BattleId}", HttpStatusCode.Accepted);

                }
                else
                {
                    SendResponse("Battle could not be processed.", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                SendResponse($"Error: {ex.Message}", HttpStatusCode.InternalServerError);
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
            string? username = _sessionManager.GetUserFromToken(token) ?? throw new InvalidOperationException("Invalid or expired token.");
            User? user = _userRepository.GetByUsername(username) ?? throw new InvalidOperationException("User not found.");
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
            byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);

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
        private static string CreatePlainTextResponse(List<Card> cards)
        {
            StringBuilder sb = new();
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