# 11. Error Handling & Observability

- **Error Model:** Fail fast on missing API key; user-friendly prompts.  
- **Network Errors:** Retry up to 2 times w/ exponential backoff; show toast on failure.  
- **Timeouts:** 8–12s per REST call with cancellation support.  
- **Logging:** `ILogger<T>`; for production, optional AppCenter/AppInsights for crash/telemetry.  
- **Sensitive Data:** Never log API key or precise personal data.

---
