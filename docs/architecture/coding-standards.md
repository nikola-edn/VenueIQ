# Coding Standards

Canonical source: [12-coding-standards-critical.md](./12-coding-standards-critical.md)

- Never hardcode secrets; always use `SecureStorage` for API key.
- All user-facing strings go through RESX.
- `ScoreCalculator` must be pure and covered by unit tests.
- Data fetch → compute → render boundaries respected (no business logic in Views).
- Use `ObservableCollection` only on UI thread.
- Always debounce weight changes; avoid repeated network calls after first fetch.

