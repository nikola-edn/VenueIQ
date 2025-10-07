using Microsoft.Extensions.Logging;
using VenueIQ.App.Services;
using VenueIQ.App.ViewModels;
using VenueIQ.Core.Services;
using VenueIQ.App.Helpers;

namespace VenueIQ.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // DI registrations
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<LocalizationService>();
            builder.Services.AddSingleton<PoiSearchService>();
            builder.Services.AddSingleton<MapAnalysisService>();
            builder.Services.AddSingleton<ScoreCalculator>();
            builder.Services.AddSingleton<ExportService>();

            builder.Services.AddTransient<StartupViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            var app = builder.Build();
            ServiceHost.Services = app.Services;
            return app;
        }
    }
}
