# 2. High Level Architecture

## 2.1 Technical Summary
- **Architecture Style:** Client-first mobile/desktop app with direct integration to Azure Maps REST + Web control (WebView).  
- **Key Components:** View Layer (XAML pages), ViewModels (MVVM), Services (`MapAnalysisService`, `PoiSearchService`, `ScoreCalculator`, `ExportService`, `LocalizationService`, `SettingsService`), Infra (SecureStorage, Preferences).  
- **Core Patterns:** MVVM, Dependency Injection, immutable DTOs, async/await with cancellation, debounced computations.  
- **Primary Tech:** .net 9 MAUI, Azure Maps REST (Search), Azure Maps Web Control heatmap, QuestPDF/Syncfusion for PDF, MAUI Graphics for PNG/JPG.  
- **Bilingual UX:** Serbian Latin default (`sr-Latn`), English toggle; RESX resources and culture switching.

## 2.2 High Level Overview
- **Monorepo/Repo:** Single app repository (monoproject).  
- **Flow:** User enters inputs → app fetches POIs from Azure Maps → samples grid/hex points → computes CI/CoI/AI/DI → renders heatmap and ranked list → optional export (image/PDF).  
- **Decisions:** Client-only for MVP; no server latency; caching in-memory; live weight sliders drive recomputation without additional network calls when possible.

## 2.3 High Level Project Diagram (Mermaid)
```mermaid
graph TD
  U[User] -->|Inputs| VM[MainViewModel]
  VM --> SET[SettingsService (SecureStorage/Preferences)]
  VM --> L10N[LocalizationService (RESX)]
  VM --> SVC[MapAnalysisService]
  SVC --> POI[PoiSearchService]
  POI -->|REST| AZ[Azure Maps Search API]
  SVC --> SCORE[ScoreCalculator]
  SCORE --> HEAT[HeatmapData]
  VM --> MAP[Azure Maps Web Control (WebView)]
  VM --> LIST[Results List]
  VM --> EXP[ExportService (PNG/JPG/PDF)]
  HEAT --> MAP
  HEAT --> LIST
```

---
