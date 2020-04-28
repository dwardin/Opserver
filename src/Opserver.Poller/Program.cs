using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Opserver.Data;
using Opserver.Data.SQL;
using Opserver.Poller.Services;

namespace Opserver.Poller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = CreateServiceProvider();

            var ps = serviceProvider.GetService<PollingService>();
            await ps.StartAsync(new CancellationToken());
            var sqlPoller = serviceProvider.GetService<PollSql>();


            Console.WriteLine("polling");
            sqlPoller.ObserveAllInstances();
 
            ConsoleKey cc;
            do
            {
                cc = Console.ReadKey().Key;
            } while (cc != ConsoleKey.Escape);
        }

        private static ServiceProvider CreateServiceProvider()
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
                .AddSingleton(config)
                .AddElasticSearch(config)
                .BuildServiceProvider();
            return serviceProvider;
        }
    }

    public static class ElasticsearchExtensions
    {
        public static IServiceCollection AddElasticSearch(
            this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["elasticsearch:url"];
            var defaultIndex = configuration["elasticsearch:index"];

            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex(defaultIndex)
                .EnableDebugMode();
            // .DefaultMappingFor<Person>(m => m
            // .Ignore(p => p.FirstName)
            // .PropertyName(p => p.Id, "id")
            // );

            var client = new ElasticClient(settings);

            services.AddSingleton<IElasticClient>(client);

            return services;
        }
    }
}
