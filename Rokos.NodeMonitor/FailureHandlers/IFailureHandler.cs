using Rokos.NodeMonitor.Monitors;

namespace Rokos.NodeMonitor.FailureHandlers
{
    public interface IFailureHandler
    {
        void OnFailure(object sender, MonitorFailureEventArgs e);
    }
}
