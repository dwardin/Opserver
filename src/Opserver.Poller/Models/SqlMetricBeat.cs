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
        public double Uptime { get ; set; }
        public string Version { get; set; }

        public Dictionary<string, object> Stats { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> PerformancePerSec { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Memory { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Cache { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> WaitsPerSec { get; set; } = new Dictionary<string, object>();
        public SqlMetricBeat() => Timestamp = DateTime.Now;
    }
}
