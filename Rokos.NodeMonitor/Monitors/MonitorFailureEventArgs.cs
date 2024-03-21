namespace Rokos.NodeMonitor.Monitors
{
    public class MonitorFailureEventArgs : EventArgs
    {
        public string MonitorTypeName { get; }

        public string Message { get; }

        public MonitorFailureEventArgs(string monitorTypeName, string message)
        {
            MonitorTypeName = monitorTypeName;
            Message = message;
        }
    }
}
