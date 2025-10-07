using System.Net;

namespace VenueIQ.App.Controls
{
    public partial class MapWebView : WebView
    {
        private TaskCompletionSource<bool>? _readyTcs;
        public event EventHandler? MapReady;
        public event EventHandler<string>? MapError;

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
                else if (host.Equals("maperror", StringComparison.OrdinalIgnoreCase))
                {
                    var reason = Uri.UnescapeDataString(uri.Query.TrimStart('?'));
                    MapError?.Invoke(this, reason);
                    _readyTcs?.TrySetException(new InvalidOperationException(reason));
                }
            }
        }
    }
}
