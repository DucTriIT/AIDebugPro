# AIDebugPro - AI-Powered Browser Debugging Assistant

[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE.txt)
[![OpenAI](https://img.shields.io/badge/AI-GPT--4-orange)](https://openai.com/)

**AIDebugPro** is an enterprise-grade browser debugging tool that uses **OpenAI GPT-4** to analyze web application telemetry and provide intelligent debugging insights. Capture console errors, network requests, performance metrics, and DOM snapshots in real-time, then let AI identify issues and recommend fixes.

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
4. **Browse & Interact:** Use the website to generate telemetry
5. **Analyze with AI:** Click **?? Analyze with AI** button
6. **Review Results:** Check the AI Assistant panel for insights
7. **Export Report:** Click **?? Export Report** ? Choose format (HTML/Markdown/JSON)

---

## ? Key Features

### ?? **Browser Integration**
- **WebView2** - Full Chromium-based browser
- **Chrome DevTools Protocol (CDP)** - Low-level browser instrumentation
- Real-time console message capture
- Network request/response tracking
- Performance metrics (FCP, LCP, memory, CPU)
- DOM snapshot capture

### ?? **AI-Powered Analysis**
- **GPT-4 Integration** - Intelligent error analysis
- Automatic issue categorization (JavaScript, Network, Performance, Security)
- Root cause analysis with explanations
- Actionable fix recommendations
- Code examples and best practices

### ?? **Telemetry Dashboard**
- **Console Tab** - JavaScript errors, warnings, logs (color-coded)
- **Network Tab** - HTTP requests with status, duration, size
- **Performance Tab** - Load times, paint metrics, memory usage
- Real-time updates with automatic buffering

### ?? **Session Management**
- Create, save, and load debug sessions
- Session statistics (errors, warnings, network failures)
- Export reports in multiple formats (HTML, Markdown, JSON)
- Archive sessions for future reference

### ?? **Security & Privacy**
- **User Secrets** for API key storage (development)
- PII redaction in telemetry data
- Configurable data retention
- Local-first architecture

---

## ?? Documentation

- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Complete folder structure
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Architecture overview & data flow
- **[PROJECT_STATUS_AND_NEXT_STEPS.md](PROJECT_STATUS_AND_NEXT_STEPS.md)** - Current status & action plan
- **[QUICK_FIX_GUIDE.md](QUICK_FIX_GUIDE.md)** - Fix build errors (5 min)
- **[SERVICES_LAYER_COMPLETE.md](SERVICES_LAYER_COMPLETE.md)** - DI, Logging, Config
- **[PERSISTENCE_LAYER_COMPLETE.md](PERSISTENCE_LAYER_COMPLETE.md)** - Database & Repositories
- **[BROWSERINTEGRATION_LAYER_COMPLETE.md](BROWSERINTEGRATION_LAYER_COMPLETE.md)** - WebView2 & CDP
- **[UTILITIES_COMPLETE.md](UTILITIES_COMPLETE.md)** - Helper classes & extensions

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
   - Verify website loads

3. **Test Console Capture:**
   - Navigate to: `https://example.com`
   - Open browser DevTools (F12)
   - Type in console: `console.error("Test error")`
   - Check **Logs Dashboard ? Console tab** for the error

4. **Test Network Capture:**
   - Navigate to: `https://jsonplaceholder.typicode.com/posts/1`
   - Check **Logs Dashboard ? Network tab** for HTTP requests

5. **Test Performance Metrics:**
   - Navigate to any website
   - Check **Logs Dashboard ? Performance tab** for metrics

6. **Test AI Analysis:**
   - Create a session
   - Capture telemetry
   - Click **?? Analyze with AI**
   - Review results in AI Assistant panel

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
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key-here"

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
- Capture console errors with stack traces
- AI explains root causes and context
- Get fix suggestions with code examples
- Identify anti-patterns and best practices

### **2. Analyze Network Issues**
- Track failed/slow requests
- Identify API problems (timeouts, 4xx/5xx errors)
- Optimize network performance
- Detect CORS issues

### **3. Performance Optimization**
- Monitor FCP, LCP, memory usage, DOM complexity
- AI suggests performance improvements
- Identify render-blocking resources
- Export performance reports

### **4. Session Recording & Sharing**
- Save debugging sessions with full telemetry
- Replay telemetry data for analysis
- Export reports to share with team
- Archive sessions for compliance

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

### **Code Style**

- Use **C# 13** language features
- Follow **SOLID** principles
- Implement **dependency injection** where appropriate
- Write **async/await** code for I/O operations
- Add **logging** for important operations
- Handle **exceptions** gracefully

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
- Chrome DevTools
- Fiddler
- Postman
- Browser Stack

### **Community**
- Stack Overflow - Problem solving
- GitHub - Code inspiration
- .NET Community - Best practices

---

## ?? Support & Resources

### **Documentation**
- **[Architecture Overview](ARCHITECTURE.md)** - System design and data flow
- **[Project Structure](PROJECT_STRUCTURE.md)** - Folder layout and organization
- **[Quick Start Guide](QUICK_START.md)** - Detailed setup instructions

### **API Keys**
- **[Get OpenAI API Key](https://platform.openai.com/api-keys)** - Required for AI features

### **WebView2**
- **[WebView2 Documentation](https://docs.microsoft.com/microsoft-edge/webview2/)** - Browser integration
- **[CDP Protocol Reference](https://chromedevtools.github.io/devtools-protocol/)** - DevTools Protocol

### **Issues & Support**
- **[GitHub Issues](https://github.com/DucTriIT/AIDebugPro/issues)** - Report bugs or request features
- **[Discussions](https://github.com/DucTriIT/AIDebugPro/discussions)** - Ask questions

---

## ??? Roadmap

### **v1.0 (Current)** ?
- [x] WebView2 browser integration
- [x] Chrome DevTools Protocol (CDP) telemetry capture
- [x] OpenAI GPT-4 analysis
- [x] Session management and persistence
- [x] Report generation (HTML, Markdown, JSON)
- [x] Dependency injection and logging
- [x] User Secrets configuration
- [x] Console, Network, Performance telemetry capture

### **v1.1 (In Progress)** ? **CRITICAL**
**Focus: Context-Aware AI Debugging**

**Week 1: Core AI Integration**
- [ ] ? `AIDebugAssistant.cs` - Context-aware AI analysis service
- [ ] ? `TelemetryContextBuilder.cs` - Gather telemetry context for AI
- [ ] ? Enhanced `AIAssistantPanel` - Interactive chat with telemetry
- [ ] AI responds to user queries about errors, network, performance
- [ ] Telemetry-specific prompt templates

**Week 2: User Experience**
- [ ] Right-click context menus on Console/Network/Performance tabs
- [ ] "Ask AI about this error" quick actions
- [ ] Quick prompt buttons (Analyze Errors, Check Network, Performance)
- [ ] Highlight telemetry items when AI references them
- [ ] Selection tracking across all telemetry tabs

**Week 3: Advanced Features**
- [ ] Auto-analysis on capture stop
- [ ] Issue tracking and resolution
- [ ] AI-identified issue persistence
- [ ] Notification system for critical issues
- [ ] End-to-end testing and documentation

**See:** [AI_CHAT_INTEGRATION_SPEC.md](AI_CHAT_INTEGRATION_SPEC.md) for detailed specification

### **v1.2 (Next)**
- [ ] Unit and integration tests
- [ ] Performance profiling and optimization
- [ ] Enhanced error handling
- [ ] User settings UI
- [ ] Session search and filtering

### **v2.0 (Future)**
- [ ] Local LLM support (Ollama, LM Studio)
- [ ] Screenshot and video capture
- [ ] Network throttling simulation
- [ ] Custom metrics and assertions
- [ ] Team collaboration features
- [ ] Cloud synchronization
- [ ] Browser extension
- [ ] Multi-browser support
- [ ] Jira/GitHub issue integration
- [ ] Custom AI model fine-tuning

---

## ?? Project Statistics

| Metric | Value |
|--------|-------|
| **Total Files** | 60+ |
| **Lines of Code** | ~10,500 |
| **NuGet Packages** | 13 |
| **Architecture Layers** | 7 |
| **UI Components** | 8 |
| **Data Models** | 19 |
| **Interfaces** | 10+ |
| **Build Status** | ? Success |

---

## ?? Show Your Support

If you find AIDebugPro helpful, please consider:

- ? **Star this repository** on GitHub
- ?? **Report issues** you encounter
- ?? **Suggest features** you'd like to see
- ?? **Contribute** code or documentation
- ?? **Share** with your network

---

**Built with ?? using .NET 9 and GPT-4**

**Version:** 1.0.0 | **Status:** Production Ready ??