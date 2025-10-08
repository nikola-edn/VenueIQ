using Microsoft.Maui.Controls;
using VenueIQ.Core.Utils;

namespace VenueIQ.App.Controls;

public partial class FactorBadge : ContentView
{
    public static readonly BindableProperty DescriptorProperty = BindableProperty.Create(
        nameof(Descriptor), typeof(BadgeDescriptor), typeof(FactorBadge), default(BadgeDescriptor), propertyChanged: OnDescriptorChanged);

    public BadgeDescriptor Descriptor
    {
        get => (BadgeDescriptor)GetValue(DescriptorProperty);
        set => SetValue(DescriptorProperty, value);
    }

    public FactorBadge()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        // Entrance animation respecting simple reduce-motion toggle via App.Current?.UserAppTheme? (placeholder)
        bool reduceMotion = false; // TODO: wire actual setting if available
        if (!reduceMotion && this.IsVisible)
        {
            this.Scale = 0.95;
            this.Opacity = 0;
            _ = this.FadeTo(1, 150, Easing.CubicOut);
            _ = this.ScaleTo(1, 150, Easing.CubicOut);
        }
    }

    private static void OnDescriptorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (FactorBadge)bindable;
        var desc = newValue as BadgeDescriptor;
        view.ApplyDescriptor(desc);
    }

    private void ApplyDescriptor(BadgeDescriptor? d)
    {
        if (d is null || d.Severity == BadgeSeverity.None || d.PrimaryMetricValue < BadgeLogic.HideThreshold)
        {
            IsVisible = false;
            return;
        }
        IsVisible = true;
        // Colors and icons per severity
        Color bg;
        string icon;
        switch (d.Severity)
        {
            case BadgeSeverity.Success:
                bg = Color.FromArgb("#2E7D32");
                icon = "✓";
                break;
            case BadgeSeverity.Warning:
                bg = Color.FromArgb("#C05621");
                icon = "!";
                break;
            default:
                bg = Color.FromArgb("#1565C0");
                icon = "•";
                break;
        }
        BadgeBorder.Background = new SolidColorBrush(bg);
        TextLabel.TextColor = Microsoft.Maui.Graphics.Colors.White;
        IconLabel.TextColor = Microsoft.Maui.Graphics.Colors.White;
        IconLabel.Text = icon;

        // Accessibility: description summarizes numeric contribution
        var label = VenueIQ.App.Helpers.LocalizationResourceManager.Instance[d.TitleKey.Replace('.', '_')];
        var pct = Math.Round(d.PrimaryMetricValue * 100);
        SemanticProperties.SetDescription(this, $"{label}: {pct}%");
    }
}
