using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Rokos.NodeMonitor.Monitors
{
    // Detect event log entries indicating failure
    internal class EventLogMonitor : IMonitor
    {
        private ILogger _log;

        private readonly List<EventLog> _eventLogs = new();

        public bool IsPrivileged => true;

        public EventLogMonitor(ILogger<EventLogMonitor> log)
        {
            _log = log;
        }

        public void Start()
        {
            _eventLogs.Add(ListenToEventLog("Application"));
            _eventLogs.Add(ListenToEventLog("System"));
        }

        private EventLog ListenToEventLog(string eventLogName)
        {
            var eventLog = new EventLog(eventLogName);

            eventLog.EntryWritten += EventLog_EntryWritten;
            eventLog.EnableRaisingEvents = true;

            return eventLog;
        }

        public void Dispose()
        {
            foreach (var eventLog in _eventLogs)
            {
                eventLog.Dispose();
            }
        }

        public event EventHandler<MonitorFailureEventArgs>? OnFailure;

        private void EventLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            // if (e.Entry.Source == "Microsoft-Windows-Ntfs")
            // {
            //     _log.LogWarning($"Event type {e.Entry.EntryType} raised: {e.Entry.Message}");
            //     OnFailure(this, EventArgs.Empty);
            // }
            // else 
            
            if (e.Entry.EntryType == EventLogEntryType.Error || e.Entry.EntryType == EventLogEntryType.Warning)
            {
                _log.LogWarning($"Event type {e.Entry.EntryType} raised: {e.Entry.Message}");
            }
        }
    }
}
