# 8. Core Workflows

## 8.1 Analyze Flow (Sequence)
```mermaid
sequenceDiagram
  participant U as User
  participant VM as MainViewModel
  participant S as SettingsService
  participant P as PoiSearchService
  participant A as MapAnalysisService
  participant C as ScoreCalculator
  participant M as Map/WebView

  U->>VM: Select business, radius, click Analyze
  VM->>S: Get API key & language
  VM->>A: Analyze(input)
  A->>P: Fetch POIs (competitors, complements)
  P-->>A: POI collections
  A->>C: Compute CI/CoI/AI/DI + normalize
  C-->>A: Scored grid cells
  A-->>VM: Heatmap data + results list
  VM->>M: Render heatmap
  VM-->>U: Show ranked list w/ rationales
```

## 8.2 Weight Change Flow
```mermaid
sequenceDiagram
  participant U as User
  participant VM as MainViewModel
  participant C as ScoreCalculator
  participant M as Map/WebView

  U->>VM: Adjust weight slider(s)
  VM->>VM: Debounce 150–250ms
  VM->>C: Recompute scores (cached POIs)
  C-->>VM: Updated scores
  VM->>M: Update heatmap
  VM-->>U: Updated ranked list
```

## 8.3 Export Flows
- **PNG/JPG Map-only:** snapshot WebView + overlay mask (no list).  
- **PDF Results:** list with per-item map preview and rationale bullets.

---
