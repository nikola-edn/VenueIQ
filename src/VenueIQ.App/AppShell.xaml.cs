using VenueIQ.App.Helpers;

namespace VenueIQ.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Explicit route registration for programmatic navigation
            Routing.RegisterRoute("Startup", typeof(Views.StartupPage));
            Routing.RegisterRoute("Main", typeof(Views.MainPage));
            Routing.RegisterRoute("Settings", typeof(Views.SettingsPage));
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, EventArgs e)
        {
            try
            {
                var settings = ServiceHost.GetRequiredService<Services.SettingsService>();
                var key = await settings.GetApiKeyAsync();
                if (string.IsNullOrWhiteSpace(key))
                {
                    await GoToAsync("//Startup");
                }
                else
                {
                    await GoToAsync("//Main");
                }
            }
            catch
            {
                await GoToAsync("//Startup");
            }
        }
    }
}
