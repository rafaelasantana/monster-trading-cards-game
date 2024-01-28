
## Introduction

This project involves the creation of a backend server for the Monster Trading Cards Game (MTCG), a platform for players to engage in a magical card game involving trading and battling with various types of cards. 

The server is designed to handle key game functionalities including user management, card trading, battle mechanics, and maintaining user stats and scoreboards.
## Technical Steps
### Design and Architecture

**Layered Architecture** 
This application follows a layered architecture, separating concerns into controllers, models, repositories, and services. 

**Responsibility Segregation** 
Each component has a clearly defined responsibility, whether it's handling HTTP requests, managing business logic, or interacting with the database.

![[<img width="765" alt="architecture-overview" src="https://github.com/rafaelasantana/monster-trading-cards-game/assets/60327926/f4bbf4e2-efcf-4ae6-81e8-b20918d50acb">
]]

#### Program

- **Configuration Setup** 
	- Loads application settings, including the database connection string and server URL, from `appsettings.json`.
- **Database Connection Initialization** 
	- Creates a database connection manager using the provided connection string.
- **HTTP Server Setup** 
	- Initializes the HTTP server with the configured URL and database connection manager.
- **Server Run Management** 
	- Keeps the server running and listens for a shutdown command (like CTRL-C) to gracefully stop the server.
#### Controllers

- The controllers are designed to separate concerns.
	- **HttpServer** focuses on server management.
	- **RequestHandler** handles application logic and routing for HTTP requests.
	- **SessionManager** is dedicated to session and token management.

**HttpServer**

- Manages the HTTP server's lifecycle.
	-  Initializes and starts the `HttpListener`.
	- Listens for incoming HTTP requests and handles them asynchronously.
	- Stops and closes the server upon termination.

**RequestHandler**

- Processes incoming HTTP requests and delegates actions based on the request type (`GET`, `POST`, `PUT`, `DELETE`).
	- Parses request data and routes it to the appropriate handling function.
	- Manages various aspects of the game, such as user registration and login, package creation and purchase, card and deck management, battles and trading.
	- Communicates with repositories to retrieve or update data in the database.
	- Sends responses back to the client, either as plain text or JSON.
	- Integrates with services like `BattleService` and `UserService` for specific functionalities.

**SessionManager**

- Manages user sessions and authentication tokens.
	- Generates and validates session tokens.
	- Associates tokens with usernames for session management.
#### Data/Models

- Common characteristics of Model classes
	 - **Constructors:** 
		 - Each model has default constructors and parameterized constructors for different use cases.
	- **Property Types:** 
		- Nullable types (`int?`, `string?`, etc.) are used, allowing for flexibility in property assignments and handling of optional or missing data.
	- **Methods:** 
		- Models like `Package` include specific methods (`PrintCards`, `GetCards`, etc.) related to their functionality.

- Models in the project
	- `Battle.cs`
	- `BattleResult.cs`
	- `Card.cs`
	- `ExtendedTradingOffer.cs`
	- `Package.cs`
	- `RoundResult.cs`
	- `Scoreboard.cs`
	- `TradeOfferJsonModel.cs`
	- `TradingOffer.cs`
	- `Transaction.cs`
	- `User.cs`
	- `UserProfile.cs`
	- `UserStats.cs`
#### Data/Repositories

- Common characteristics of Repository classes
	- **Database Connection Management:** 
		- Each repository uses an `IDbConnectionManager` to manage database connections.
	- **SQL Queries Execution:** 
		- Repositories perform CRUD (Create, Read, Update, Delete) operations using SQL queries.
	- **Parameterized Queries:** 
		- Use of parameterized queries for security and to handle nullable fields.
	- **Data Mapping:** 
		- Utilization of the `DataMapperService` to map database records to corresponding model objects.
	- **Exception Handling:** 
		- Repositories manage potential exceptions and provide informative messages for failure scenarios.

- Repositories in the project
	- `BattleRepository.cs`
	- `BattleResultRepository.cs`
	- `CardRepository.cs`
	- `ExtendedTradingOfferRepository.cs`
	- `PackageRepository.cs`
	- `RoundResultRepository.cs`
	- `ScoreboardRepository.cs`
	- `TradeOfferJsonModelRepository.cs`
	- `TradingOfferRepository.cs`
	- `TransactionRepository.cs`
	- `UserRepository.cs`
	- `UserProfileRepository.cs`
	- `UserStatsRepository.cs`
#### Data/Services

**BattleService**

- responsible for managing and orchestrating battles in the game. 
	- handles player requests to enter battles
	- validates player decks
	- conducts battles between players
	- updates battle results
	- calculates Elo ratings for players based on battle outcomes

**DataMapperService**

- used to convert database query results into the corresponding Model object
	- provides a generic method for mapping data from a `NpgsqlDataReader` to a model object. 
	- iterates through the properties of the model object, retrieves corresponding data from the data reader, and populates the object's properties.

