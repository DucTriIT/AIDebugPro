# AIDebugPro - AI-Powered Browser Debugging Assistant

[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE.txt)
[![OpenAI](https://img.shields.io/badge/AI-GPT--4-orange)](https://openai.com/)
[![Status](https://img.shields.io/badge/status-Production%20Ready-brightgreen)](https://github.com/DucTriIT/AIDebugPro)

**AIDebugPro** is an enterprise-grade browser debugging tool that uses **OpenAI GPT-4** to analyze web application telemetry and provide intelligent debugging insights. Capture console errors, network requests, performance metrics, and DOM snapshots in real-time, then let AI identify issues and recommend fixes.

---

## ?? What's New in v1.0 ?

### ? **Recently Completed Features**

- ? **Full AI Analysis Integration** - Complete workflow from telemetry capture to AI-powered insights
- ? **Session Persistence** - Save and load debugging sessions with all telemetry and AI analyses
- ? **Comprehensive Export** - Export reports in HTML, Markdown, and JSON formats with live statistics
- ? **Snapshot-Based Architecture** - Proper telemetry snapshot management for historical analysis
- ? **Network Timestamp Fix** - Accurate timestamp capture for network requests (now shows in Performance tab)
- ? **Session Import/Export** - Full session lifecycle management with `.aidp` file format

---

## ?? Quick Start (5 Minutes)

### **Prerequisites**
- Windows 10/11 (x64)
- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- OpenAI API Key ([Get one here](https://platform.openai.com/api-keys))
- Visual Studio 2022 or VS Code (optional)

### **Installation**

```bash
# 1. Clone the repository
git clone https://github.com/DucTriIT/AIDebugPro.git
cd AIDebugPro

# 2. Restore NuGet packages
dotnet restore

# 3. Configure your OpenAI API key (User Secrets)
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key-here"

# 4. Build the project
dotnet build

# 5. Run the application
dotnet run
```

### **First Steps**

1. **Create a Session:** `File ? New Session` ? Enter session name
2. **Navigate to a Website:** Enter URL in the browser panel ? Click **Go**
3. **Start Capture:** Click **? Start Capture** button
4. **Browse & Interact:** Use the website to generate telemetry (errors, network requests)
5. **Analyze with AI:** Click **?? Analyze with AI** button
6. **Review Results:** Check the AI Assistant panel for insights and recommendations
7. **Export Report:** Click **?? Export Report** ? Choose format (HTML/Markdown/JSON)
8. **Save Session:** Click **File ? Save Session** to preserve your debugging work

---

## ? Key Features

### ?? **Browser Integration**
- **WebView2** - Full Chromium-based browser (Edge WebView2 Runtime)
- **Chrome DevTools Protocol (CDP)** - Low-level browser instrumentation
- Real-time console message capture (errors, warnings, logs, info)
- Network request/response tracking with accurate timestamps
- Performance metrics (FCP, LCP, load time, memory, CPU)
- DOM snapshot capture for state preservation

### ?? **AI-Powered Analysis**
- **GPT-4 Integration** - Intelligent error analysis with context awareness
- Automatic issue categorization (JavaScript, Network, Performance, Security)
- Root cause analysis with detailed explanations
- Actionable fix recommendations with priority levels
- Code examples and best practices
- Session-wide telemetry analysis

### ?? **Telemetry Dashboard**
- **Console Tab** - JavaScript errors, warnings, logs (color-coded by severity)
- **Network Tab** - HTTP requests with status codes, duration, size, timestamps
- **Performance Tab** - Load times, paint metrics, memory usage (last 30 seconds)
- Real-time updates with automatic buffering
- Duplicate detection and filtering
- Context menu integration ("Ask AI about this error")

### ?? **Session Management**
- Create, save, and load debug sessions (`.aidp` format)
- Session statistics (errors, warnings, network failures)
- Snapshot-based telemetry preservation
- AI analysis results linked to sessions
- Export reports in multiple formats (HTML, Markdown, JSON)
- Archive sessions for future reference

### ?? **Security & Privacy**
- **User Secrets** for API key storage (development)
- PII redaction in telemetry data
- Configurable data retention policies
- Local-first architecture (no cloud dependency)
- Secure JSON serialization

---

## ?? Documentation

- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Complete folder structure and organization
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Architecture overview & data flow diagrams
- **[AI_CHAT_INTEGRATION_SPEC.md](AI_CHAT_INTEGRATION_SPEC.md)** - AI assistant specification
- **[WEEK1_TESTING_GUIDE.md](WEEK1_TESTING_GUIDE.md)** - Comprehensive testing guide
- **[TROUBLESHOOTING_GUIDE.md](TROUBLESHOOTING_GUIDE.md)** - Common issues and solutions

---

## ?? Testing

### **Manual Testing**

1. **Run the application:**
   ```bash
   dotnet run
   ```

2. **Test Navigation:**
   - Enter URL: `https://example.com`
   - Click **Go** button
   - Verify website loads in WebView2 panel

3. **Test Console Capture:**
   - Navigate to: `https://example.com`
   - Open browser DevTools (F12) in the WebView2 panel
   - Type in console: `console.error("Test error")`
   - Check **Logs Dashboard ? Console tab** for the error

4. **Test Network Capture:**
   - Navigate to: `https://jsonplaceholder.typicode.com/posts/1`
   - Check **Logs Dashboard ? Network tab** for HTTP requests
   - Verify timestamps are current (not 1970)

5. **Test Performance Metrics:**
   - Navigate to any website
   - Check **Logs Dashboard ? Performance tab** for metrics
   - Verify metrics appear in last 30 seconds

6. **Test AI Analysis:**
   - Create a session
   - Navigate to a page with errors
   - Capture telemetry (10+ seconds)
   - Click **?? Analyze with AI**
   - Review results in AI Assistant panel
   - Verify issues and recommendations are displayed

7. **Test Session Save/Load:**
   - Create a session with captured telemetry
   - Click **File ? Save Session** ? Save as `test.aidp`
   - Click **File ? Open Session** ? Select `test.aidp`
   - Verify session loads with all snapshots and analyses

8. **Test Report Export:**
   - After capturing telemetry and running AI analysis
   - Click **File ? Export Report**
   - Choose HTML format
   - Verify report contains actual statistics (not zeros)
   - Open exported HTML file to verify formatting

### **Unit Tests (Coming Soon)**

```bash
dotnet test
```

---

## ?? Configuration

### **User Secrets (Development)**

For development, use User Secrets to store your OpenAI API key securely:

```bash
# Initialize User Secrets (already configured in .csproj)
dotnet user-secrets init

# Set your OpenAI API key
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key-here"

# Verify it's set
dotnet user-secrets list
```

### **appsettings.json**

The application uses `Services/Configuration/appsettings.json` for configuration:

```json
{
  "OpenAI": {
    "ApiKey": "",  // Leave empty - use User Secrets
    "Model": "gpt-4-turbo-preview",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "TimeoutSeconds": 120
  },
  "Database": {
    "Path": "data/aidebugpro.db",
    "EnableAutoCompact": true,
    "BackupRetentionDays": 30
  },
  "Telemetry": {
    "MaxConsoleMessages": 1000,
    "MaxNetworkRequests": 500,
    "AutoCapture": false
  },
  "Logging": {
    "LogLevel": "Information",
    "Path": "logs/aidebugpro-.txt",
    "RetentionDays": 30
  }
}
```

### **Environment Variables**

You can also use environment variables (useful for production):

```bash
# Windows PowerShell
$env:OpenAI__ApiKey = "sk-your-key-here"

# Windows CMD
set OpenAI__ApiKey=sk-your-key-here

# Linux/Mac
export OpenAI__ApiKey="sk-your-key-here"
```

**Note:** Use double underscores `__` to represent nested configuration (e.g., `OpenAI:ApiKey` ? `OpenAI__ApiKey`)

---

## ?? Use Cases

### **1. Debug JavaScript Errors**
- Capture console errors with stack traces and line numbers
- AI explains root causes with context
- Get fix suggestions with code examples
- Identify anti-patterns and best practices

### **2. Analyze Network Issues**
- Track failed/slow requests with accurate timestamps
- Identify API problems (timeouts, 4xx/5xx errors)
- Optimize network performance
- Detect CORS issues and missing resources

### **3. Performance Optimization**
- Monitor FCP, LCP, memory usage, DOM complexity
- AI suggests performance improvements
- Identify render-blocking resources
- Export performance reports for stakeholders

### **4. Session Recording & Sharing**
- Save debugging sessions with full telemetry
- Replay telemetry data for offline analysis
- Export reports to share with team members
- Archive sessions for compliance and documentation

---

## ?? Contributing

Contributions are welcome! Here's how you can help:

### **Getting Started**

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (if available)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### **Guidelines**

- Follow existing code style (C# conventions)
- Add XML comments to public APIs
- Update documentation for new features
- Test your changes manually
- Keep pull requests focused on a single feature
- Write descriptive commit messages

### **Code Style**

- Use **C# 13** language features (primary constructors, collection expressions)
- Follow **SOLID** principles
- Implement **dependency injection** where appropriate
- Write **async/await** code for I/O operations
- Add **logging** for important operations
- Handle **exceptions** gracefully with proper error messages

---

## ?? License

This project is licensed under the **MIT License** - see the [LICENSE.txt](LICENSE.txt) file for details.

### **What This Means:**

? **You can:**
- Use commercially
- Modify the code
- Distribute copies
- Use privately
- Sublicense

? **You cannot:**
- Hold the author liable
- Use without including the license

---

## ?? Acknowledgments

### **Technologies**
- **[OpenAI](https://openai.com/)** - GPT-4 API for AI analysis
- **[Microsoft](https://microsoft.com/)** - .NET 9, WebView2, C#
- **[LiteDB](https://www.litedb.org/)** - Embedded NoSQL database
- **[Serilog](https://serilog.net/)** - Structured logging

### **Inspiration**
- Chrome DevTools - Browser debugging UI/UX
- Fiddler - Network traffic analysis
- Postman - API testing and debugging
- Browser Stack - Cross-browser testing

### **Community**
- Stack Overflow - Problem solving and troubleshooting
- GitHub - Code inspiration and open source examples
- .NET Community - Best practices and architectural patterns

---

## ?? Support & Resources

### **Documentation**
- **[Architecture Overview](ARCHITECTURE.md)** - System design and data flow
- **[Project Structure](PROJECT_STRUCTURE.md)** - Folder layout and organization
- **[AI Chat Integration](AI_CHAT_INTEGRATION_SPEC.md)** - AI assistant details

### **API Keys**
- **[Get OpenAI API Key](https://platform.openai.com/api-keys)** - Required for AI features

### **WebView2**
- **[WebView2 Documentation](https://docs.microsoft.com/microsoft-edge/webview2/)** - Browser integration
- **[CDP Protocol Reference](https://chromedevtools.github.io/devtools-protocol/)** - DevTools Protocol

### **Issues & Support**
- **[GitHub Issues](https://github.com/DucTriIT/AIDebugPro/issues)** - Report bugs or request features
- **[Discussions](https://github.com/DucTriIT/AIDebugPro/discussions)** - Ask questions and share ideas

---

## ??? Roadmap

### **v1.0 (Current - Completed)** ?
- [x] WebView2 browser integration with full Chromium support
- [x] Chrome DevTools Protocol (CDP) telemetry capture
- [x] OpenAI GPT-4 analysis with context-aware prompts
- [x] Session management and persistence (save/load `.aidp` files)
- [x] Report generation (HTML, Markdown, JSON)
- [x] Dependency injection and structured logging
- [x] User Secrets configuration for API keys
- [x] Console, Network, Performance telemetry capture
- [x] Snapshot-based architecture for historical analysis
- [x] Session import/export with automatic ID generation
- [x] Real-time telemetry display with deduplication
- [x] AI analysis workflow with issue tracking

### **v1.1 (Next - Planned)**
- [ ] Enhanced AI context awareness
  - [ ] Right-click context menu on telemetry items
  - [ ] "Ask AI about this error" quick actions
  - [ ] Quick prompt buttons (Analyze Errors, Check Network, Performance)
  - [ ] Highlight telemetry items referenced by AI
- [ ] Advanced session features
  - [ ] Session search and filtering
  - [ ] Session comparison tool
  - [ ] Automatic snapshots at intervals
- [ ] UI/UX improvements
  - [ ] Settings dialog for configuration
  - [ ] Customizable dashboard layouts
  - [ ] Dark/Light theme support

### **v1.2 (Future)**
- [ ] Unit and integration tests
- [ ] Performance profiling and optimization
- [ ] Enhanced error handling and recovery
- [ ] Database persistence for sessions
- [ ] Session history and management UI

### **v2.0 (Vision)**
- [ ] Local LLM support (Ollama, LM Studio)
- [ ] Screenshot and video capture during debugging
- [ ] Network throttling simulation
- [ ] Custom metrics and assertions
- [ ] Team collaboration features
- [ ] Cloud synchronization
- [ ] Browser extension for remote debugging
- [ ] Multi-browser support (Chrome, Firefox, Safari)
- [ ] Jira/GitHub issue integration
- [ ] Custom AI model fine-tuning

---

## ?? Project Statistics

| Metric | Value |
|--------|-------|
| **Total Files** | 70+ |
| **Lines of Code** | ~12,000 |
| **NuGet Packages** | 13 |
| **Architecture Layers** | 7 |
| **UI Components** | 8 |
| **Data Models** | 20+ |
| **Interfaces** | 12+ |
| **Build Status** | ? Success |
| **Test Coverage** | Coming Soon |

---

## ? Show Your Support

If you find AIDebugPro helpful, please consider:

- ? **Star this repository** on GitHub
- ?? **Report issues** you encounter
- ?? **Suggest features** you'd like to see
- ?? **Contribute** code or documentation
- ?? **Share** with your network

---

## ?? Release Notes

### **v1.0.0** - Initial Release (Current)

**Features:**
- Complete browser debugging with WebView2
- AI-powered telemetry analysis using GPT-4
- Real-time console, network, and performance monitoring
- Session save/load with `.aidp` format
- HTML/Markdown/JSON report export
- Comprehensive logging with Serilog

**Bug Fixes:**
- Fixed network request timestamp issue (was showing 1970)
- Fixed session import error when loading saved sessions
- Fixed export report showing zero statistics
- Fixed AI analysis not saving to session

**Known Issues:**
- Performance tab only shows last 30 seconds (by design)
- AI panel context menu not fully implemented
- Settings dialog is placeholder

---

**Built with ?? using .NET 9 and GPT-4**

**Version:** 1.0.0 | **Status:** Production Ready ?  
**Author:** DucTriIT | **License:** MIT  
**Last Updated:** December 2024