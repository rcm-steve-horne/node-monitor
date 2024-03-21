using Microsoft.Extensions.Logging;
using Rokos.NodeMonitor.Monitors;
using System.Text.Json;
using System.Text;
using Rokos.NodeMonitor.Kubernetes;

namespace Rokos.NodeMonitor.FailureHandlers
{
    internal class DelegatingFailureHandler : IFailureHandler
    {
        private readonly ILogger<DelegatingFailureHandler> _log;
        private readonly IKubernetesService _kubernetesService;

        public DelegatingFailureHandler(ILogger<DelegatingFailureHandler> log, IKubernetesService kubernetesService)
        {
            _log = log;
            _kubernetesService = kubernetesService;
        }

        public async void OnFailure(object sender, MonitorFailureEventArgs e)
        {
            _log.LogWarning($"Failure identified by monitor {e.MonitorTypeName}: {e.Message}");

            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(e);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var uri = $"http://{_kubernetesService.NodeIp}:5099/";

            _log.LogInformation($"Notifying privileged pod of failure via {uri}");

            var response = await httpClient.PostAsync(uri, content);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError($"Failed to notify the privileged pod: {response.StatusCode}");
            }
        }
    }
}
