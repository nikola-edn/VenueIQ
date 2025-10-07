# 13. Test Strategy

> Environment Constraint: WSL + MAUI
>
> In this workspace (WSL), .NET MAUI projects cannot be built, run, or UI-tested even though `dotnet` is installed. Automated test execution is expected to occur on a proper MAUI host (Windows/macOS) or CI configured for MAUI. Local contributors and agents should author tests and code, but avoid running them here.

## 13.1 Unit Tests
- `ScoreCalculatorTests`: CI/CoI/AI/DI normalization, weight application, edge distances.  
- `PoiSearchServiceTests`: parameter building, language flag, error handling (mock HTTP).  
- `SettingsServiceTests`: secure storage get/set; culture switching.

## 13.2 Integration Tests
- Mock Azure Maps via WireMock/FlurlHttpTest; validate end-to-end `Analyze(input)` produces expected ranked order for fixed POIs.

## 13.3 E2E (optional)
- UI automation (Appium/MAUI UITest) for: API key flow, analyze flow, slider updates, exports.

**Coverage Targets:** Unit ≥ 75%, critical logic ≥ 90%.

---
