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
        }
    }
}
