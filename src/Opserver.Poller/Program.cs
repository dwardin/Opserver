using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opserver.Data;
using Opserver.Data.SQL;
using Opserver.Poller.Services;

namespace Opserver.Poller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("opserverPollerSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("localSettings.json", optional: true, reloadOnChange: true)
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddMemoryCache()
                .AddCoreOpserverServices(config)
                .AddStatusModules()
                .AddSingleton<PollSql, PollSql>()
                .AddSingleton<IConfiguration>(config)
                .BuildServiceProvider();

            // var sqlModule = new SQLModule(config, serviceProvider.GetService<PollingService>());
            var sqlModule = serviceProvider.GetService<SQLModule>();

            var ps = serviceProvider.GetService<PollingService>();
            await ps.StartAsync(new CancellationToken());
            while (true)
            {
                Console.WriteLine("polling");

                if (sqlModule.AllInstances.First().ServerProperties.Data == null)
                {
                    Console.WriteLine("Waiting for data");
                    Thread.Sleep(1000);
                    continue;
                }

                Console.WriteLine(sqlModule.AllInstances.First().ServerProperties.Data.ConnectionCount);
                Thread.Sleep(1000);
                // Console.ReadKey(); // != ConsoleKey.Escape;
            }
        }
    }
}
