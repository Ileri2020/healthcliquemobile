using System.Net.Http.Json;

namespace EEE.Components.Utility;

public class VisitTracker : ContentView
{
    private static readonly HttpClient HttpClient = new();
    private static readonly HashSet<string> TrackedPaths = new(StringComparer.Ordinal);
    private const string BrowserIdKey = "health-clique-browser-id";

    public static readonly BindableProperty EndpointUrlProperty =
        BindableProperty.Create(nameof(EndpointUrl), typeof(string), typeof(VisitTracker), string.Empty, propertyChanged: OnTrackingPropertyChanged);
    public static readonly BindableProperty PathProperty =
        BindableProperty.Create(nameof(Path), typeof(string), typeof(VisitTracker), string.Empty, propertyChanged: OnTrackingPropertyChanged);
    public static readonly BindableProperty IsTrackingEnabledProperty =
        BindableProperty.Create(nameof(IsTrackingEnabled), typeof(bool), typeof(VisitTracker), true, propertyChanged: OnTrackingPropertyChanged);

    private static readonly string[] ExcludedPathPrefixes = ["/stock", "/sales", "/expenses"];

    public VisitTracker()
    {
        WidthRequest = 0;
        HeightRequest = 0;
        IsVisible = false;
        TrackVisit();
    }

    public string EndpointUrl { get => (string)GetValue(EndpointUrlProperty); set => SetValue(EndpointUrlProperty, value); }
    public string Path { get => (string)GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public bool IsTrackingEnabled { get => (bool)GetValue(IsTrackingEnabledProperty); set => SetValue(IsTrackingEnabledProperty, value); }
    public event Action<object?, Exception>? TrackingFailed;

    private static void OnTrackingPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((VisitTracker)bindable).TrackVisit();

    private async void TrackVisit()
    {
        if (!IsTrackingEnabled || string.IsNullOrWhiteSpace(Path) || IsExcluded(Path))
            return;

        if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            TrackingFailed?.Invoke(this, new InvalidOperationException("Set VisitTracker.EndpointUrl to an absolute HTTP(S) URL."));
            return;
        }

        lock (TrackedPaths)
        {
            if (!TrackedPaths.Add(Path))
                return;
        }

        try
        {
            var browserId = Preferences.Get(BrowserIdKey, string.Empty);
            if (string.IsNullOrWhiteSpace(browserId))
            {
                browserId = Guid.NewGuid().ToString();
                Preferences.Set(BrowserIdKey, browserId);
            }

            using var response = await HttpClient.PostAsJsonAsync(endpoint, new { path = Path, browserId });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            lock (TrackedPaths)
                TrackedPaths.Remove(Path);
            TrackingFailed?.Invoke(this, exception);
        }
    }

    private static bool IsExcluded(string path) =>
        ExcludedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
