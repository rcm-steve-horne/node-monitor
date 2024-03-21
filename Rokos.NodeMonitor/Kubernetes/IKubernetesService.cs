namespace Rokos.NodeMonitor.Kubernetes
{
    interface IKubernetesService
    {
        string? NodeIp { get; }
    }
}
