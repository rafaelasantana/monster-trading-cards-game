using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace mtcg
{
    public class RequestHandler
    {
        private readonly HttpListenerContext context;
        // Store registered users
        static List<User> users = new List<User>();

        public RequestHandler(HttpListenerContext context)
        {
            this.context = context;
        }

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

        private void HandleUserRegistration()
        {
            using var reader = new StreamReader(context.Request.InputStream);
            string json = reader.ReadToEnd();
            User newUser = ParseUserFromJson(json);

            if (UserExists(newUser.Username))
            {
                string errorResponse = "Username already exists!";
                SendResponse(errorResponse, HttpStatusCode.BadRequest);
            }
            else
            {
                // Assuming users is a shared resource accessible from here
                users.Add(newUser);

                PrintAllUsers();

                string successResponse = "User registered successfully!";
                SendResponse(successResponse, HttpStatusCode.OK);
            }
        }

        private void SendResponse(string responseString, HttpStatusCode statusCode)
        {
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }

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

        // checks if an username exists on the database
        private static bool UserExists(string username)
        {
            return users.Exists(u => u.Username == username);
        }

        // prints all users on the database
        private static void PrintAllUsers()
        {
            Console.WriteLine("Registered Users:");

            foreach (var user in users)
            {
                Console.WriteLine($"Username: {user.Username}, Password: {user.PasswordHash}");
            }

            Console.WriteLine();
        }
    }
}