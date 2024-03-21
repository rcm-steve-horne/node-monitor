namespace Rokos.NodeMonitor.Kubernetes
{
    internal class KubernetesService : IKubernetesService
    {
        public string? NodeIp => Environment.GetEnvironmentVariable("K8S_NODE_IP");
    }
}
