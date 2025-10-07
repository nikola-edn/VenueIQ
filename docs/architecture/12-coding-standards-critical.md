# 12. Coding Standards (Critical)

- **Never** hardcode secrets; always use `SecureStorage` for API key.  
- All user-facing strings go through RESX.  
- `ScoreCalculator` must be **pure** and covered by unit tests.  
- Data fetch → compute → render boundaries respected (no business logic in Views).  
- Use `ObservableCollection` only on UI thread.  
- Always debounce weight changes; avoid repeated network calls after first fetch.

---
