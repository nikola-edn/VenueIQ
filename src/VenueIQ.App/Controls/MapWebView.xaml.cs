using System.Net;
using Microsoft.Extensions.Logging;

namespace VenueIQ.App.Controls
{
    public partial class MapWebView : WebView
    {
        private TaskCompletionSource<bool>? _readyTcs;
        public event EventHandler? MapReady;
        public event EventHandler<string>? MapError;
        public event EventHandler? HeatmapRendered;
        private readonly Microsoft.Extensions.Logging.ILogger<MapWebView>? _logger;

        public MapWebView()
        {
            InitializeComponent();
            Navigating += OnNavigating;
            SizeChanged += (_, __) => _ = EvaluateJavaScriptAsync("window.dispatchEvent(new Event('resize'))");
            try { _logger = VenueIQ.App.Helpers.ServiceHost.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MapWebView>>(); } catch { /* ignore */ }
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
            _logger?.LogDebug("MapWebView: waiting for ready event");
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
                    _logger?.LogDebug("MapWebView: MapReady event received");
                    MapReady?.Invoke(this, EventArgs.Empty);
                }
                else if (host.Equals("maprendered", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogDebug("MapWebView: HeatmapRendered event received");
                    HeatmapRendered?.Invoke(this, EventArgs.Empty);
                }
                else if (host.Equals("maperror", StringComparison.OrdinalIgnoreCase))
                {
                    var reason = Uri.UnescapeDataString(uri.Query.TrimStart('?'));
                    _logger?.LogError("MapWebView: error {Reason}", reason);
                    MapError?.Invoke(this, reason);
                    _readyTcs?.TrySetException(new InvalidOperationException(reason));
                }
            }
        }

        public async Task UpdateHeatmapAsync(string geoJson, CancellationToken ct = default)
        {
            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(geoJson));
            var js = $"window.venueiq && window.venueiq.updateHeatmapB64 && window.venueiq.updateHeatmapB64('{b64}')";
            _logger?.LogDebug("MapWebView: updateHeatmap len={Len}", geoJson?.Length ?? 0);
            await EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public Task SetOverlayModeAsync(string mode, CancellationToken ct = default)
        {
            var m = (mode ?? "heat").Trim().ToLowerInvariant();
            if (m != "grid" && m != "heat") m = "bubbles";
            var js = $"window.venueiq && window.venueiq.setOverlay && window.venueiq.setOverlay('{m}')";
            return EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public Task ClearHeatmapAsync(CancellationToken ct = default)
        {
            var js = "window.venueiq && window.venueiq.clearHeatmap && window.venueiq.clearHeatmap()";
            _logger?.LogDebug("MapWebView: clearHeatmap");
            return EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public async Task UpdateTopResultsAsync(string geoJson, CancellationToken ct = default)
        {
            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(geoJson));
            var js = $"window.venueiq && window.venueiq.updateTopResultsB64 && window.venueiq.updateTopResultsB64('{b64}')";
            _logger?.LogDebug("MapWebView: updateTopResults len={Len}", geoJson?.Length ?? 0);
            await EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public Task CenterOnAsync(double lat, double lng, double? score = null, CancellationToken ct = default)
        {
            var props = score.HasValue ? $", {score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : string.Empty;
            var js = $"window.venueiq && window.venueiq.centerOn && window.venueiq.centerOn({lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {{ score: {(score.HasValue ? score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0")} }})";
            return EvaluateJavaScriptAsync(js).WaitAsync(ct);
        }

        public async Task<(double lat, double lng)?> GetCenterAsync(CancellationToken ct = default)
        {
            try
            {
                var js = "(function(){try{ return (window.venueiq && window.venueiq.getCenter) ? window.venueiq.getCenter() : ''; }catch(e){return ''}})()";
                var res = await EvaluateJavaScriptAsync(js).WaitAsync(ct);
                if (string.IsNullOrWhiteSpace(res)) return null;
                var parts = res.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2) return null;
                if (double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                {
                    _logger?.LogDebug("MapWebView: getCenter lat={Lat} lng={Lng}", lat, lng);
                    return (lat, lng);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MapWebView: GetCenterAsync failed");
                return null;
            }
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
                var bytes = Convert.FromBase64String(result);
                _logger?.LogDebug("MapWebView: captureImage format={Format} scale={Scale} bytes={Len}", format, scale, bytes?.Length ?? 0);
                return bytes;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _logger?.LogError("MapWebView: captureImage failed");
                return null;
            }
        }
    }
}
