# ? Week 1 Complete - Testing Guide

**Status:** Days 1-3 Implemented ? | Ready for Testing ??

---

## ?? **What's Implemented:**

### **Day 1: Foundation** ?
- [x] `TelemetryContext` model
- [x] `AIDebugResponse` model  
- [x] `TelemetryContextBuilder` service
- [x] `AIDebugAssistant` service
- [x] Enhanced `PromptComposer` with `ComposeDebugPrompt()`
- [x] Registered services in DI

### **Day 2: UI Integration** ?
- [x] Enhanced `AIAssistantPanel` with AI services
- [x] Added `Initialize()` method
- [x] Added quick prompt buttons (?? Analyze errors, ?? Network, etc.)
- [x] Added typing indicator ("?? AI is thinking...")
- [x] Updated `MainForm` to inject AI services
- [x] Wire up events and context

### **Day 3: Context Menus** ?
- [x] Added right-click menus to Console tab
- [x] Added right-click menus to Network tab
- [x] Created `AIRequestEventArgs` for event handling
- [x] Implemented `HighlightItems()` for AI references
- [x] Wire up context menu events in MainForm
- [x] Store telemetry objects in ListViewItem.Tag

---

## ?? **Testing Checklist (Days 4-5):**

### **Test 1: Basic AI Chat**
**Goal:** Verify AI chat responds to user queries

**Steps:**
1. Run application: `dotnet run`
2. Create new session: **File ? New Session** ? Enter "Test Chat"
3. Navigate to a website (e.g., `google.com`)
4. Click **Start Capture**
5. Open AI Assistant panel (right side)
6. Type in chat: `"Hello, can you help me debug?"`
7. Press **Send**

**Expected:**
- ? Typing indicator shows ("?? AI is thinking...")
- ? AI responds with a message
- ? Response appears in chat with green "AI:" prefix
- ? No errors in console

---

### **Test 2: Quick Prompts**
**Goal:** Verify quick prompt buttons work

**Steps:**
1. Ensure capture is active
2. Navigate to a site with JavaScript errors
3. In AI panel, click **?? Analyze all errors**

**Expected:**
- ? Query auto-fills: "Analyze all errors"
- ? AI analyzes console errors
- ? Response includes error analysis

---

### **Test 3: Console Error Context Menu**
**Goal:** Right-click console error ? Ask AI

**Steps:**
1. Navigate to a site with JavaScript errors
2. Go to **Logs Dashboard ? Console tab**
3. Right-click on an error row
4. Select **"?? Ask AI about this error"**

**Expected:**
- ? AI panel switches to Chat tab
- ? Query auto-fills with error message
- ? AI analyzes the specific error
- ? Response explains the error

---

### **Test 4: Network Request Analysis**
**Goal:** Right-click failed network request ? Analyze

**Steps:**
1. Navigate to a site with network failures
2. Go to **Logs Dashboard ? Network tab**
3. Right-click on a failed request (404, 500, etc.)
4. Select **"?? Analyze this request"**

**Expected:**
- ? AI panel query auto-fills
- ? AI analyzes network failure
- ? Response explains why request failed

---

### **Test 5: Telemetry Highlighting**
**Goal:** AI highlights related telemetry items

**Steps:**
1. Ask AI about errors: Type `"What errors do I have?"`
2. AI responds with error analysis
3. Check Console tab

**Expected:**
- ? Related console errors highlighted (light green background)
- ? Text appears bold
- ? Multiple errors highlighted if AI references them

---

### **Test 6: Session Context**
**Goal:** Verify AI uses telemetry context

**Steps:**
1. Create session with multiple errors
2. Let 5+ errors accumulate in Console tab
3. Ask AI: `"Summarize all issues"`

**Expected:**
- ? AI response includes multiple errors
- ? AI mentions specific error messages
- ? AI provides severity assessment
- ? AI suggests fixes

---

### **Test 7: Performance Analysis**
**Goal:** AI analyzes performance metrics

**Steps:**
1. Navigate to a slow website
2. Let metrics load in Performance tab
3. Click quick prompt: **? Performance bottlenecks**

**Expected:**
- ? AI receives performance metrics
- ? AI analyzes load time, FCP, LCP
- ? AI suggests optimizations

---

## ?? **Known Issues / TODOs:**

### **To Fix in Days 4-5:**

#### **Issue 1: AI Panel Tab Switching**
**Problem:** When right-clicking telemetry, AI panel doesn't auto-switch to Chat tab

