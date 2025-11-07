using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol
{
    public interface INetworkListener
    {
        Task StartAsync(Guid sessionId);
        Task StopAsync();

    }
}
