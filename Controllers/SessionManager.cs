using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mtcg.Controllers
{
    public class SessionManager
    {
        private static readonly Dictionary<string, string> UserTokens = new Dictionary<string, string>();

        /// <summary>
        /// Creates a session token for this username and stores it locally in UserTokens
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static string CreateSessionToken(string username)
        {
            // generate token for this user
            string token = GenerateNotSecureToken(username);
            // store pair locally
            UserTokens[username] = token;
            Console.WriteLine($"Created token { token } for { username }");
            return token;
        }

        /// <summary>
        /// Generates a (not) secure token based on the username (based on curl tests format)
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private static string GenerateNotSecureToken(string username)
        {
            return $"{username}-mtcgToken";
        }

        /// <summary>
        /// Checks if a token is valid and returns the associated username
        /// </summary>
        /// <param name="token"></param>
        /// <param name="username"></param>
        /// <returns>Returns true and the associated username if the token is valid, or returns false</returns>
        public static bool IsValidSessionToken(string? token, out string? username)
        {
            // initialize username to null
            username = null;

            // check if token exists
            foreach(var pair in UserTokens)
            {
                if (pair.Value == token)
                {
                    // sets the username to the one associated with this token
                    username = pair.Key;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this token belongs to the admin user
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsAdmin(string? token)
        {
            return IsValidSessionToken(token, out var username) && username == "admin";
        }

    }
}