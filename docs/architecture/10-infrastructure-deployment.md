# 10. Infrastructure & Deployment

- **Build/CI:** GitHub Actions (dotnet restore/build/test).  
- **Distribution:** Android (.aab), iOS (TestFlight), Windows (MSIX), macOS (.pkg).  
- **Environment:** no server required; users provide their own Azure Maps key.  
- **Config:** `AppConstants.cs` for static values (e.g., default radius). No secrets in code.

**Rollback Strategy:** Mobile store rollbacks via previous build; desktop via previous installers.

---
