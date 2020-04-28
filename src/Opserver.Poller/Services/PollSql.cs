#nullable enable
using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Opserver.Data;
using Opserver.Data.SQL;

namespace Opserver.Poller.Services
{
    public interface IPollSql { }

    public class PollSql : IPollSql
    {
        private readonly SQLModule _sqlModule;
        private readonly IElasticClient _elasticClient;

        public PollSql(SQLModule sqlModule, IElasticClient elasticClient)
        {
            _sqlModule = sqlModule;
            _elasticClient = elasticClient;
        }

        public void ObserveAllInstances()
        {
            var sqlInstances = _sqlModule.AllInstances;

            foreach (var sqlInstance in sqlInstances)
            {
                sqlInstance.Polled += SqlInstanceOnMonitorStatusChanged;
            }
        }

        private void SqlInstanceOnMonitorStatusChanged(object? sender, PollNode.PollResultArgs a)
        {
            if (!(sender is SQLInstance sqlInstance) || sqlInstance.ServerProperties.Data == null)
            {
                return;
            }

            // sqlInstance.PerfCounters.Data;
            var cacheHitRatio = sqlInstance
                .GetPerfCounter("Plan Cache", "Cache Hit Ratio", "_Total");

            if (cacheHitRatio != null)
            {
                Console.WriteLine("Hit ratio" + cacheHitRatio.CalculatedValue);
            }

            Console.WriteLine(sqlInstance.ServerProperties.Data.ConnectionCount);

            var indexResponse = _elasticClient.IndexDocument(sqlInstance.ServerProperties.Data);

            Console.WriteLine(indexResponse.Result.ToString());
        }
    }
}
