using System.CommandLine;
using Microsoft.Extensions.Logging;
using Rokos.NodeMonitor.Monitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Rokos.NodeMonitor;
using Rokos.NodeMonitor.FailureHandlers;
using Rokos.NodeMonitor.Kubernetes;

public class Program
{
    private static ILogger<Program> _log;
    private static IEnumerable<IMonitor> _monitors;

    public Program(ILogger<Program> log, IEnumerable<IMonitor> monitors)
    {
        _log = log;
        _monitors = monitors;
    }

    public static async Task Main(string[] args)
    {
        var privilegedOption = new Option<bool>(new[] { "-p", "--privileged" }, () => false, "Run in privileged mode");
        var rootCommand = new RootCommand { privilegedOption };

        rootCommand.SetHandler(async isPrivileged =>
        {
            var services = ConfigureServices(isPrivileged);

            var serviceProvider = services.BuildServiceProvider();

            var failureHandler = serviceProvider.GetService<IFailureHandler>();

            await serviceProvider.GetService<Program>().RunAsync(isPrivileged, failureHandler);
        }, privilegedOption);

        await rootCommand.InvokeAsync(args);
    }

    private static IServiceCollection ConfigureServices(bool isPrivileged)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddConsole(i => i.FormatterName = "customFormatter").AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
        });

        services.AddSingleton<Program>();

        services.AddSingleton<IKubernetesService, KubernetesService>();

        services.AddSingleton<IMonitor, CrashMonitor>();
        services.AddSingleton<IMonitor, DnsMonitor>();
        services.AddSingleton<IMonitor, EventLogMonitor>();
        services.AddSingleton<IMonitor, RemoteMonitor>();

        if (isPrivileged)
        {
            services.AddSingleton<IFailureHandler, PrimaryFailureHandler>();
        }
        else
        {
            services.AddSingleton<IFailureHandler, DelegatingFailureHandler>();
        }

        return services;
    }

    public async Task RunAsync(bool isPrivileged, IFailureHandler failureHandler)
    {
        _log.LogInformation($"Kubernetes node monitor (running on {System.Runtime.InteropServices.RuntimeInformation.OSDescription})");
        _log.LogInformation($"Privileged mode: {isPrivileged}");

        foreach (var monitor in _monitors.Where(i => i.IsPrivileged == isPrivileged))
        {
            _log.LogInformation($"Starting monitor {monitor.GetType().Name}");
            monitor.OnFailure += failureHandler.OnFailure;
            monitor.Start();
        }

        await Task.Delay(-1);
    }
}

