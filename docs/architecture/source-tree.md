# Source Tree

Canonical source: [9-source-tree.md](./9-source-tree.md)

```text
venueiq/
├── App.xaml / App.xaml.cs
├── Resources/
│   └── Strings/
│       ├── AppResources.resx
│       └── AppResources.sr-Latn.resx
├── Assets/
│   └── categories.serbia.json
├── Views/
│   ├── StartupPage.xaml
│   ├── MainPage.xaml
│   └── SettingsPage.xaml
├── ViewModels/
│   ├── StartupViewModel.cs
│   ├── MainViewModel.cs
│   └── SettingsViewModel.cs
├── Services/
│   ├── SettingsService.cs
│   ├── LocalizationService.cs
│   ├── PoiSearchService.cs
│   ├── MapAnalysisService.cs
│   ├── ScoreCalculator.cs
│   └── ExportService.cs
├── Controls/
│   └── MapWebView.xaml(.cs)       # WebView host + JS bridge
├── Models/
│   ├── Dtos.cs
│   └── Enums.cs
├── MauiProgram.cs                 # DI registrations
└── Directory.Packages.props       # pinned versions
```

DI registrations (MauiProgram.cs):
```csharp
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<PoiSearchService>();
builder.Services.AddSingleton<MapAnalysisService>();
builder.Services.AddSingleton<ScoreCalculator>();
builder.Services.AddSingleton<ExportService>();

builder.Services.AddTransient<StartupViewModel>();
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<SettingsViewModel>();
```

