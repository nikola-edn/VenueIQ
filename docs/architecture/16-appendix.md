# 16. Appendix

## 16.1 Default Weights
```
WComplements=0.35, WAccessibility=0.25, WDemand=0.25, WCompetition=0.35
```

## 16.2 Distance Decay
```
Competitors:  exp(-d/300)
Complements:  exp(-d/200)
```

## 16.3 Example Normalize
```
Normalize(x) = (x - min) / (max - min + ε)
```

---

**Status:** Ready for implementation. This document + prd.md together are sufficient to begin story sharding and development.
