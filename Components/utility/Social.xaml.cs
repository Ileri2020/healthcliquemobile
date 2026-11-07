using System.Collections.ObjectModel;

namespace EEE.Components.Utility;

public partial class Social : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(ObservableCollection<SocialLink>), typeof(Social));

    public Social()
    {
        InitializeComponent();
        Items = [];
    }

    public ObservableCollection<SocialLink> Items { get => (ObservableCollection<SocialLink>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public event Action<object?, Exception>? OpenFailed;

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SocialLink link)
            return;

        try
        {
            if (!Uri.TryCreate(link.Uri, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeMailto))
                throw new InvalidOperationException($"Social link '{link.Name}' must use an https, http, or mailto URI.");

            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception exception)
        {
            OpenFailed?.Invoke(this, exception);
        }
        finally
        {
            ((CollectionView)sender!).SelectedItem = null;
        }
    }
}
