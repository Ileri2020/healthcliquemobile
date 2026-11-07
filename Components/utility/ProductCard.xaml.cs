using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class ProductCard : ContentView
{
    private bool _isLoadingWishlist;

    public static readonly BindableProperty ProductProperty = BindableProperty.Create(nameof(Product), typeof(DiscoveryProduct), typeof(ProductCard), propertyChanged: OnProductChanged);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(ProductCard), string.Empty, propertyChanged: OnContextChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(ProductCard), string.Empty, propertyChanged: OnContextChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(ProductCard), "customer", propertyChanged: OnDisplayChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(ProductCard), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsInWishlistProperty = BindableProperty.Create(nameof(IsInWishlist), typeof(bool), typeof(ProductCard), false, BindingMode.TwoWay, propertyChanged: OnDisplayChanged);
    public static readonly BindableProperty DiscountProperty = BindableProperty.Create(nameof(Discount), typeof(double), typeof(ProductCard), 0d, propertyChanged: OnDisplayChanged);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ProductCard), false);
    public static readonly BindableProperty IsDetailsOpenProperty = BindableProperty.Create(nameof(IsDetailsOpen), typeof(bool), typeof(ProductCard), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ProductCard), string.Empty);

    public ProductCard()
    {
        InitializeComponent();
        AddToCartCommand = new Command(() =>
        {
            if (Product is not null)
                AddToCartRequested?.Invoke(this, Product);
        });
        WishlistCommand = new Command(async () => await ToggleWishlistAsync(), () => Product is not null);
        DetailsCommand = new Command(() =>
        {
            if (Product is null)
                return;
            IsDetailsOpen = true;
            ProductSelected?.Invoke(this, Product);
        });
        DeleteCommand = new Command(async () => await DeleteAsync(), () => IsAdmin && Product is not null);
    }

    public DiscoveryProduct? Product { get => (DiscoveryProduct?)GetValue(ProductProperty); set => SetValue(ProductProperty, value); }
    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsInWishlist { get => (bool)GetValue(IsInWishlistProperty); set => SetValue(IsInWishlistProperty, value); }
    public double Discount { get => (double)GetValue(DiscountProperty); set => SetValue(DiscountProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public bool IsDetailsOpen { get => (bool)GetValue(IsDetailsOpenProperty); set => SetValue(IsDetailsOpenProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string CategoryName => Product?.Category?.Name ?? "Pharmacy";
    public string PriceText => $"₦{Math.Ceiling((Product?.PriceForRole(UserRole) ?? 0) * (1 - Math.Clamp(Discount, 0, 100) / 100) / 5) * 5:N0}";
    public string WishlistButtonText => IsInWishlist ? "♥" : "♡";
    public string RatingText => "★★★★★  5.0";
    public string DetailsTitle => Product?.Name ?? "Product details";
    public ICommand AddToCartCommand { get; }
    public ICommand WishlistCommand { get; }
    public ICommand DetailsCommand { get; }
    public ICommand DeleteCommand { get; }
    public event Action<object?, DiscoveryProduct>? AddToCartRequested;
    public event Action<object?, DiscoveryProduct?>? ToggleWishlistRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event Action<object?, DiscoveryProduct?>? ProductDeleted;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnProductChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ProductCard)bindable;
        control.OnPropertyChanged(nameof(CategoryName));
        control.OnPropertyChanged(nameof(PriceText));
        control.OnPropertyChanged(nameof(RatingText));
        control.OnPropertyChanged(nameof(DetailsTitle));
        (control.DeleteCommand as Command)?.ChangeCanExecute();
        (control.WishlistCommand as Command)?.ChangeCanExecute();
        _ = control.LoadWishlistStatusAsync();
    }

    private static void OnContextChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((ProductCard)bindable).LoadWishlistStatusAsync();

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        (((ProductCard)bindable).DeleteCommand as Command)?.ChangeCanExecute();

    private static void OnDisplayChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ProductCard)bindable;
        control.OnPropertyChanged(nameof(PriceText));
        control.OnPropertyChanged(nameof(WishlistButtonText));
    }

    private async Task DeleteAsync()
    {
        if (!IsAdmin || Product is null || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete,
                UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=product&id={Uri.EscapeDataString(Product.Id)}"), (object?)null);
            ProductDeleted?.Invoke(this, Product);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task ToggleWishlistAsync()
    {
        if (Product is null)
            return;
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl to update the wishlist."));
            return;
        }

        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/wishlist"),
                new { productId = Product.Id });
            var result = await response.Content.ReadFromJsonAsync<WishlistToggleResult>()
                ?? throw new System.Text.Json.JsonException("The wishlist endpoint returned an empty response.");
            IsInWishlist = result.Added;
            ToggleWishlistRequested?.Invoke(this, Product);
            HasStatus = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task LoadWishlistStatusAsync()
    {
        if (_isLoadingWishlist || Product is null || string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(UserId))
            return;
        _isLoadingWishlist = true;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Get,
                UtilityApi.CreateUri(ApiBaseUrl, "api/wishlist"), (object?)null);
            var result = await response.Content.ReadFromJsonAsync<WishlistProducts>()
                ?? throw new System.Text.Json.JsonException("The wishlist endpoint returned an empty response.");
            IsInWishlist = result.ProductIds.Contains(Product.Id, StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            _isLoadingWishlist = false;
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private sealed class WishlistToggleResult
    {
        public bool Added { get; set; }
    }

    private sealed class WishlistProducts
    {
        public List<string> ProductIds { get; set; } = [];
    }
}
