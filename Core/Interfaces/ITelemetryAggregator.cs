using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.Core.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    internal interface ITelemetryAggregator
    {
        Task<string> AggregateTelemetryAsync(string sessionId);
    }
}
