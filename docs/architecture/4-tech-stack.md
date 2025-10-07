# 4. Tech Stack

## 4.1 Cloud Infrastructure
- **Provider:** Microsoft Azure  
- **Key Services:** Azure Maps (Search REST, Web Control)  
- **Regions:** EU region preferred for lower latency (e.g., West Europe)

## 4.2 Technology Stack Table
| Category | Technology | Version | Purpose | Rationale |
|---|---|---|---|---|
| Language | C# | 12 | App code | Modern features, performance |
| Runtime | .NET | 9.0.x | Cross-platform MAUI | LTS, perf |
| Framework | .NET MAUI | 8.x | Cross-platform UI | Single codebase |
| Mapping UI | Azure Maps Web Control | latest | Heatmap render | Mature heatmap, webview |
| Mapping REST | Azure Maps Search API | v1 | POI/category search | Category filters, language |
| PDF Export | QuestPDF **or** Syncfusion.Pdf | latest | Results report | Reliable PDF generation |
| Image Export | MAUI Graphics | n/a | Map viewport snapshot overlay | No extra dependency |
| DI | Built-in `Microsoft.Extensions.DependencyInjection` | latest | Composition | Standard |
| JSON | System.Text.Json | latest | (De)serialization | Fast, built-in |
| Logging | Microsoft.Extensions.Logging | latest | App logs | Unified abstractions |

> Pin exact versions in `Directory.Packages.props` once NuGet libs are finalized.

---

> Environment Constraint: WSL + MAUI
>
> This repository is operated under WSL. Although `dotnet` is available, .NET MAUI build/run/test is not supported in this environment. Development agents should implement code only; builds and tests occur on a proper MAUI host or CI.
