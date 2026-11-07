using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Gallery : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Gallery), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(Gallery), "customer");
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Gallery), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Gallery), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Gallery), string.Empty);

    public Gallery()
    {
        InitializeComponent();
        Products = [];
        RefreshCommand = new Command(async () => await LoadAsync());
        AddToCartCommand = new Command<DiscoveryProduct>(product =>
        {
            if (product is not null)
                AddToCartRequested?.Invoke(this, product);
        });
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<GalleryProductView> Products { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddToCartCommand { get; }
    public event Action<object?, DiscoveryProduct>? AddToCartRequested;
    public event Action<object?, Exception>? RequestFailed;

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=product&include=category,stock&minimal=true&limit=50&requireImages=true");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var products = await response.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions)
                ?? throw new JsonException("Product catalogue API returned an invalid response.");
            Products.Clear();
            foreach (var product in products.Where(product => !string.IsNullOrWhiteSpace(product.ImageUrl)).OrderBy(_ => Random.Shared.Next()).Take(10))
                Products.Add(new GalleryProductView(product, UserRole));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public sealed class GalleryProductView
    {
        public GalleryProductView(DiscoveryProduct product, string role)
        {
            Product = product;
            Name = product.Name;
            Description = product.Description ?? string.Empty;
            ImageUrl = product.ImageUrl;
            PriceLabel = product.PriceText(role);
        }
        public DiscoveryProduct Product { get; }
        public string Name { get; }
        public string Description { get; }
        public string ImageUrl { get; }
        public string PriceLabel { get; }
    }
}
