using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;

namespace VenueIQ.App.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            var vm = ServiceHost.GetRequiredService<SettingsViewModel>();
            BindingContext = vm;
            _ = vm.LoadAsync();
        }
    }
}
