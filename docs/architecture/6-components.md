# 6. Components

## 6.1 Services
- **SettingsService**: API key, language, last-used weights/radius (`SecureStorage` + `Preferences`).  
- **LocalizationService**: RESX resource manager; sets `CultureInfo("sr-Latn")` default; supports `"en"`.  
- **PoiSearchService**: Wraps Azure Maps Search REST (category search). Adds `language=sr-Latn` when Serbian active.  
- **MapAnalysisService**: Orchestrates POI fetch per category, creates sampling grid/hex within radius, transforms to features, requests minimal data; hands off to scoring.  
- **ScoreCalculator**: Pure methods for CI/CoI/AI/DI + normalization and final score. Distance-decay kernels: `exp(-d/300)` for competitors, `exp(-d/200)` for complements.  
- **ExportService**: 
  - **PNG/JPG** map overlay export: capture current WebView frame + optional overlay using MAUI Graphics (or inject a JS helper for DOM-to-canvas inside the map; fallback to native snapshot).  
  - **PDF**: table of results + mini-map thumbnails (request static image tiles for each location or render local snapshot), rationale bullets, metadata, timestamp.  

## 6.2 ViewModels (key)
- **MainViewModel**: Inputs (business type, radius, weights), Analyze command, bindable heatmap/list.  
- **SettingsViewModel**: API key (masked), Language toggle, Test Connection command.  
- **ResultsViewModel**: Wraps `ObservableCollection<ResultItem>`, handles item selection → centers map.

## 6.3 Views (XAML Pages)
- **StartupPage** (API key prompt if missing)  
- **MainPage** (Map + inputs panel + advanced sliders + results list)  
- **SettingsPage** (API key mgmt, language)

---
