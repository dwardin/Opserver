#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
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

            var pd = sqlInstance.ServerProperties.Data;
            var d = new SqlMetricBeat()
            {
                InstanceName = sqlInstance.Name, MachineName = pd.MachineName, Uptime = DateTime.UtcNow - pd.SQLServerStartTime, Version = pd.Version
            };

            var pc = d.PerfCounters;

            var i = sqlInstance;

            pc["jobs_count"] = sqlInstance.JobSummary.Data.Count(j => j.IsRunning);

            pc["cpu_percent"] = sqlInstance.CurrentCPUPercent;
            pc["worker_count"] = pd.CurrentWorkerCount;
            pc["session_count"] = pd.SessionCount;
            pc["connection_count"] = pd.ConnectionCount;

            /// Performance
            pc["batches_per_sec"] = i.GetPerfCounter("SQL Statistics", "Batch Requests/sec", "").CurrentValue;
            pc["compilations_per_sec"] = i.GetPerfCounter("SQL Statistics", "SQL Compilations/sec", "").CurrentValue;
            pc["recompilations_per_sec"] = i.GetPerfCounter("SQL Statistics", "SQL Re-Compilations/sec", "").CurrentValue;
            pc["guided_plans_per_sec"] = i.GetPerfCounter("SQL Statistics", "Guided plan executions/sec", "").CurrentValue;

            pc["transactions_per_sec"] = i.GetPerfCounter("Databases", "Transactions/sec", "_Total").CurrentValue;
            pc["index_searches_per_sec"] = i.GetPerfCounter("Access Methods", "Index Searches/sec", "").CurrentValue;
            pc["lock_requests_per_sec"] = i.GetPerfCounter("Locks", "Lock Requests/sec", "_Total").CurrentValue;
            pc["errors_per_sec"] = i.GetPerfCounter("SQL Errors", "Errors/sec", "_Total").CurrentValue;

            /// Memory and storage
            pc["total_server_memory_kb"] = i.GetPerfCounter("Memory Manager", "Total Server Memory (KB)", "").CalculatedValue;
            pc["target_server_memory_kb"] = i.GetPerfCounter("Memory Manager", "Target Server Memory (KB)", "").CalculatedValue;
            pc["db_cache_memory_kb"] = i.GetPerfCounter("Memory Manager", "Database Cache Memory (KB)", "").CalculatedValue;
            pc["free_memory_kb"] = i.GetPerfCounter("Memory Manager", "Free Memory (KB)", "").CalculatedValue;

            pc["data_files_kb"] = i.GetPerfCounter("Databases", "Data File(s) Size (KB)", "_Total").CalculatedValue;
            pc["log_files_kb"] = i.GetPerfCounter("Databases", "Log File(s) Size (KB)", "_Total").CalculatedValue;
            pc["log_files_used_kb"] = i.GetPerfCounter("Databases", "Log File(s) Used Size (KB)", "_Total").CalculatedValue;
            pc["temp_db_free_kb"] = i.GetPerfCounter("Transactions", "Free Space in tempdb (KB)", "").CalculatedValue;

            /// Cache
            pc["page_life_expectancy_sec"] = i.GetPerfCounter("Buffer Manager", "Page life expectancy", "").CalculatedValue;
            pc["page_lookups_per_sec"] = i.GetPerfCounter("Buffer Manager", "Page lookups/sec", "").CalculatedValue;
            pc["database_pages_count"] = i.GetPerfCounter("Buffer Manager", "Database pages", "").CalculatedValue;
            pc["database_pages_kb"] = i.GetPerfCounter("Buffer Manager", "Database pages", "").CurrentValue;

            pc["cache_objects_count"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "_Total").CalculatedValue;
            pc["cache_objects_kb"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "_Total").CurrentValue; //TODO: check unit

            pc["plans_in_cache_count"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "SQL Plans").CurrentValue;
            pc["plans_in_cache_kb"] = i.GetPerfCounter("Plan Cache", "Cache Object Counts", "SQL Plans").CalculatedValue;
            pc["plan_cache_hit_ratio"] = i.GetPerfCounter("Plan Cache", "Cache Hit Ratio", "_Total").CalculatedValue;

            /// Waits
            pc["lock_waits_per_sec"] = i.GetPerfCounter("Wait Statistics", "Lock waits", "Waits started per second").CurrentValue;
            pc["log_buffer_per_sec"] = i.GetPerfCounter("Wait Statistics", "Log buffer waits", "Waits started per second").CurrentValue;
            pc["log_write_per_sec"] = i.GetPerfCounter("Wait Statistics", "Log write waits", "Waits started per second").CurrentValue;
            pc["network_io_per_sec"] = i.GetPerfCounter("Wait Statistics", "Network IO waits", "Waits started per second").CurrentValue;
            pc["non_page_latch_per_sec"] = i.GetPerfCounter("Wait Statistics", "Non-Page latch waits", "Waits started per second").CurrentValue;
            pc["page_io_per_sec"] = i.GetPerfCounter("Wait Statistics", "Page IO latch waits", "Waits started per second").CurrentValue;
            pc["page_latch_per_sec"] = i.GetPerfCounter("Wait Statistics", "Page latch waits", "Waits started per second").CurrentValue;
            pc["thread_mem_obj_per_sec"] = i.GetPerfCounter("Wait Statistics", "Thread-safe memory objects waits", "Waits started per second").CurrentValue;
            pc["transaction_ownership_per_sec"] = i.GetPerfCounter("Wait Statistics", "Transaction ownership waits", "Waits started per second").CurrentValue;
            pc["worker_per_sec"] = i.GetPerfCounter("Wait Statistics", "Wait for the worker", "Waits started per second").CurrentValue;
            pc["workspace_sync_per_sec"] = i.GetPerfCounter("Wait Statistics", "Workspace synchronization waits", "Waits started per second").CurrentValue;
            pc["memory_grant_per_sec"] = i.GetPerfCounter("Wait Statistics", "Memory grant queue waits", "Cumulative wait time (ms) per second").CurrentValue;

            var indexResponse = _elasticClient.IndexDocument(d);

            Console.WriteLine(indexResponse.Result.ToString());
        }
    }
}
