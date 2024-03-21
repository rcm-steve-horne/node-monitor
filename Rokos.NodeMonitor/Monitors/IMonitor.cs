namespace Rokos.NodeMonitor.Monitors
{
    public interface IMonitor : IDisposable
    {
        bool IsPrivileged { get; }

        void Start();

        event EventHandler<MonitorFailureEventArgs> OnFailure;
    }
}
