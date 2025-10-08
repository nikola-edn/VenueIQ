using System.Net;

namespace VenueIQ.App.Controls
{
    public partial class MapWebView : WebView
    {
        private TaskCompletionSource<bool>? _readyTcs;
        public event EventHandler? MapReady;
        public event EventHandler<string>? MapError;
        public event EventHandler? HeatmapRendered;

        public MapWebView()
        {
            InitializeComponent();
            Navigating += OnNavigating;
            SizeChanged += (_, __) => _ = EvaluateJavaScriptAsync("window.dispatchEvent(new Event('resize'))");
        }

        public async Task InitializeAsync(string apiKey, string language = "sr-Latn", double centerLat = 44.787, double centerLng = 20.449, int zoom = 11, CancellationToken ct = default)
        {
            _readyTcs = new TaskCompletionSource<bool>();
            var html = await LoadHtmlTemplateAsync(ct).ConfigureAwait(false);
            html = html.Replace("{{API_KEY}}", WebUtility.HtmlEncode(apiKey))
                       .Replace("{{LANG}}", WebUtility.HtmlEncode(language))
                       .Replace("{{CENTER_LAT}}", centerLat.ToString(System.Globalization.CultureInfo.InvariantCulture))
                       .Replace("{{CENTER_LNG}}", centerLng.ToString(System.Globalization.CultureInfo.InvariantCulture))
                       .Replace("{{ZOOM}}", zoom.ToString());
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Source = new HtmlWebViewSource { Html = html };
            });
            using var reg = ct.Register(() => _readyTcs!.TrySetCanceled(ct));
            await _readyTcs.Task.ConfigureAwait(false);
        }

        private async Task<string> LoadHtmlTemplateAsync(CancellationToken ct)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("Map/index.html").WaitAsync(ct);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url?.StartsWith("app://", StringComparison.OrdinalIgnoreCase) == true)
            {
                e.Cancel = true;
                var uri = new Uri(e.Url);
                var host = uri.Host;
                if (host.Equals("mapready", StringComparison.OrdinalIgnoreCase))
                {
                    _readyTcs?.TrySetResult(true);
                    MapReady?.Invoke(this, EventArgs.Empty);
                }
                else if (host.Equals("maprendered", StringComparison.OrdinalIgnoreCase))
                {
                    HeatmapRendered?.Invoke(this, EventArgs.Empty);
                }
                else if (host.Equals("maperror", StringComparison.OrdinalIgnoreCase))
                {
                    var reason = Uri.UnescapeDataString(uri.Query.TrimStart('?'));
                    MapError?.Invoke(this, reason);
                    _readyTcs?.TrySetException(new InvalidOperationException(reason));
                }
            }
        }

        public async Task UpdateHeatmapAsync(string geoJson, CancellationToken ct = default)
        {
            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(geoJson));
            var js = $"window.venueiq && window.venueiq.updateHeatmapB64 && window.venueiq.updateHeatmapB64('{b64}')";
            await EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public Task ClearHeatmapAsync(CancellationToken ct = default)
        {
            var js = "window.venueiq && window.venueiq.clearHeatmap && window.venueiq.clearHeatmap()";
            return EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public Task CenterOnAsync(double lat, double lng, double? score = null, CancellationToken ct = default)
        {
            var props = score.HasValue ? $", {score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : string.Empty;
            var js = $"window.venueiq && window.venueiq.centerOn && window.venueiq.centerOn({lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {{ score: {(score.HasValue ? score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0")} }})";
            return EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public async Task<byte[]?> CaptureImageAsync(string format = "png", double scale = 1.0, CancellationToken ct = default)
        {
            try
            {
                var js = $"(window.venueiq && window.venueiq.exportImage) ? window.venueiq.exportImage('{format}', {scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}) : 'ERROR:noexport'";
                using var reg = ct.Register(() => throw new OperationCanceledException(ct));
                var result = await EvaluateJavaScriptAsync(js).WaitAsync(ct);
                if (result is null) return null;
                if (result.StartsWith("ERROR:")) return null;
                // result is base64 without prefix
                return Convert.FromBase64String(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }
    }
}
