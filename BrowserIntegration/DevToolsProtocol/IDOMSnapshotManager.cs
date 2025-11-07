using System;
using System.Threading;
using System.Threading.Tasks;
using AIDebugPro.Core.Models;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol
{
    public interface IDOMSnapshotManager
    {
        /// <summary>
        /// Captures a DOM snapshot from the current browser page
        /// </summary>
        /// <returns>The captured DOM snapshot</returns>
        Task CaptureSnapshotAsync(Guid sessionId);

        /// <summary>
        /// Captures a screen snapshot from a specific node in the DOM
        /// </summary>
        /// <returns>The captured DOM snapshot</returns>
        Task<byte[]> CaptureScreenshotAsync(int? nodeId = null);
        Task<string> GetNodeDetailsAsync(int nodeId);
        Task HighlightNodeAsync(int nodeId);
        Task HideHighlightAsync();
        /// <summary>
        /// Clears all stored snapshots
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
    }
}
