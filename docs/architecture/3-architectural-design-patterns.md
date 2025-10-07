# 3. Architectural & Design Patterns

- **MVVM** for UI separation and testability.  
- **Dependency Injection** via `MauiProgram` (`IServiceCollection`).  
- **Repository/Service** abstraction for POI access (swappable if Google Places or server is added later).  
- **Normalization & Scoring** in `ScoreCalculator` (pure functions; unit-testable).  
- **Debounce + CancellationTokens** for repeated compute during slider changes.  
- **Resilience:** Retry w/ backoff on transient network failures (simple).

---
