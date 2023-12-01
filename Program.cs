using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace mtcg
{
    class Program
    {
        static void Main(string[] args)
        {
            // build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // create database manager
            var dbConnectionManager = new DbConnectionManager(configuration);

            // // create HTTP server
            // HttpServer server = new(configuration["ServerUrl"]!);

            var userRepo = new UserRepository(dbConnectionManager);
            User newUser = new User("testUser", "testPassword");
            userRepo.Save(newUser);
            userRepo.Save(newUser);
        }
    }
}
