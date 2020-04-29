using System;
using System.Collections.Generic;
using Opserver.Data.SQL;

namespace Opserver.Poller.Models
{
    public class SqlMetricBeat
    {
        public DateTime Timestamp { get; set; }
        public string InstanceName { get; set; }
        public string MachineName { get; set; }
        public TimeSpan Uptime { get; set; }
        public string Version { get; set; }
        public Dictionary<string, object> PerfCounters { get; set; } = new Dictionary<string, object>();
        public SqlMetricBeat() => Timestamp = DateTime.Now;
    }
}
