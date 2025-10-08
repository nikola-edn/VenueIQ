using Microsoft.Maui.Controls;
using VenueIQ.App.ViewModels;
using System.Windows.Input;
using Microsoft.Maui.Devices;

namespace VenueIQ.App.Controls;

public partial class ResultTooltip : ContentView
{
    public ResultTooltip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        CloseButton.Clicked += (_, __) =>
        {
            if (BindingContext is ResultItemViewModel vm) vm.IsTooltipOpen = false;
        };
        // Defer setting AutomationId for desktop/mobile variant
        var id = DeviceInfo.Idiom == DeviceIdiom.Desktop ? "Results.Tooltip.Desktop" : "Results.Tooltip.Mobile";
        TooltipContainer.AutomationId = id;
    }

    public static readonly BindableProperty OpenInMapsCommandProperty = BindableProperty.Create(
        nameof(OpenInMapsCommand), typeof(ICommand), typeof(ResultTooltip));

    public ICommand? OpenInMapsCommand
    {
        get => (ICommand?)GetValue(OpenInMapsCommandProperty);
        set => SetValue(OpenInMapsCommandProperty, value);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        // Animate entrance if visible
        if (IsVisible)
        {
            bool reduceMotion = false; // TODO: wire to platform setting
            if (!reduceMotion)
            {
                this.Opacity = 0;
                this.TranslationY = 4;
                _ = this.FadeTo(1, 200, Easing.CubicOut);
                _ = this.TranslateTo(0, 0, 200, Easing.CubicOut);
            }
            CloseButton.Focus();
        }
    }
}
