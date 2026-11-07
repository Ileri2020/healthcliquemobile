using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class SearchInput : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(SearchInput), string.Empty);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchInput), "Search products...");
    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchInput), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(SearchInput), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(SearchInput), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(SearchInput), string.Empty);

    public SearchInput()
    {
        InitializeComponent();
        Results = [];
        SearchCommand = new Command(async () => await SearchAsync(), () => !IsLoading);
        SelectCommand = new Command<DiscoveryProduct>(product =>
        {
            if (product is not null)
                ProductSelected?.Invoke(this, product);
        });
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasResults => Results.Count > 0;
    public ObservableCollection<DiscoveryProduct> Results { get; }
    public ICommand SearchCommand { get; }
    public ICommand SelectCommand { get; }
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event Action<object?, Exception>? RequestFailed;

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl before searching."));
            return;
        }
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Results.Clear();
            OnPropertyChanged(nameof(HasResults));
            return;
        }
        IsLoading = true;
        HasStatus = false;
        (SearchCommand as Command)?.ChangeCanExecute();
        try
        {
            var path = $"api/dbhandler?model=product&minimal=true&limit=20&query={Uri.EscapeDataString(SearchText.Trim())}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
            var products = await response.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions)
                ?? throw new JsonException("The product search endpoint returned an empty response.");
            Results.Clear();
            foreach (var product in products)
                Results.Add(product);
            OnPropertyChanged(nameof(HasResults));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            (SearchCommand as Command)?.ChangeCanExecute();
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }
}
