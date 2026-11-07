namespace EEE.Components.UI;

public class ThemedOverlay : ContentView
{
    private const string LightOverlayResource = "ThemeOverlayLight";
    private const string DarkOverlayResource = "ThemeOverlayDark";
    private bool _isSubscribed;

    public ThemedOverlay()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        ApplyTheme(Application.Current?.RequestedTheme ?? AppTheme.Light);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Application.Current is not { } app)
            return;

        if (Parent is not null && !_isSubscribed)
        {
            app.RequestedThemeChanged += OnRequestedThemeChanged;
            _isSubscribed = true;
        }
        else if (Parent is null && _isSubscribed)
        {
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
            _isSubscribed = false;
        }
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyTheme(e.RequestedTheme);

    private void ApplyTheme(AppTheme theme)
    {
        var resources = Application.Current?.Resources;
        var key = theme == AppTheme.Dark ? DarkOverlayResource : LightOverlayResource;
        if (resources is not null && resources.TryGetValue(key, out var resource) && resource is Color color)
        {
            BackgroundColor = color;
        }
    }
}
