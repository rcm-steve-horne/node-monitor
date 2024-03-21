using k8s;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Rokos.NodeMonitor.Monitors;

namespace Rokos.NodeMonitor.FailureHandlers
{
    internal class PrimaryFailureHandler : IFailureHandler
    {
        private readonly ILogger<PrimaryFailureHandler> _log;

        private const int FailureLimit = 3;
        private const int MaxEmailCount = 5;

        private int _emailCount;
        private bool _failureHandled;

        public PrimaryFailureHandler(ILogger<PrimaryFailureHandler> log)
        {
            _log = log;
        }

        public void OnFailure(object sender, MonitorFailureEventArgs e)
        {
            try
            {
                if (_failureHandled)
                {
                    return;
                }

                _log.LogWarning($"Failure identified by monitor {e.MonitorTypeName}: {e.Message}");

                var nodeName = Environment.MachineName.ToLower();

                var k8sHost = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
                var k8sPort = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT");
                var accessToken = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_TOKEN");

                // Get node restart count
                var config = new KubernetesClientConfiguration
                {
                    AccessToken = accessToken,
                    Host = $"https://{k8sHost}:{k8sPort}",
                    SkipTlsVerify = true
                };

                var k8s = new k8s.Kubernetes(config);

                var node = k8s.ReadNode(nodeName);

                node.Metadata.Annotations ??= new Dictionary<string, string>();

                const string annotationKey = "rokos.corp/nodeCrashCount";
                int failureCount;

                if (node.Metadata.Annotations.TryAdd(annotationKey, "1"))
                {
                    failureCount = 1;
                }
                else
                {
                    // If the annotation exists, get its value and increment it.
                    failureCount = int.Parse(node.Metadata.Annotations[annotationKey]) + 1;
                    node.Metadata.Annotations[annotationKey] = failureCount.ToString();
                }

                // Update the node with changed annotations.
                k8s.ReplaceNode(node, nodeName);

                _log.LogInformation($"This is failure number {failureCount} for node {nodeName}");

                if (failureCount < FailureLimit)
                {
                    _log.LogInformation($"Rebooting node {nodeName}");

                    SendEmail($"Restarted node {nodeName}",
                        $"Restarted node {nodeName} due to {e.MonitorTypeName} failure (failure {failureCount}/{FailureLimit}): {e.Message}");

                    // Reboot node
                    //Process.Start("shutdown", "/r /t 0");
                }
                else
                {
                    _log.LogInformation($"Deleting node {nodeName} from AKS");

                    // Delete node from AKS
                    SendEmail($"Deleted node {nodeName} from AKS",
                        $"Deleted node {nodeName} from AKS due to {e.MonitorTypeName} failure (failure {failureCount}/{FailureLimit}): {e.Message}");

                    //k8s.DeleteNode(nodeName);
                }

                _failureHandled = true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex.ToString());
            }
        }

        void SendEmail(string subject, string body)
        {
            if (_emailCount++ > MaxEmailCount)
            {
                _log.LogWarning("Breached email limit, halting emails");
                return;
            }

            var client = new SmtpClient("mail.rokoscapital.com");

            //client.UseDefaultCredentials = false;
            //client.Credentials = new System.Net.NetworkCredential("username", "password");

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("nodemonitor@rokoscapital.com");
            mailMessage.To.Add("steve.horne@rokoscapital.com");
            mailMessage.Subject = subject;
            mailMessage.Body = body;

            client.Send(mailMessage);
        }
    }
}
