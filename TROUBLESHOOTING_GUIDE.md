# ?? Troubleshooting Guide - Week 1 Issues

**Last Updated:** December 5, 2024

---

## ? **Fixed Issues:**

### **Issue #1: Network Tab Empty** ??
**Status:** ? FIXED

**Problem:**
- Network tab shows no requests even when capturing
- No network logs in log file

**Root Cause:**
- Network requests were being captured but not logged properly
- Hard to debug without visibility

**Fix Applied:**
- Added detailed logging to `NetworkListener.OnRequestWillBeSent()`
- Now logs: `?? NETWORK REQUEST: GET https://example.com (ID: abc123)`

**How to Verify Fix:**
1. Run app, create session, start capture
2. Navigate to any website
3. Check logs: `logs/aidebugpro-YYYYMMDD.txt`
4. Look for: `?? NETWORK REQUEST:` entries
5. Check Network tab - should show requests

**If Still Empty:**
```bash
# Check if CDP session started
grep "CDP session started" logs/aidebugpro-*.txt

# Check if Network.enable was called
grep "Network.enable" logs/aidebugpro-*.txt

# Check for network listener errors
grep "NetworkListener" logs/aidebugpro-*.txt | grep ERR
```

---

### **Issue #2: AI Chatbox Shows URL** ??
**Status:** ? FIXED

**Problem:**
- AI chat shows website URL instead of analysis
- Empty or unhelpful responses
- No error messages

**Root Causes:**
1. OpenAI API key not set
2. Session not created before using AI
3. No telemetry captured yet
4. API errors not surfaced to user

**Fixes Applied:**
1. **Better Validation:**
   ```csharp
   if (!_currentSessionId.HasValue || _currentSessionId == Guid.Empty)
   {
       AddMessageToChat("?? No active session. Please create a session first.", false);
       return;
   }
   ```

2. **Better Error Messages:**
   ```csharp
   if (string.IsNullOrWhiteSpace(response.Message))
   {
       AddMessageToChat("?? AI returned empty response. Check API key.", false);
   }
   ```

3. **API Key Detection:**
   ```csharp
   if (ex.Message.Contains("401") || ex.Message.Contains("API key"))
   {
       AddMessageToChat("?? Tip: Set OpenAI API key in User Secrets", false);
   }
   ```

4. **Enhanced Logging:**
   - Logs context size: `Context built: 5 console, 10 network msgs`
   - Logs query: `Calling AI with query: Why is this error...`
   - Logs errors with full stack trace

**How to Verify Fix:**
1. Ensure API key is set:
   ```bash
   dotnet user-secrets list
   # Should show: OpenAI:ApiKey = sk-proj-...
   ```

2. Create session and start capture
3. Let some telemetry accumulate (errors, network requests)
4. Ask AI a question
5. Check for helpful error messages if something fails

**Common Error Messages:**

| Message | Cause | Fix |
|---------|-------|-----|
| "?? AI Assistant not initialized" | AI services not injected | Restart app |
| "?? No active session" | Session not created | File ? New Session |
| "?? AI returned empty response" | API key issue | Check User Secrets |
| "? Error: 401 Unauthorized" | Invalid API key | Update API key |
| "? Error: Insufficient quota" | OpenAI account issue | Add credits to OpenAI account |

---

### **Issue #3: Menu Bar Overlay** ??
**Status:** ? FIXED

**Problem:**
- Menu bar (File/Tools/View/Help) overlays browser and chat
- Can't see full chat box or website
- Controls not properly positioned

**Root Cause:**
- Controls added to form in wrong order
- `MainSplitContainer` added AFTER menu strip
- Z-order issue in Windows Forms

**Fix Applied:**
Changed order in `InitializeCustomComponents()`:

**Before (Wrong):**
```csharp
CreateMenuStrip();           // Added first
_commandToolbar = ...;       // Added second
mainSplitContainer = ...;    // Added last (gets covered!)
```

**After (Correct):**
```csharp
mainSplitContainer = ...;    // Added FIRST (bottom layer)
this.Controls.Add(mainSplitContainer);

_commandToolbar = ...;       // Added second (middle layer)
this.Controls.Add(_commandToolbar);

CreateMenuStrip();           // Added LAST (top layer)
```

**How to Verify Fix:**
1. Run app
2. Menu bar should be at very top
3. Toolbar below menu bar
4. Browser and chat should NOT be covered
5. You should see full website in left panel
6. You should see full AI chat in right panel

---

## ?? **Testing Checklist:**

After fixing these issues, test:

### **Test 1: Network Tab Works**
- [ ] Create session
- [ ] Start capture
- [ ] Navigate to `https://example.com`
- [ ] Check logs for `?? NETWORK REQUEST:`
- [ ] Go to Logs Dashboard ? Network tab
- [ ] Verify at least 5-10 requests visible

