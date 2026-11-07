using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Stocks : ContentView
{
    private const int PageSize = 30;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _reloadRequested;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Stocks), string.Empty, propertyChanged: OnApiChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(Stocks), "customer", propertyChanged: OnApiChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(Stocks), false, propertyChanged: OnApiChanged);
    public static readonly BindableProperty CategoryFilterProperty = BindableProperty.Create(nameof(CategoryFilter), typeof(string), typeof(Stocks), string.Empty);
    public static readonly BindableProperty ConcernFilterProperty = BindableProperty.Create(nameof(ConcernFilter), typeof(string), typeof(Stocks), string.Empty);
    public static readonly BindableProperty BrandFilterProperty = BindableProperty.Create(nameof(BrandFilter), typeof(string), typeof(Stocks), string.Empty);
    public static readonly BindableProperty SearchFilterProperty = BindableProperty.Create(nameof(SearchFilter), typeof(string), typeof(Stocks), string.Empty);
    public static readonly BindableProperty IsFeaturedProperty = BindableProperty.Create(nameof(IsFeatured), typeof(bool), typeof(Stocks), false);
    public static readonly BindableProperty IsDiscountedProperty = BindableProperty.Create(nameof(IsDiscounted), typeof(bool), typeof(Stocks), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Stocks), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Stocks), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Stocks), string.Empty);
    public static readonly BindableProperty CurrentPageProperty = BindableProperty.Create(nameof(CurrentPage), typeof(int), typeof(Stocks), 1);
    public static readonly BindableProperty TotalProductsProperty = BindableProperty.Create(nameof(TotalProducts), typeof(int), typeof(Stocks), 0);

    public Stocks()
    {
        InitializeComponent();
        Products = [];
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        ApplyFiltersCommand = new Command(async () => await ApplyFiltersAsync(), () => !IsLoading);
        PreviousCommand = new Command(async () =>
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                await LoadAsync();
            }
        }, () => CanGoPrevious && !IsLoading);
        NextCommand = new Command(async () =>
        {
            if (CanGoNext)
            {
                CurrentPage++;
                await LoadAsync();
            }
        }, () => CanGoNext && !IsLoading);
        AddToCartCommand = new Command<StockProductView>(item =>
        {
            if (item is not null)
                AddToCartRequested?.Invoke(this, item.Product);
        });
        SelectProductCommand = new Command<StockProductView>(item =>
        {
            if (item is not null)
                ProductSelected?.Invoke(this, item.Product);
        });
        AddProductCommand = new Command(() => AddProductRequested?.Invoke(this, EventArgs.Empty), () => IsAdmin);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public string CategoryFilter { get => (string)GetValue(CategoryFilterProperty); set => SetValue(CategoryFilterProperty, value); }
    public string ConcernFilter { get => (string)GetValue(ConcernFilterProperty); set => SetValue(ConcernFilterProperty, value); }
    public string BrandFilter { get => (string)GetValue(BrandFilterProperty); set => SetValue(BrandFilterProperty, value); }
    public string SearchFilter { get => (string)GetValue(SearchFilterProperty); set => SetValue(SearchFilterProperty, value); }
    public bool IsFeatured { get => (bool)GetValue(IsFeaturedProperty); set => SetValue(IsFeaturedProperty, value); }
    public bool IsDiscounted { get => (bool)GetValue(IsDiscountedProperty); set => SetValue(IsDiscountedProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public int CurrentPage { get => (int)GetValue(CurrentPageProperty); private set => SetValue(CurrentPageProperty, value); }
    public int TotalProducts { get => (int)GetValue(TotalProductsProperty); private set => SetValue(TotalProductsProperty, value); }
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalProducts / PageSize));
    public string PageText => $"{CurrentPage} / {TotalPages}";
    public ObservableCollection<StockProductView> Products { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand SelectProductCommand { get; }
    public ICommand AddProductCommand { get; }
    public event Action<object?, DiscoveryProduct>? AddToCartRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event EventHandler? AddProductRequested;
    public event Action<object?, Exception>? RequestFailed;

    public async Task ApplyFiltersAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    private static void OnApiChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Stocks)bindable).OnContextChanged();

    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        if (IsLoading)
        {
            _reloadRequested = true;
            return;
        }
        IsLoading = true;
        HasStatus = false;
        UpdateCommands();
        try
        {
            if (IsFeatured || IsDiscounted)
                await LoadFilteredPageAsync();
            else
                await LoadPageAsync();
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
            if (_reloadRequested)
            {
                _reloadRequested = false;
                await LoadAsync();
            }
        }
    }

    private void OnContextChanged()
    {
        UpdateCommands();
        _ = LoadAsync();
    }

    private async Task LoadPageAsync()
    {
        var path = BuildProductPath(true);
        var result = await FetchAsync<StockPageResponse>(path);
        TotalProducts = result.Total;
        SetProducts(result.Data);
    }

    private async Task LoadFilteredPageAsync()
    {
        var products = await FetchAsync<List<DiscoveryProduct>>(BuildProductPath(false, 5000));
        IEnumerable<DiscoveryProduct> filtered = products;
        if (IsFeatured)
        {
            var featured = await FetchAsync<List<DiscoveryFeaturedProduct>>("api/dbhandler?model=featuredProduct&minimal=true&limit=5000");
            var ids = featured.Select(item => item.ProductId).ToHashSet(StringComparer.Ordinal);
            filtered = filtered.Where(product => ids.Contains(product.Id));
        }
        if (IsDiscounted)
        {
            var discounted = await FetchAsync<List<DiscountedProductReference>>("api/heavily-discounted?admin=false");
            var ids = discounted.Select(item => item.ProductId).ToHashSet(StringComparer.Ordinal);
            filtered = filtered.Where(product => ids.Contains(product.Id));
        }

        var filteredProducts = filtered.ToList();
        TotalProducts = filteredProducts.Count;
        SetProducts(filteredProducts.Skip((CurrentPage - 1) * PageSize).Take(PageSize));
    }

    private string BuildProductPath(bool pagination, int? limit = null)
    {
        var parameters = new List<string>
        {
            "model=product",
            "include=category,brand,stock",
            $"limit={limit ?? PageSize}"
        };
        if (!string.IsNullOrWhiteSpace(BrandFilter))
            parameters.Add($"brand={Uri.EscapeDataString(BrandFilter.Trim())}");
        if (!string.IsNullOrWhiteSpace(CategoryFilter))
            parameters.Add($"categoryName={Uri.EscapeDataString(CategoryFilter.Trim())}");
        if (!string.IsNullOrWhiteSpace(ConcernFilter))
            parameters.Add($"concern={Uri.EscapeDataString(ConcernFilter.Trim())}");
        if (!string.IsNullOrWhiteSpace(SearchFilter))
            parameters.Add($"query={Uri.EscapeDataString(SearchFilter.Trim())}");
        if (!IsAdmin)
        {
            parameters.Add("requireImages=true");
            parameters.Add("requirePrice=true");
        }
        if (pagination)
        {
            parameters.Add("pagination=true");
            parameters.Add($"offset={(CurrentPage - 1) * PageSize}");
        }
        return "api/dbhandler?" + string.Join("&", parameters);
    }

    private async Task<T> FetchAsync<T>(string path)
    {
        using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new JsonException($"The product endpoint returned an empty response for '{path}'.");
    }

    private void SetProducts(IEnumerable<DiscoveryProduct> products)
    {
        Products.Clear();
        foreach (var product in products)
            Products.Add(new StockProductView(product, UserRole));
    }

    private void UpdateCommands()
    {
        (RefreshCommand as Command)?.ChangeCanExecute();
        (ApplyFiltersCommand as Command)?.ChangeCanExecute();
        (PreviousCommand as Command)?.ChangeCanExecute();
        (NextCommand as Command)?.ChangeCanExecute();
        (AddProductCommand as Command)?.ChangeCanExecute();
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private sealed class StockPageResponse
    {
        public List<DiscoveryProduct> Data { get; set; } = [];
        public int Total { get; set; }
    }

    private sealed class DiscountedProductReference
    {
        public string ProductId { get; set; } = string.Empty;
    }
}

public sealed class StockProductView
{
    public StockProductView(DiscoveryProduct product, string role)
    {
        Product = product;
        Name = product.Name;
        ImageUrl = product.ImageUrl;
        PriceText = product.PriceText(role);
    }

    public DiscoveryProduct Product { get; }
    public string Name { get; }
    public string ImageUrl { get; }
    public string PriceText { get; }
}