**UserService**

- provides essential functionality for user registration and authentication within the application
	- **RegisterUser**: 
		- takes a `User` object as input, validates the data, ensures that the username is unique, and creates user records in the `UserRepository`, `UserProfileRepository`, and `UserStatsRepository.
	- **LoginUser**: 
		- takes a username and password as input, checks if the provided credentials match any registered user, and returns the user if authentication is successful. 
		- uses password hashing for security.
#### Database Design

![[<img width="631" alt="database-overview" src="https://github.com/rafaelasantana/monster-trading-cards-game/assets/60327926/61b13bbd-86c4-491b-ad6e-49393c66821c">
]]

### Development Process

1. **Model Definition**
	- The development began with the definition of model classes, that represent the core data structures used in the application. 
2. **Database Design**: 
	-  Initially, there was an attempt to create the models first and then work on the database. 
	- **Challenges**
		- There were challenges in parsing data from the database records into these objects. 
		- This approach introduced complexity and inefficiencies.
	-  **Revised Approach** 
		- The focus shifted to database design and implementation before creating the matching models. 
			- This decision significantly improved the development process by ensuring that the database schema aligns seamlessly with the application's needs.
1. **Database Repositories**: 
	- The development process involved the creation of database repositories responsible for interacting with the database for CRUD operations. 
2. **Business Logic**: 
	- With a well-structured database in place, the development process continued by implementing the core business logic of the application. 
	- This included features such as user registration, package creation and acquisition, card trading, and battling.
3. **Authentication and Security**: 
	- Security measures were implemented, such as password hashing using `BCrypt`, to ensure the protection of user credentials and data integrity.
## Unit Testing

### DataMapperServiceTests

- **Summary**
	- This test class focuses on unit testing the `DataMapperService` class, specifically the `MapToObject<T>` method.
- **Significance**
	- It ensures that the data mapping functionality from database tables to C# models (e.g., mapping data from the `users` table to the `User` model) is working correctly. 
	- By testing the mapping logic, it helps maintain data integrity and consistency when interacting with the database, which is crucial for the overall reliability of the application.
- **Risks**
	- Incorrect mapping can lead to data corruption, loss of data integrity, and potential security vulnerabilities. Mismanagement of data can also result in application crashes or unexpected behavior, affecting user experience.

### BattleServiceTests

- **Summary**
	- This test class focuses on unit testing the `BattleService` class, which handles various aspects of player battles, including requesting battles, conducting battles, and updating player statistics.
- **Significance**
	- These tests are essential to verify that the battle-related functionality of the application works as expected. 
	- They ensure that battles can be requested, conducted, and that player statistics are correctly updated after each battle. 
- **Risks**
	- Flaws in the battle logic can lead to unfair gameplay, user frustration, and decreased engagement. Inconsistencies in updating player statistics can result in data inaccuracies and impact the competitive balance of the game.
  
### TradingTests

- **Summary**
	- This test class is responsible for unit testing the trading functionality of the application, which includes creating trading offers, executing trades, and handling various trade conditions and exceptions.
- **Significance**
	- The trading functionality is a complex feature, it involves multiple steps and conditions, such as verifying ownership of cards, ensuring cards are not in a user's deck, and meeting specific trade requirements. 
	- The tests ensure that trading offers are created and executed correctly, and that trade conditions and edge cases are properly handled. 
- **Risks**
	- Faults in the trading system can lead to the loss of user items or currency, exploitation of the trading system, and overall dissatisfaction among users. Handling edge cases incorrectly can also lead to security vulnerabilities.

### UserServiceTests

- **Summary**
	- The `UserServiceTests` class contains a suite of tests that evaluate the functionality of the `UserService` in managing user-related operations within the application. These tests cover user registration, login, updates to user details, and data consistency.
- **Significance**
	- The tests in this class are essential for ensuring that the user management system of the application functions correctly and securely. 
	- They validate the core features that allow users to create accounts, log in, modify their information, and maintain data integrity.
- **Risks**
	- Inadequate testing in this area can lead to critical security breaches, unauthorized access, and compromise of user data. 

## Time Tracking

Reflecting on the around 200-hour investment in this project, it's clear that the initial approach influenced the overall time distribution. Starting with model classes seemed intuitive but led to later adjustments:

1. **Model Classes as the Starting Point**
    - Initial efforts focused on developing model classes. This approach required significant time for revisions once the project's requirements evolved, especially during database integration.
2. **Insight for Future Projects**
    - A more efficient method would have been to first define the database schemas, then align them with the model classes, and finally build the logic. This would likely have saved time and reduced the need for revising the models.
## Appendices

**GitHub Repository**
https://github.com/rafaelasantana/monster-trading-cards-game
