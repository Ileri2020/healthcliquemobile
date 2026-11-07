using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.Utility;

namespace EEE.App.Pages;

public partial class ProductDetailsPage : ContentPage, IQueryAttributable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _productId;

    public ProductDetailsPage()
    {
        InitializeComponent();
        BindingContext = this;
        AddToCartCommand = new Command(AddToCart);
        WhatsAppCommand = new Command(async () => await OpenWhatsAppAsync());
        SimilarProducts.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        SimilarProducts.AddToCartRequested += (_, product) => CommerceCartState.Shared.Add(product);
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlert("Navigation unavailable", exception.Message, "OK");
    }

    public DiscoveryProduct? Product { get; private set; }
    public bool IsLoading { get; private set; }
    public bool HasStatus { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public string ProductPriceText => Product?.PriceText("visitor") ?? string.Empty;
    public string DescriptionText => string.IsNullOrWhiteSpace(Product?.Description)
        ? "No description available."
        : Product.Description;
    public bool HasIngredients => Product?.ActiveIngredients.Count > 0;
    public ICommand AddToCartCommand { get; }
    public ICommand WhatsAppCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value))
            return;

        _productId = Convert.ToString(value);
        if (!string.IsNullOrWhiteSpace(_productId))
            _ = LoadProductAsync(_productId);
    }

    private async Task LoadProductAsync(string productId)
    {
        IsLoading = true;
        HasStatus = false;
        NotifyProductChanged();
        try
        {
            var path = $"api/dbhandler?model=product&id={Uri.EscapeDataString(productId)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Get,
                UtilityApi.CreateUri(PageLayout.ApiBaseUrl, path), (object?)null);
            Product = await response.Content.ReadFromJsonAsync<DiscoveryProduct>(JsonOptions)
                ?? throw new JsonException("The product endpoint returned an empty response.");

            var categoryId = Product.Category?.Id;
            var similarPath = categoryId is null
                ? "api/dbhandler?model=product&include=category,brand,stock,activeIngredients&limit=50"
                : $"api/dbhandler?model=product&categoryId={Uri.EscapeDataString(categoryId)}&include=category,brand,stock,activeIngredients&limit=50";
            using var similarResponse = await UtilityApi.SendAsync(HttpMethod.Get,
                UtilityApi.CreateUri(PageLayout.ApiBaseUrl, similarPath), (object?)null);
            var similarProducts = await similarResponse.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions)
                ?? throw new JsonException("The similar products endpoint returned an empty response.");
            SimilarProducts.Products = similarProducts
                .Where(item => !string.Equals(item.Id, Product.Id, StringComparison.Ordinal))
                .Take(10)
                .ToList();
            Title = Product.Name;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
        }
        finally
        {
            IsLoading = false;
            NotifyProductChanged();
        }
    }

    private void AddToCart()
    {
        if (Product is null)
            return;
        CommerceCartState.Shared.Add(Product);
        _ = DisplayAlert("Added to cart", $"{Product.Name} was added to your cart.", "Continue");
    }

    private async Task OpenWhatsAppAsync()
    {
        if (Product is null)
            return;
        var message = Uri.EscapeDataString($"Hello, I would like to order {Product.Name}.");
        try
        {
            await Launcher.Default.OpenAsync($"https://wa.me/2348188762448?text={message}");
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or UriFormatException or InvalidOperationException)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
            NotifyProductChanged();
        }
    }

    private async Task OpenProductAsync(string productId)
    {
        if (!string.IsNullOrWhiteSpace(productId))
            await PageLayout.NavigateToAsync($"/products/{Uri.EscapeDataString(productId)}");
    }

    private void NotifyProductChanged()
    {
        OnPropertyChanged(nameof(Product));
        OnPropertyChanged(nameof(ProductPriceText));
        OnPropertyChanged(nameof(DescriptionText));
        OnPropertyChanged(nameof(HasIngredients));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(StatusMessage));
    }
}
