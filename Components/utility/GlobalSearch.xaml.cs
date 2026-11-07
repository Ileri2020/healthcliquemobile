using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class GlobalSearch : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? _searchCancellation;
    private List<DiscoveryProduct> _allProducts = [];
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(GlobalSearch), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(GlobalSearch), "Search products...");
    public static readonly BindableProperty ClassNameProperty = BindableProperty.Create(nameof(ClassName), typeof(string), typeof(GlobalSearch), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(GlobalSearch), "customer", propertyChanged: OnFilterChanged);
    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(nameof(SearchText), typeof(string), typeof(GlobalSearch), string.Empty, BindingMode.TwoWay, propertyChanged: OnSearchTextChanged);
    public static readonly BindableProperty SelectedCategoryProperty = BindableProperty.Create(nameof(SelectedCategory), typeof(string), typeof(GlobalSearch), "All", BindingMode.TwoWay, propertyChanged: OnFilterChanged);
    public static readonly BindableProperty SelectedBrandProperty = BindableProperty.Create(nameof(SelectedBrand), typeof(string), typeof(GlobalSearch), "All", BindingMode.TwoWay, propertyChanged: OnFilterChanged);
    public static readonly BindableProperty SelectedPriceRangeProperty = BindableProperty.Create(nameof(SelectedPriceRange), typeof(string), typeof(GlobalSearch), "All", BindingMode.TwoWay, propertyChanged: OnFilterChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(GlobalSearch), false);
    public static readonly BindableProperty ShowResultsProperty = BindableProperty.Create(nameof(ShowResults), typeof(bool), typeof(GlobalSearch), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(GlobalSearch), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(GlobalSearch), string.Empty);

    public GlobalSearch()
    {
        InitializeComponent();
        Categories = ["All"];
        Brands = ["All"];
        PriceRanges = ["All", "Under ₦5,000", "₦5,000–₦20,000", "Over ₦20,000"];
        Results = [];
        SearchCommand = new Command(async () => await SearchAsync());
        SelectProductCommand = new Command<DiscoveryProduct>(SelectProduct);
        Loaded += async (_, _) => await LoadProductsAsync();
        Unloaded += (_, _) => _searchCancellation?.Cancel();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string ClassName { get => (string)GetValue(ClassNameProperty); set => SetValue(ClassNameProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public string SelectedCategory { get => (string)GetValue(SelectedCategoryProperty); set => SetValue(SelectedCategoryProperty, value); }
    public string SelectedBrand { get => (string)GetValue(SelectedBrandProperty); set => SetValue(SelectedBrandProperty, value); }
    public string SelectedPriceRange { get => (string)GetValue(SelectedPriceRangeProperty); set => SetValue(SelectedPriceRangeProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool ShowResults { get => (bool)GetValue(ShowResultsProperty); private set => SetValue(ShowResultsProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<string> Brands { get; }
    public ObservableCollection<string> PriceRanges { get; }
    public ObservableCollection<SearchProductView> Results { get; }
    public ICommand SearchCommand { get; }
    public ICommand SelectProductCommand { get; }
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event Action<object?, Exception>? SearchFailed;

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((GlobalSearch)bindable).LoadProductsAsync();

    private static void OnSearchTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (GlobalSearch)bindable;
        control.ShowResults = !string.IsNullOrWhiteSpace((string)newValue);
        _ = control.DebouncedSearchAsync();
    }

    private static void OnFilterChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (GlobalSearch)bindable;
        if (control.IsLoaded)
            _ = control.SearchAsync();
    }

    private async Task LoadProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || IsLoading)
            return;
        IsLoading = true;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=product&include=category,brand,stock,activeIngredients&limit=100");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            _allProducts = await response.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions)
                ?? throw new JsonException("Product search returned an invalid response.");
            Categories.Clear();
            Categories.Add("All");
            foreach (var category in _allProducts.Select(item => item.Category?.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name))
                Categories.Add(category!);
            Brands.Clear();
            Brands.Add("All");
            foreach (var brand in _allProducts.Select(item => item.Brand?.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name))
                Brands.Add(brand!);
            if (!string.IsNullOrWhiteSpace(SearchText))
                await SearchAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DebouncedSearchAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        try
        {
            await Task.Delay(300, token);
            await SearchAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        var query = SearchText.Trim();
        if (query.Length > 0 && (query.Length < 3 || !query.Any(char.IsLetter)))
        {
            Results.Clear();
            return;
        }

        IsLoading = true;
        HasStatus = false;
        try
        {
            var path = "api/dbhandler?model=product&include=category,brand,stock,activeIngredients&limit=100";
            if (query.Length >= 3)
                path += $"&query={Uri.EscapeDataString(query)}";
            if (SelectedCategory != "All")
                path += $"&categoryName={Uri.EscapeDataString(SelectedCategory)}";
            if (SelectedBrand != "All")
                path += $"&brand={Uri.EscapeDataString(SelectedBrand)}";
            var uri = UtilityApi.CreateUri(ApiBaseUrl, path);
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null, cancellationToken);
            var products = await response.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions, cancellationToken)
                ?? throw new JsonException("Product search returned an invalid response.");
            Results.Clear();
            foreach (var product in products.Where(product => MatchesPrice(product)).Take(8))
                Results.Add(new SearchProductView(product, UserRole));
            ShowResults = query.Length > 0;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            if (exception is not TaskCanceledException || !cancellationToken.IsCancellationRequested)
                SetError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool MatchesPrice(DiscoveryProduct product)
    {
        var price = product.PriceForRole(UserRole);
        return SelectedPriceRange switch
        {
            "Under ₦5,000" => price < 5000,
            "₦5,000–₦20,000" => price is >= 5000 and <= 20000,
            "Over ₦20,000" => price > 20000,
            _ => true
        };
    }

    private void SelectProduct(DiscoveryProduct? product)
    {
        if (product is not null)
        {
            ShowResults = false;
            ProductSelected?.Invoke(this, product);
        }
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        SearchFailed?.Invoke(this, exception);
    }

    public sealed class SearchProductView
    {
        public SearchProductView(DiscoveryProduct product, string role)
        {
            Product = product;
            Name = product.Name;
            ImageUrl = product.ImageUrl;
            Category = product.Category;
            PriceLabel = product.PriceText(role);
        }
        public DiscoveryProduct Product { get; }
        public string Name { get; }
        public string ImageUrl { get; }
        public DiscoveryCategory? Category { get; }
        public string PriceLabel { get; }
    }
}