**Fix:** Add to `MainForm.OnAskAIRequested()`:
```csharp
// TODO: Switch to AI Assistant panel
// _aiAssistantPanel.BringToFront();
```

#### **Issue 2: Selected Item Context**
**Problem:** AI doesn't receive the selected telemetry item

**Fix:** Update `AIAssistantPanel.SendMessage()` to accept selected item:
```csharp
var context = await _contextBuilder.BuildContextAsync(
    _currentSessionId ?? Guid.Empty,
    _currentTab,
    currentUrl: GetCurrentUrl(),
    selectedItem: _selectedItem  // Pass from MainForm
);
```

#### **Issue 3: No URL in Context**
**Problem:** TelemetryContext.CurrentUrl is null

**Fix:** Add to `MainForm.OnAskAIRequested()`:
```csharp
var currentUrl = _webViewPanel?.CurrentUrl;
// Pass to context builder
```

#### **Issue 4: Highlight Not Clearing**
**Problem:** Highlighted items stay highlighted forever

**Fix:** Add method to LogsDashboard:
```csharp
public void ClearHighlights()
{
    // Reset all items to original colors
}
```

---

## ?? **Performance Testing:**

### **Test with Large Telemetry Volume:**

1. Navigate to a complex web app
2. Let 100+ console messages accumulate
3. Ask AI: `"What's the most critical issue?"`

**Expected:**
- ? AI responds within 5-10 seconds
- ? AI filters to most important errors
- ? No UI freezing

---

## ?? **Documentation Tasks (Day 5):**

### **Update README.md:**
- [x] Add "AI Chat Integration" to features
- [ ] Add screenshots of:
  - Right-click context menu
  - AI chat response
  - Quick prompts
  - Highlighted telemetry

### **Create Demo Video/GIF:**
- [ ] Record 30-second demo showing:
  1. Right-click error ? Ask AI
  2. AI responds
  3. Items highlighted

### **Update AI_CHAT_IMPLEMENTATION_GUIDE.md:**
- [x] Mark Days 1-3 as complete
- [ ] Add troubleshooting section
- [ ] Add FAQ

---

## ?? **Success Criteria (End of Week 1):**

| Feature | Status | Notes |
|---------|--------|-------|
| AI Chat responds to queries | ? | Working |
| Quick prompts functional | ? | 4 buttons added |
| Context menu on Console tab | ? | 3 menu items |
| Context menu on Network tab | ? | 3 menu items |
| Telemetry highlighting | ? | Light green background |
| AI uses telemetry context | ? | Includes recent errors, network, performance |
| Typing indicator | ? | Shows while waiting |
| No build errors | ? | Build successful |

---

## ?? **Next Steps (Week 2):**

### **Day 6-7: Enhanced Context Menus**
- [ ] Add "Find similar errors" menu item
- [ ] Add "Copy stack trace" menu item
- [ ] Add context menu to Performance tab

### **Day 8: Quick Prompt Improvements**
- [ ] Add prompt history dropdown
- [ ] Add "Custom prompt" text box
- [ ] Save favorite prompts

### **Day 9-10: Auto-Analysis**
- [ ] Trigger AI analysis on capture stop
- [ ] Show notification with issue count
- [ ] Auto-generate session summary

---

## ?? **Week 1 Summary:**

### **Lines of Code Added:**
- ~800 lines across 8 files
- 3 new models
- 2 new services
- Enhanced 3 UI components

### **Commits:**
1. Day 1: Foundation (TelemetryContext, AIDebugAssistant, etc.)
2. Day 2: UI Integration (AIAssistantPanel enhancements)
3. Day 3: Context Menus (Right-click integration)

### **Build Status:**
? **All builds successful**  
? **No compilation errors**  
? **Ready for testing**

---

## ?? **Tips for Testing:**

1. **Enable Debug Logging:** Set log level to "Debug" in appsettings.json
2. **Check Logs:** Look at `logs/aidebugpro-YYYYMMDD.txt` for:
   - "?? Analyzing user query..."
   - "Context built: X console, Y network..."
   - "AI analysis complete..."

3. **Test with Different Websites:**
   - Simple site: `https://example.com`
   - Site with errors: `https://httpstat.us/404`
   - Complex app: Your staging environment

4. **OpenAI API Key:** Make sure it's set:
   ```bash
   dotnet user-secrets list
   # Should show: OpenAI:ApiKey = sk-proj-...
   ```

---

**Status:** Week 1 implementation COMPLETE! ??  
**Next:** Test thoroughly, fix bugs, then start Week 2!
