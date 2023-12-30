using System.Net;
using MTCG.Data.Repositories;

namespace MTCG.Controllers
{
    public class HttpServer
    {
        private HttpListener _listener;
        private readonly DbConnectionManager _dbConnectionManager;

        public HttpServer(string prefix, DbConnectionManager dbConnectionManager)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _dbConnectionManager = dbConnectionManager;
            // start server
            Start();
        }

        // starts the HTTP server
        public void Start()
        {
            // start server
            _listener.Start();
            Console.WriteLine($"Listening for requests on {_listener.Prefixes.FirstOrDefault() ?? "No prefixes"}");

            // handle incoming requests
            while (true)
            {
                // get context from the request
                HttpListenerContext context = _listener.GetContext();
                // create a request handler
                var requestHandler = new RequestHandler(context, _dbConnectionManager);
                // handle the request
                requestHandler.HandleRequest();
            }
        }

        // stops the HTTP server
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

    }
}