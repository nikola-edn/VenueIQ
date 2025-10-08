namespace VenueIQ.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Set default culture to Serbian Latin on first run
            try
            {
                var settings = Helpers.ServiceHost.GetRequiredService<Services.SettingsService>();
                var loc = Helpers.ServiceHost.GetRequiredService<Services.LocalizationService>();
                var lang = settings.GetLanguageAsync().GetAwaiter().GetResult();
                loc.SetCulture(lang);
            }
            catch { /* ignore in design-time */ }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
