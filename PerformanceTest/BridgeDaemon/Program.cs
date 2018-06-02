using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using BridgeDaemon.Options;
using System.Threading;
using TinyBlockStorage.Blob;

namespace BridgeDaemon
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();
            var options = serviceProvider.GetRequiredService<IOptions<DaemonOptions>>().Value;

            var exitEvent = SetupExitHandler();

            Log.Information("Started.");
            Eternity.Initialize(serviceProvider);

            Enumerable
                .Range(1, 10)
                .Select(i => 2000 + i)
                .ToList()
                .ForEach(c => Eternity.AddJob<MqttMsgClient>(c));

            Eternity.Start(options.MonitorEverySeconds);

            exitEvent.Wait();

            Log.Information("Shutting down..");
            Eternity.Stop();
            Log.CloseAndFlush();
        }

        private static ManualResetEventSlim SetupExitHandler()
        {
            var exitEvent = new ManualResetEventSlim();

            // Allow exit with SIGTERM
            AssemblyLoadContext.Default.Unloading += (AssemblyLoadContext obj) =>
            {
                exitEvent.Set();
            };

            // Allow exit with CTRL+C (SIGINT)
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
            return exitEvent;
        }

        private static IServiceProvider ConfigureServices()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .CreateLogger();

            return new ServiceCollection()
                .Configure<DaemonOptions>(options =>
                {
                    options.GatewayId = int.Parse(config.GetSection("DaemonOptions:GatewayId").Value);
                    options.MonitorEverySeconds = int.Parse(config.GetSection("DaemonOptions:MonitorEverySeconds").Value);
                })
                .AddSingleton<BlobDatabase>(_ =>
                    new BlobDatabase(config.GetConnectionString("Default")))
                .BuildServiceProvider();
        }
    }
}
