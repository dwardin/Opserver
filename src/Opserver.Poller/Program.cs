using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using Opserver.Data;
using Opserver.Poller.Services;

namespace Opserver.Poller
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var serviceProvider = CreateServiceProvider();

            var ps = serviceProvider.GetService<PollingService>();
            await ps.StartAsync(new CancellationToken());
            var sqlPoller = serviceProvider.GetService<IPollSql>();

            sqlPoller.ObserveAllInstances();

            do
            {
                Thread.Sleep(2000);
            } while (sqlPoller.IsActive());

            Console.WriteLine(sqlPoller.GetStatusReason());
        }


        private static ServiceProvider CreateServiceProvider()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("opserverPollerSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("localSettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .AddMemoryCache()
                .AddSingleton(config)
                .AddCoreOpserverServices(config)
                .AddSingleton<IPollSql, PollSql>()
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

            // Console.WriteLine(configuration["elasticsearch"].ToJson());

            defaultIndex += DateTime.Now.ToString("MMdd-HHmm");

            var settings = new ConnectionSettings(new Uri(url))
                .DefaultIndex(defaultIndex)
                .EnableDebugMode();

            var client = new ElasticClient(settings);

            services.AddSingleton<IElasticClient>(client);

            return services;
        }
    }
}