### **Test 2: AI Chat Works**
- [ ] Create session
- [ ] Start capture
- [ ] Navigate to site with errors
- [ ] Let telemetry accumulate (10+ seconds)
- [ ] Go to AI panel
- [ ] Type: "What errors do I have?"
- [ ] Click Send
- [ ] Verify AI responds (not just URL)

### **Test 3: Menu Not Overlaying**
- [ ] Run app
- [ ] Check menu bar at top (not covering anything)
- [ ] Check toolbar below menu
- [ ] Check browser visible in left panel
- [ ] Check AI chat visible in right panel
- [ ] No overlap or covering

---

## ?? **Log Analysis:**

### **Healthy Logs Look Like:**

```log
[INF] Starting AIDebugPro application...
[INF] MainForm initialized
[INF] MainForm loading...
[INF] MainForm loaded successfully
[INF] Created new session: "abc123..."
[INF] WebView2Host created
[INF] CDP session started for session "abc123..."
[INF] ? NetworkListener STARTED successfully
[INF] ? ConsoleListener STARTED successfully
[INF] ? PerformanceCollector STARTED successfully
[INF] Capture started for session "abc123..."
[INF] ?? NETWORK REQUEST: GET https://example.com
[INF] ?? NETWORK REQUEST: GET https://example.com/style.css
[INF] ?? Analyzing user query with telemetry context
[DBG] Context built: 5 console, 10 network msgs
[INF] ? AI analysis complete: High severity, 2 recommended fixes
```

### **Problem Logs Look Like:**

```log
[ERR] ? Error during AI analysis
System.ArgumentNullException: Value cannot be null. (Parameter 'apiKey')

[WRN] ?? NO NETWORK REQUESTS found in last 30 seconds

[ERR] Error starting capture
AIDebugPro.Core.Exceptions.WebView2InitializationException: WebView2 initialization failed
```

---

## ?? **Debugging Commands:**

### **Check Network Capture:**
```bash
# See if network listener started
grep "NetworkListener STARTED" logs/aidebugpro-*.txt

# Count network requests captured
grep "?? NETWORK REQUEST:" logs/aidebugpro-*.txt | wc -l

# See latest network request
grep "?? NETWORK REQUEST:" logs/aidebugpro-*.txt | tail -1
```

### **Check AI Chat:**
```bash
# See AI queries
grep "?? Analyzing user query" logs/aidebugpro-*.txt

# Check context size
grep "Context built:" logs/aidebugpro-*.txt

# See AI errors
grep "AI analysis" logs/aidebugpro-*.txt | grep ERR
```

### **Check API Key:**
```bash
# Verify API key is set
dotnet user-secrets list

# Check for API key errors in logs
grep "401" logs/aidebugpro-*.txt
grep "API key" logs/aidebugpro-*.txt
```

---

## ?? **Quick Fixes:**

### **If Network Tab Still Empty:**

1. **Check CDP Session:**
   ```bash
   grep "CDP session started" logs/aidebugpro-*.txt
   ```
   If missing, WebView2Host isn't starting correctly.

2. **Restart Capture:**
   - Stop capture
   - Wait 2 seconds
   - Start capture again
   - Navigate to new page

3. **Clear Browser Cache:**
   - Some cached resources might not trigger network events
   - Navigate to a fresh URL

### **If AI Chat Not Working:**

1. **Verify API Key:**
   ```bash
   dotnet user-secrets list
   # Should output: OpenAI:ApiKey = sk-proj-...
   ```

2. **Test API Key:**
   ```bash
   curl https://api.openai.com/v1/models \
     -H "Authorization: Bearer YOUR_API_KEY"
   ```

3. **Check Quota:**
   - Go to: https://platform.openai.com/account/usage
   - Ensure you have credits

4. **Check Session State:**
   - Make sure session is created (File ? New Session)
   - Make sure capture is started (Click ? Start Capture)
   - Let telemetry accumulate (wait 10+ seconds)

### **If Menu Still Overlaying:**

1. **Restart App:**
   - Sometimes Z-order is cached
   - Close and reopen

2. **Check Code Order:**
   - Verify `InitializeCustomComponents()` order is correct
   - Should be: split container ? toolbar ? menu ? status

---

## ?? **Next Steps:**

After verifying all fixes work:

1. ? Run all 7 test scenarios from [WEEK1_TESTING_GUIDE.md](WEEK1_TESTING_GUIDE.md)
2. ? Take screenshots of working features
3. ? Record demo video
4. ? Update README with examples
5. ? Push to GitHub

---

## ?? **Success Criteria:**

You know fixes are working when:

- ? Network tab shows 10+ requests after browsing
- ? AI chat gives helpful analysis (not just URL)
- ? Menu bar doesn't cover browser or chat
- ? No errors in logs during normal operation
- ? All test scenarios pass

---

**Status:** All 3 issues FIXED in commit 271df35  
**Next:** Test thoroughly and continue to Week 2!
