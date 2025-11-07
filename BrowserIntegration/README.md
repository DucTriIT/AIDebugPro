# Browser Integration Layer

This layer handles all WebView2 and Chrome DevTools Protocol (CDP) interactions.

## Components:
- **WebView2Host**: WebView2 control management
- **DevToolsProtocol**: CDP bridge and event handlers
  - ConsoleListener
  - NetworkListener
  - PerformanceCollector
  - DOMSnapshotManager
- **ScriptExecution**: JavaScript injection and execution
- **EventProcessors**: CDP event stream processing

## Responsibilities:
- Host Chromium Edge WebView2
- Listen to console errors/warnings
- Capture network requests and timing
- Collect performance metrics (CPU, paint times)
- Extract DOM structure and HTML/JS context
