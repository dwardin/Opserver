using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nest;
using Opserver.Data;
using Opserver.Data.SQL;
using Opserver.Poller.Models;

namespace Opserver.Poller.Services
{
    public interface IPollSql
    {
        public void ObserveAllInstances();
    }

    public class PollSql : IPollSql
    {
        private readonly SQLModule _sqlModule;
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<IPollSql> _logger;

        public PollSql(SQLModule sqlModule, IElasticClient elasticClient, ILogger<IPollSql> logger)
        {
            _sqlModule = sqlModule;
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public void ObserveAllInstances()
        {
            var sqlInstances = _sqlModule.AllInstances;

            foreach (var sqlInstance in sqlInstances)
            {
                _logger.LogInformation("Observing " + sqlInstance.Name);
                sqlInstance.Polled += SqlInstanceOnMonitorStatusChanged;
            }
        }

        private void SqlInstanceOnMonitorStatusChanged(object sender, PollNode.PollResultArgs a)
        {
            _logger.LogDebug("Polled");
            if (!(sender is SQLInstance sqlInstance))
            {
                return;
            }

            if (sqlInstance.ServerProperties.Data == null)
            {
                _logger.LogWarning($"No ServerProperties data for ${sqlInstance.Name} skipping ");
            }

            var esDoc = new SqlMetricBeat()
            {
                InstanceName = sqlInstance.Name,
                MachineName = sqlInstance.ServerProperties.Data.MachineName,
                Uptime = (DateTime.UtcNow - sqlInstance.ServerProperties.Data.SQLServerStartTime).TotalSeconds,
                Version = sqlInstance.ServerProperties.Data.Version,
                Stats = GetStats(sqlInstance),
                PerformancePerSec = GetPerformance(sqlInstance),
                Memory = GetMemory(sqlInstance),
                Cache = GetCache(sqlInstance),
                WaitsPerSec = GetWaits(sqlInstance),
            };


            var indexResponse = _elasticClient.IndexDocument(esDoc);

            if (indexResponse == null)
            {
                _logger.LogWarning("No response from ES");
                return;
            }

            _logger.LogDebug($"ES: Result:{indexResponse.Result}, seqNo:{indexResponse.SequenceNumber}");
        }

        private static Dictionary<string, object> GetStats(SQLInstance i)
        {
            return new Dictionary<string, object>
            {
                ["cpu_percent"] = i.CurrentCPUPercent,
                ["worker_count"] = i.ServerProperties.Data.CurrentWorkerCount,
                ["session_count"] = i.ServerProperties.Data.SessionCount,
                ["connection_count"] = i.ServerProperties.Data.ConnectionCount,
                ["jobs_count"] = i.JobSummary.Data.Count(j => j.IsRunning)
            };
        }

        private static Dictionary<string, object> GetWaits(SQLInstance i)
        {
            return new Dictionary<string, object>
            {
                ["lock"] = i.GetPerfCounter("Wait Statistics", "Lock waits", "Waits started per second")?.CurrentValue,
                ["log_buffer"] = i.GetPerfCounter("Wait Statistics", "Log buffer waits", "Waits started per second")?.CurrentValue,
                ["log_write"] = i.GetPerfCounter("Wait Statistics", "Log write waits", "Waits started per second")?.CurrentValue,
                ["network_io"] = i.GetPerfCounter("Wait Statistics", "Network IO waits", "Waits started per second")?.CurrentValue,
                ["non_page_latch"] = i.GetPerfCounter("Wait Statistics", "Non-Page latch waits", "Waits started per second")?.CurrentValue,
                ["page_io"] = i.GetPerfCounter("Wait Statistics", "Page IO latch waits", "Waits started per second")?.CurrentValue,
                ["page_latch"] = i.GetPerfCounter("Wait Statistics", "Page latch waits", "Waits started per second")?.CurrentValue,
                ["thread_mem_obj"] =
                    i.GetPerfCounter("Wait Statistics", "Thread-safe memory objects waits", "Waits started per second")?.CurrentValue,
                ["transaction_ownership"] = i.GetPerfCounter("Wait Statistics", "Transaction ownership waits", "Waits started per second")
                    ?.CurrentValue,
                ["worker"] = i.GetPerfCounter("Wait Statistics", "Wait for the worker", "Waits started per second")?.CurrentValue,
                ["workspace_sync"] = i.GetPerfCounter("Wait Statistics", "Workspace synchronization waits", "Waits started per second")?.CurrentValue,
                ["memory_grant"] = i.GetPerfCounter("Wait Statistics", "Memory grant queue waits", "Cumulative wait time (ms) per second")?.CurrentValue
            };
        }

        private static Dictionary<string, object> GetCache(SQLInstance i)
        {
            return new Dictionary<string, object>
            {
                ["page_life_expectancy_sec"] = i.GetPerfCounter("Buffer Manager", "Page life expectancy", "")?.CalculatedValue,
                ["page_lookups_per_sec"] = i.GetPerfCounter("Buffer Manager", "Page lookups/sec", "")?.CalculatedValue,
                ["database_pages_count"] = i.GetPerfCounter("Buffer Manager", "Database pages", "")?.CalculatedValue,
                ["database_pages_kb"] = i.GetPerfCounter("Buffer Manager", "Database pages", "")?.CalculatedValue * 8,
                ["cache_objects_count"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "_Total")?.CalculatedValue,
                ["cache_objects_kb"] = i.GetPerfCounter("Plan Cache", "Cache Pages", "_Total")?.CurrentValue,
                ["plans_in_cache_count"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "SQL Plans")?.CurrentValue,
                ["plans_in_cache_kb"] = i.GetPerfCounter("Plan Cache", "Cache Pages", "SQL Plans")?.CalculatedValue,
                ["plan_cache_hit_ratio"] = i.GetPerfCounter("Plan Cache", "Cache Hit Ratio", "_Total")?.CalculatedValue
            };
        }

        private static Dictionary<string, object> GetMemory(SQLInstance i)
        {
            return new Dictionary<string, object>
            {
                ["current_memory_kb"] = i.GetPerfCounter("Memory Manager", "Total Server Memory (KB)", "")?.CalculatedValue,
                ["target_memory_kb"] = i.GetPerfCounter("Memory Manager", "Target Server Memory (KB)", "")?.CalculatedValue,
                ["db_cache_memory_kb"] = i.GetPerfCounter("Memory Manager", "Database Cache Memory (KB)", "")?.CalculatedValue,
                ["free_memory_kb"] = i.GetPerfCounter("Memory Manager", "Free Memory (KB)", "")?.CalculatedValue,
                ["data_files_kb"] = i.GetPerfCounter("Databases", "Data File(s) Size (KB)", "_Total")?.CalculatedValue,
                ["log_files_kb"] = i.GetPerfCounter("Databases", "Log File(s) Size (KB)", "_Total")?.CalculatedValue,
                ["log_files_used_kb"] = i.GetPerfCounter("Databases", "Log File(s) Used Size (KB)", "_Total")?.CalculatedValue,
                ["temp_db_free_kb"] = i.GetPerfCounter("Transactions", "Free Space in tempdb (KB)", "")?.CalculatedValue
            };
        }

        private static Dictionary<string, object> GetPerformance(SQLInstance i)
        {
            return new Dictionary<string, object>
            {
                ["batches"] = i.GetPerfCounter("SQL Statistics", "Batch Requests/sec", "")?.CalculatedValue,
                ["compilations"] = i.GetPerfCounter("SQL Statistics", "SQL Compilations/sec", "")?.CalculatedValue,
                ["recompilations"] = i.GetPerfCounter("SQL Statistics", "SQL Re-Compilations/sec", "")?.CalculatedValue,
                ["guided_plans"] = i.GetPerfCounter("SQL Statistics", "Guided plan executions/sec", "")?.CalculatedValue,
                ["transactions"] = i.GetPerfCounter("Databases", "Transactions/sec", "_Total")?.CalculatedValue,
                ["index_searches"] = i.GetPerfCounter("Access Methods", "Index Searches/sec", "")?.CalculatedValue,
                ["lock_requests"] = i.GetPerfCounter("Locks", "Lock Requests/sec", "_Total")?.CalculatedValue,
                ["errors"] = i.GetPerfCounter("SQL Errors", "Errors/sec", "_Total")?.CalculatedValue
            };
        }
    }
}
