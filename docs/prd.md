# Product Requirements Document (PRD)
## Project: VenueIQ

### 1. Overview
**VenueIQ** is a bilingual (.NET MAUI) **mobile + desktop** application for identifying optimal business locations in Serbia using **Azure Maps API**. The app visualizes competitive and complementary businesses on a map and provides a ranked list of recommended locations with **explainable scoring**, **advanced weighting sliders**, and **export options**.

### 2. Goals and Background Context
Entrepreneurs in Serbia typically select locations based on instinct or limited research. VenueIQ fills this gap by offering a data‑driven tool that analyzes competition, accessibility, and demand via Azure Maps.

**Business Goal:** Deliver a simple yet professional‑grade tool to analyze potential business locations for small‑business owners, franchise managers and real‑estate professionals.

**Success Metrics**
- Generate first heatmap within **≤ 10 seconds** of input
- Achieve **1,000 monthly analyses** within first quarter
- **90%** user comprehension of scoring rationale
- **70%** user retention after **3** completed analyses

---

### 3. Requirements

#### 3.1 Functional Requirements (FR)
- **FR1:** Allow user to input **Azure Maps API key** (with secure local storage)
- **FR2:** Display **Azure Maps** interactive map supporting **heatmap overlay**
- **FR3:** Business type selector (dropdown mapped to Azure POI categories)
- **FR4:** Analysis radius selector (**0.5–5 km**)
- **FR5:** Analyze selected area and compute competition & opportunity scores
- **FR6:** Visualize results on map as **heatmap** and as a **ranked list** with scores
- **FR7:** Provide **detailed rationale** for each suggested location (competition, demand, accessibility, complements)
- **FR8:** **Advanced Weighting Sliders** to fine‑tune scoring (live recomputation of heatmap & list)
- **FR9:** **Export Heatmap** → save current map view with overlay only (**PNG/JPG**)
- **FR10:** **Export Results List** → **PDF** containing:
  - Ranked list of top suggested locations
  - Map preview thumbnails for each location
  - Explanation of top scoring factors and nearby competition/complements
- **FR11:** **Bilingual UI** (Serbian Latin default / English toggle)
- **FR12:** Language switching in **Settings**
- **FR13:** Startup API key validation with **Test Connection**
- **FR14:** Persist preferences locally (language, last radius, last weights)

#### 3.2 Non‑Functional Requirements (NFR)
- **NFR1:** All text strings sourced from **RESX** files for localization
- **NFR2:** Use **SecureStorage** for sensitive user data (API key)
- **NFR3:** Heatmap analysis completes within **≤ 10 s** for standard radius (≤ 2 km)
- **NFR4:** App runs on **Android, iOS, Windows, macOS** (.net 9 MAUI)
- **NFR5:** Respect Azure Maps **API quotas** (e.g., ≤ 50,000/day/key)
- **NFR6:** Follow **MVVM** pattern; clear separation of services; testable components
- **NFR7:** Data accuracy depends solely on **Azure Maps** data integrity

#### 3.3 Compatibility Requirements (CR)
- **CR1:** Works with individual **Azure Maps keys** obtained by users
- **CR2:** Use `language=sr-Latn` for localized POI names when UI is Serbian
- **CR3:** UI consistent across platforms (responsive layouts for mobile/desktop)

---

### 4. User Interface Design Goals
**Primary Layout:** Main map view with side‑panel list.

- **Top Bar:** Business type selector, Radius selector, **Analyze** button
- **Collapsible Panel (Advanced):** Weight sliders for Competition, Demand, Accessibility, Complements (with “Auto‑balance” reset)
- **Results List:** Top‑10 suggested locations, score, badges (e.g., competitors within 300 m, complements within 250 m, nearest transit distance). Clicking a row **recenters the map** and toggles pins.
- **Settings:** API key management, Language toggle (Srpski / English), Export options

**Core Screens**
1. **Startup / API Key Entry**
2. **Main Analysis Screen** (map + controls + advanced weights)
3. **Results List & Details** (clickable; toggles competitors/complements)
4. **Export flows** (Map PNG/JPG; PDF report)
5. **Settings / Preferences**

