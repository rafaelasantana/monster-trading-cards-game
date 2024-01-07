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

            // Start server
            Start();
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Listening for requests on {_listener.Prefixes.FirstOrDefault() ?? "No prefixes"}");

            // Run request handling in a separate task
            Task.Run(() => HandleRequests());
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                // GetContextAsync to avoid blocking the main thread
                var contextTask = _listener.GetContextAsync();

                // Wait for the client to connect
                contextTask.Wait();

                var context = contextTask.Result;

                // Handle each request in a new task
                Task.Run(() =>
                {
                    var requestHandler = new RequestHandler(context, _dbConnectionManager);
                    requestHandler.HandleRequest();
                });
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
