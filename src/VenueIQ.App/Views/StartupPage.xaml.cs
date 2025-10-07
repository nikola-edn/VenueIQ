using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;

namespace VenueIQ.App.Views
{
    public partial class StartupPage : ContentPage
    {
        public StartupPage()
        {
            InitializeComponent();
            BindingContext = ServiceHost.GetRequiredService<StartupViewModel>();
        }
    }
}
