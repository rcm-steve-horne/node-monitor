using Microsoft.Extensions.Logging;
using System.Net;

namespace Rokos.NodeMonitor.Monitors
{
    // Detect internal DNS resolution failures
    internal class DnsMonitor : IMonitor
    {
        private const string TestDnsEntry = "kube-dns.kube-system.svc.cluster.local"; // "cluster.local";
        private const int MaxRetries = 10; // 3

        private readonly ILogger _log;
        private readonly CancellationTokenSource _cts = new();

        public bool IsPrivileged => false;

        public DnsMonitor(ILogger<DnsMonitor> log)
        {
            _log = log;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var success = await TryResolveAsync(TestDnsEntry, MaxRetries);

                    // If we're unable to resolve within a set retry count, raise a failure
                    if (!success)
                    {
                        OnFailure?.Invoke(this, new MonitorFailureEventArgs(nameof(DnsMonitor), $"Unable to resolve {TestDnsEntry} after {MaxRetries} attempts"));
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }, _cts.Token);
        }

        // TODO: Make this use DnsClient again
        private async Task<bool> TryResolveAsync(string domain, int maxRetries)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Use the Dns.GetHostAddressesAsync method from System.Net namespace
                    var result = await Dns.GetHostAddressesAsync(domain);
                    if (result.Length > 0)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }

                _log.LogWarning($"Failed to successfully resolve {domain} (attempt {attempt + 1}/{maxRetries})");

                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            // If we've reached here, we've exhausted our retries and the domain is not resolvable.
            return false;
        }

        public event EventHandler<MonitorFailureEventArgs>? OnFailure;

        public void Dispose() => _cts.Cancel();
    }
}
