using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;

namespace VenueIQ.App.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            var vm = ServiceHost.GetRequiredService<MainViewModel>();
            BindingContext = vm;
            _ = vm.LoadAsync();
        }
    }
}