**Localization**
- Default UI language: **Serbian Latin (sr‑Latn)**
- English available via Settings or system‑language detection
- All strings in `Resources/Strings/AppResources.resx` and `AppResources.sr‑Latn.resx`

---

### 5. Technical Assumptions
- **Platform:** .net 9 MAUI (single project; MVVM)
- **Mapping:** Azure Maps Web control (via WebView) + REST Search APIs (POI/category)
- **Scoring formula (initial default):**
  ```
  score = 0.35*complements_index 
        + 0.25*accessibility_index
        + 0.25*demand_index
        - 0.35*competition_index
  ```
- **Indexes:**
  - **Competition (CI):** distance‑decay kernel density on competitor POIs (normalized)
  - **Complements (CoI):** weighted proximity to complementary POIs (normalized)
  - **Accessibility (AI):** inverted min distance to transit/parking (normalized)
  - **Demand (DI):** density of residential/office/school POIs (normalized)
- **Exports:** MAUI Graphics (map snapshot); **QuestPDF** or **Syncfusion.Pdf** (results PDF)

---

### Development Environment Constraint
> This repository is currently edited within WSL. While `dotnet` is available, .NET MAUI apps cannot be built, run, or UI-tested in this environment. Implementation agents should focus on writing code and updating documentation only. The maintainer will execute builds and manual testing on a supported MAUI host (Windows/macOS with required workloads) and provide feedback.

### 6. Epics and Stories

#### Epic 1: Application Setup & Configuration
- **Story 1.1:** Startup flow for API key entry, secure storage, and “Test connection”
- **Story 1.2:** Preferences for language, last radius, last weights
- **Story 1.3:** Bilingual resources & language switcher (Serbian default)

#### Epic 2: Core Analysis Engine
- **Story 2.1:** Map control wrapper + base layers; heatmap layer placeholder
- **Story 2.2:** POI fetching for business & complementary categories (Azure Maps Search)
- **Story 2.3:** Grid/hex sampling within radius; compute CI, CoI, AI, DI; normalization
- **Story 2.4:** Render heatmap intensity per sample cell
- **Story 2.5:** Ranked list generation with explainable rationale

#### Epic 3: Advanced Interactivity
- **Story 3.1:** Advanced weighting sliders (four weights + reset to defaults)
- **Story 3.2:** Live recomputation of scores/heatmap on weight change (debounced)

#### Epic 4: Export & Sharing
- **Story 4.1:** Export map with heatmap overlay as PNG/JPG (current viewport)
- **Story 4.2:** Export results list as PDF (rank, score, map preview, rationale)

#### Epic 5: UX Quality & Explainability
- **Story 5.1:** Factor badges (competition, complements, transit, demand)
- **Story 5.2:** Tooltip with distances and top contributing POIs
- **Story 5.3:** Serbian‑first labels; ensure no hard‑coded strings

---

### 7. Technical Risks
- Azure Maps POI coverage variability in some regions
- Performance on mobile for dense grids (sampling size/step tuning required)
- API usage costs for high‑volume users
- User trust in algorithm if rationale is too technical or noisy

**Mitigations**
- Start with well‑covered categories; allow manual category overrides
- Debounce, cache POIs, tune sampling resolution (adaptive by zoom)
- Provide clear “top factors” explanations; clamp and round distances

---

### 8. Success Metrics
| Metric | Target |
|---|---|
| Average analysis time | ≤ 10 s |
| Monthly successful analyses | ≥ 1,000 |
| Export usage per active user | ≥ 50% |
| Retention after 3 analyses | ≥ 70% |
| “Understood rationale” survey | ≥ 90% |

---

### 9. Next Steps
1. **Finalize PRD** and lock MVP scope
2. Create **architecture.md** (data flow, POI categories, services, map layer pipeline)
3. Engage **UX Expert** for wireframes (Serbian/English) and weights panel
4. Prototype **Azure Maps heatmap** for Belgrade; validate performance & data quality
5. Prepare **category mapping JSON** for initial business types (coffee, pharmacy, grocery, fitness, kids services)
