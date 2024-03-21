using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Rokos.NodeMonitor.Monitors
{
    // Detect crashes
    internal class CrashMonitor : IMonitor
    {
        private readonly ILogger _log;

        private FileSystemWatcher _watcher;

        public bool IsPrivileged => true;

        public CrashMonitor(ILogger<CrashMonitor> log)
        {
            _log = log;
        }

        public void Start()
        {
            // Validate registry crash dump config
            using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps");

            string dumpFolder = key?.GetValue("DumpFolder")?.ToString();

            if (string.IsNullOrEmpty(dumpFolder))
            {
                _log.LogWarning("Crash dumps are not enabled on this node");
                return;
            }

            // Start monitoring location identified by crash dump config
            _log.LogInformation($"Monitoring for crash dumps at {dumpFolder}");
            _watcher = new FileSystemWatcher(dumpFolder, "*.dmp");
            _watcher.Created += (s, e) =>
            {
                OnFailure(this, new MonitorFailureEventArgs(nameof(CrashMonitor), $"Crash dump created at {e.FullPath}"));
            };
            
            // Begin watching.
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }

        public event EventHandler<MonitorFailureEventArgs>? OnFailure;
    }
}
