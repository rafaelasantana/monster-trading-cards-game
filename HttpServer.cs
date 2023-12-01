using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace mtcg
{
    public class HttpServer
    {
        private HttpListener listener;
        private readonly DbConnectionManager dbConnectionManager;

        public HttpServer(string prefix, DbConnectionManager dbConnectionManager)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            this.dbConnectionManager = dbConnectionManager;
            // start server
            Start();
        }

        // starts the HTTP server
        public void Start()
        {
            // start server
            listener.Start();
            Console.WriteLine($"Listening for requests on {listener.Prefixes.FirstOrDefault() ?? "No prefixes"}");

            // handle incoming requests
            while (true)
            {
                // get context from the request
                HttpListenerContext context = listener.GetContext();
                // create a request handler
                var requestHandler = new RequestHandler(context, dbConnectionManager);
                // handle the request
                requestHandler.HandleRequest();
            }
        }

        // stops the HTTP server
        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }

    }
}