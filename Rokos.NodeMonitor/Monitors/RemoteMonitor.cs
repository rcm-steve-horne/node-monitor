using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Rokos.NodeMonitor.Monitors
{
    // Monitor the local non-privileged pod by providing an endpoint for it to call
    internal class RemoteMonitor : IMonitor
    {
        private readonly ILogger<RemoteMonitor> _log;
        private readonly HttpListener _httpListener = new();

        public RemoteMonitor(ILogger<RemoteMonitor> log)
        {
            _log = log;
            _httpListener.Prefixes.Add("http://+:5099/");
        }

        public void Start()
        {
            _httpListener.Start();
            _httpListener.BeginGetContext(OnRequestReceived, null);
        }

        private void OnRequestReceived(IAsyncResult result)
        {
            try
            {
                var context = _httpListener.EndGetContext(result);

                _log.LogInformation($"Received notification from {context.Request.RemoteEndPoint}");

                _httpListener.BeginGetContext(OnRequestReceived, null);

                if (context.Request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    var content = reader.ReadToEnd();
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Close();

                    var eventArgs = JsonSerializer.Deserialize<MonitorFailureEventArgs>(content);

                    OnFailure?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to process incoming request");
            }
        }

        public void Dispose()
        {
            _httpListener.Stop();
            _httpListener.Close();
        }


        public bool IsPrivileged => true;

        public event EventHandler<MonitorFailureEventArgs>? OnFailure;
    }
}
