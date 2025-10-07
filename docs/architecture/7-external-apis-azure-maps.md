# 7. External APIs (Azure Maps)

## 7.1 Search (POI) — Category-based
- **Endpoint:** `GET https://atlas.microsoft.com/search/poi/category/json`  
- **Params:** `api-version=1.0`, `subscription-key={key}`, `lat`, `lon`, `radius`, `categorySet`, `language=sr-Latn|en`, `limit`, `view=Auto`  
- **Auth:** Subscription key in query (MVP)  
- **Rate/Quotas:** Observe free/paid tier limits; backoff + user messaging on throttling.

## 7.2 Static Map (optional for thumbnails)
- **Endpoint:** `GET /map/static/png` for small preview images in PDF (if used).

**Integration Notes:**  
- Batch queries per tile/ring to respect limits.  
- Cache per `BusinessTypeId + center + radius` for session-lifetime to reduce calls.  
- When weights change, recompute locally (no new REST calls).

---
