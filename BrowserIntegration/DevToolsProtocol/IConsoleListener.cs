using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol
{
    /// <summary>
    /// Interface for listening to console events from the browser
    /// </summary>
    internal interface IConsoleListener : IDisposable
    {
        /// <summary>
        /// Starts listening to console events
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        Task StartAsync(Guid sessionId);

        /// <summary>
        /// Stops listening to console events
        /// </summary>
        Task StopAsync();
    }
}
