using System.IO;
using VenueIQ.App.Helpers;
using VenueIQ.App.ViewModels;
using VenueIQ.App.Controls;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;
using VenueIQ.Core.Models;

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
            Appearing += async (_, __) =>
            {
                var (apiKey, lang) = await vm.GetMapInitAsync();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        Map.MapReady += (_, ____) => MapLoadingOverlay.IsVisible = false;
                        Map.HeatmapRendered += (_, ____) => MainThread.BeginInvokeOnMainThread(() => SemanticScreenReader.Announce("Heatmap updated"));
                        Map.MapError += (_, reason) =>
                        {
                            MapLoadingOverlay.IsVisible = false;
                            // TODO: Hook telemetry/logging here
                            MainThread.BeginInvokeOnMainThread(() =>
                                SemanticScreenReader.Announce($"Map error: {reason}"));
                        };
                        await Map.InitializeAsync(apiKey, lang);
                    }
                    catch
                    {
                        MapLoadingOverlay.IsVisible = false;
                    }
                }
            };

            AnalyzeButton.Clicked += async (_, __) => await RunAnalysisAsync();
            ExportButton.Clicked += async (_, __) => await OpenExportDialogAsync();
            ExportPdfButton.Clicked += async (_, __) => await OpenExportPdfDialogAsync();
            ResultsList_SelectionSetup();

            // Sliders drag state: disable Analyze while dragging
            void HookSlider(Slider s)
            {
                s.DragStarted += (_, __) => vm.SetWeightsDragging(true);
                s.DragCompleted += (_, __) => vm.SetWeightsDragging(false);
            }
            HookSlider(ComplementsSlider);
            HookSlider(AccessibilitySlider);
            HookSlider(DemandSlider);
            HookSlider(CompetitionSlider);

            // Live recompute: update map when weights recompute
            vm.WeightsRecomputed += async (_, cells) =>
            {
                try
                {
                    _renderCts?.Cancel();
                    _renderCts = new CancellationTokenSource();
                    var geo = GeoJsonHelper.BuildFeatureCollection(cells);
                    await Map.UpdateHeatmapAsync(geo, _renderCts.Token);
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Status_Updated"]))
                    ;
                }
                catch (OperationCanceledException) { }
            };

            // Announce updating state politely
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsUpdating) && vm.IsUpdating)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Weights_Updating"]))
                    ;
                }
            };

            // Open-in-Maps from tooltip
            vm.OpenInMapsRequested += async (_, coords) =>
            {
                await Map.CenterOnAsync(coords.lat, coords.lng);
            };
        }

        private async Task OpenExportDialogAsync()
        {
            try
            {
                ExportDialogOverlay.IsVisible = true;
                ExportProgressLabel.IsVisible = true;
                ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Loading"];
                await Task.Yield();
                var preview = await Map.CaptureImageAsync("png", 0.5);
                if (preview is not null)
                {
                    ExportPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(preview));
                    ExportProgressLabel.IsVisible = false;
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Export_Preview_Ready"]))
                    ;
                }
                else
                {
                    ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Unavailable"];
                }
            }
            catch
            {
                ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Unavailable"];
            }

            CancelExportButton.Clicked -= CancelExportButton_Clicked;
            CancelExportButton.Clicked += CancelExportButton_Clicked;
            SaveExportButton.Clicked -= SaveExportButton_Clicked;
            SaveExportButton.Clicked += SaveExportButton_Clicked;
        }

        private void CancelExportButton_Clicked(object? sender, EventArgs e)
        {
            ExportDialogOverlay.IsVisible = false;
            ExportPreviewImage.Source = null;
            ExportProgressLabel.IsVisible = false;
        }

        private async void SaveExportButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                SaveExportButton.IsEnabled = false;
                ExportProgressLabel.IsVisible = true;
                ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Saving"];
                MainThread.BeginInvokeOnMainThread(() =>
                    SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Export_Saving"]))
                ;

                var vm = (MainViewModel)BindingContext;
                var format = ExportFormatJpg.IsChecked ? "jpeg" : "png";
                var scale = ExportResHigh.IsChecked ? 2.0 : 1.0;
                var weights = (vm.WComplements, vm.WAccessibility, vm.WDemand, vm.WCompetition);
                var business = "Coffee"; // TODO: bind from picker when wired

                var export = ServiceHost.GetRequiredService<ExportService>();
                var path = await export.ExportHeatmapAsync(Map, format, scale, weights, vm.RadiusKm, business);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    ExportProgressLabel.Text = string.Format(Helpers.LocalizationResourceManager.Instance["Export_Success"], path);
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Export_Success_A11y"]))
                    ;
                }
                else
                {
                    ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Error"];
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Export_Error"]))
                    ;
                }
            }
            catch
            {
                ExportProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Error"];
            }
            finally
            {
                SaveExportButton.IsEnabled = true;
            }
        }

        private async Task OpenExportPdfDialogAsync()
        {
            try
            {
                ExportPdfDialogOverlay.IsVisible = true;
                ExportPdfProgressLabel.IsVisible = true;
                ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Loading"];
                await Task.Yield();
                var preview = await Map.CaptureImageAsync("png", 0.5);
                if (preview is not null)
                {
                    ExportPdfCoverPreview.Source = ImageSource.FromStream(() => new MemoryStream(preview));
                    ExportPdfProgressLabel.IsVisible = false;
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["Export_Preview_Ready"]))
                    ;
                }
                else
                {
                    ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Unavailable"];
                }
            }
            catch
            {
                ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["Export_Preview_Unavailable"];
            }

            CancelExportPdfButton.Clicked -= CancelExportPdfButton_Clicked;
            CancelExportPdfButton.Clicked += CancelExportPdfButton_Clicked;
            SaveExportPdfButton.Clicked -= SaveExportPdfButton_Clicked;
            SaveExportPdfButton.Clicked += SaveExportPdfButton_Clicked;
        }

        private void CancelExportPdfButton_Clicked(object? sender, EventArgs e)
        {
            ExportPdfDialogOverlay.IsVisible = false;
            ExportPdfCoverPreview.Source = null;
            ExportPdfProgressLabel.IsVisible = false;
        }

        private async void SaveExportPdfButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                SaveExportPdfButton.IsEnabled = false;
                ExportPdfProgressLabel.IsVisible = true;
                ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["ExportPdf_Building"];
                MainThread.BeginInvokeOnMainThread(() =>
                    SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["ExportPdf_Building"]))
                ;

                var vm = (MainViewModel)BindingContext;
                var weights = (vm.WComplements, vm.WAccessibility, vm.WDemand, vm.WCompetition);
                var business = "Coffee"; // TODO: bind from picker when wired
                var opts = new PdfExportOptions
                {
                    Language = ExportPdfLanguagePicker.SelectedIndex == 1 ? "en" : "sr-Latn",
                    Orientation = ExportPdfLandscape.IsChecked ? "Landscape" : "Portrait",
                    IncludeMapThumbnails = ExportPdfIncludeThumbnails.IsChecked == true,
                    IncludePoiTables = ExportPdfIncludePoiTables.IsChecked == true,
                    IncludeMethodology = ExportPdfIncludeMethodology.IsChecked == true,
                    IncludeExecutiveSummary = ExportPdfIncludeExecSummary.IsChecked == true,
                    MaxResults = 10
                };
                var progress = new Progress<string>(key =>
                {
                    ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance[key];
                });
                var export = ServiceHost.GetRequiredService<ExportService>();
                var path = await export.ExportResultsPdfAsync(Map, vm.Results, weights, vm.RadiusKm, business, opts, progress);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    ExportPdfProgressLabel.Text = string.Format(Helpers.LocalizationResourceManager.Instance["Export_Success"], path);
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["ExportPdf_Success_A11y"]))
                    ;
                }
                else
                {
                    ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["ExportPdf_Error"];
                    MainThread.BeginInvokeOnMainThread(() =>
                        SemanticScreenReader.Announce(Helpers.LocalizationResourceManager.Instance["ExportPdf_Error"]))
                    ;
                }
            }
            catch
            {
                ExportPdfProgressLabel.Text = Helpers.LocalizationResourceManager.Instance["ExportPdf_Error"];
            }
            finally
            {
                SaveExportPdfButton.IsEnabled = true;
            }
        }

        private CancellationTokenSource? _renderCts;
        private async Task RunAnalysisAsync()
        {
            try
            {
                _renderCts?.Cancel();
                _renderCts = new CancellationTokenSource();
                MapLoadingOverlay.IsVisible = true;
                var vm = (MainViewModel)BindingContext;
                var (apiKey, lang) = await vm.GetMapInitAsync();
                // BusinessType selection TBD; default to Coffee for MVP
                var input = new AnalysisInput(BusinessType.Coffee, 44.787, 20.449, vm.RadiusKm, lang);
                var weights = new Weights(vm.WComplements, vm.WAccessibility, vm.WDemand, vm.WCompetition);
                var analysis = ServiceHost.GetRequiredService<MapAnalysisService>();
                var res = await analysis.AnalyzeAsync(input, weights, _renderCts.Token);
                if (res.CellDetails is { Count: > 0 })
                {
                    var geo = GeoJsonHelper.BuildFeatureCollection(res.CellDetails);
                    await Map.UpdateHeatmapAsync(geo, _renderCts.Token);
                    vm.SetResults(res.CellDetails);
                }
                else
                {
                    await Map.ClearHeatmapAsync(_renderCts.Token);
                    vm.Results.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch
            {
                // TODO: telemetry
            }
            finally
            {
                MapLoadingOverlay.IsVisible = false;
            }
        }

        private void ResultsList_SelectionSetup()
        {
            // Hook selection events to center the map
            // Handle via BindingContext changes
            this.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.Selected))
                {
                    var vm = (MainViewModel)BindingContext;
                    var sel = vm.Selected;
                    if (sel is not null)
                    {
                        await Map.CenterOnAsync(sel.Lat, sel.Lng, sel.Score);
                    }
                }
            };
        }
    }
}
