using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class FeaturedProducts : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] TopBrands = ["Emzor", "Vitabiotics", "Pfizer", "GSK", "Shalina", "Fidson"];
    private readonly Dictionary<string, string> _featuredIds = [];
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(FeaturedProducts), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(FeaturedProducts), "customer");
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(FeaturedProducts), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(FeaturedProducts), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty HasFeaturedProperty = BindableProperty.Create(nameof(HasFeatured), typeof(bool), typeof(FeaturedProducts), false);
    public static readonly BindableProperty HasOralCareProperty = BindableProperty.Create(nameof(HasOralCare), typeof(bool), typeof(FeaturedProducts), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(FeaturedProducts), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(FeaturedProducts), string.Empty);

    public FeaturedProducts()
    {
        InitializeComponent();
        FeaturedProductItems = [];
        OralCareProducts = [];
        BrandGroups = [];
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        AddToCartCommand = new Command<DiscoveryProduct>(product =>
        {
            if (product is not null)
                AddToCartRequested?.Invoke(this, product);
        });
        ToggleFeaturedCommand = new Command<DiscoveryProduct>(async product => await ToggleFeaturedAsync(product), product => IsAdmin && !IsLoading);
        ProductSelectedCommand = new Command<DiscoveryProduct>(product =>
        {
            if (product is not null)
                ProductSelected?.Invoke(this, product);
        });
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasFeatured { get => (bool)GetValue(HasFeaturedProperty); private set => SetValue(HasFeaturedProperty, value); }
    public bool HasOralCare { get => (bool)GetValue(HasOralCareProperty); private set => SetValue(HasOralCareProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<ProductCardView> FeaturedProductItems { get; }
    public ObservableCollection<ProductCardView> OralCareProducts { get; }
    public ObservableCollection<BrandProductGroup> BrandGroups { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand ToggleFeaturedCommand { get; }
    public ICommand ProductSelectedCommand { get; }
    public event Action<object?, DiscoveryProduct>? AddToCartRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((FeaturedProducts)bindable).LoadAsync();

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((FeaturedProducts)bindable).UpdateCommands();

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((FeaturedProducts)bindable).UpdateCommands();

    private void UpdateCommands()
    {
        (RefreshCommand as Command)?.ChangeCanExecute();
        (ToggleFeaturedCommand as Command<DiscoveryProduct>)?.ChangeCanExecute();
    }

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var featuredTask = FetchAsync<List<DiscoveryFeaturedProduct>>("api/dbhandler?model=featuredProduct&minimal=true&limit=15");
            var brandTasks = TopBrands.Select(brand => FetchAsync<List<DiscoveryProduct>>(
                $"api/dbhandler?model=product&brand={Uri.EscapeDataString(brand)}&minimal=true&include=category,brand,stock&limit=6")).ToArray();
            var oralCareTask = FetchAsync<List<DiscoveryProduct>>(
                "api/dbhandler?model=product&include=category,stock&minimal=true&limit=10");
            await Task.WhenAll(brandTasks.Cast<Task>().Append(featuredTask).Append(oralCareTask));
            var featured = await featuredTask;
            if (featured.Count == 0)
            {
                var fallback = await FetchAsync<List<DiscoveryProduct>>("api/dbhandler?model=product&minimal=true&include=category,stock&limit=10");
                featured = fallback.Select(product => new DiscoveryFeaturedProduct { Id = string.Empty, ProductId = product.Id, Product = product }).ToList();
            }

            _featuredIds.Clear();
            FeaturedProductItems.Clear();
            foreach (var item in featured.Where(item => item.Product is not null))
            {
                _featuredIds[item.ProductId] = item.Id;
                FeaturedProductItems.Add(new ProductCardView(item.Product!, UserRole, !string.IsNullOrWhiteSpace(item.Id)));
            }
            HasFeatured = FeaturedProductItems.Count > 0;

            BrandGroups.Clear();
            for (var index = 0; index < TopBrands.Length; index++)
            {
                var products = await brandTasks[index];
                BrandGroups.Add(new BrandProductGroup(TopBrands[index], products.Take(6).Select(product =>
                    new ProductCardView(product, UserRole, _featuredIds.TryGetValue(product.Id, out var id) && !string.IsNullOrWhiteSpace(id)))));
            }

            var oralCare = await oralCareTask;
            OralCareProducts.Clear();
            foreach (var product in oralCare.Where(product => product.Category?.Name.Contains("oral", StringComparison.OrdinalIgnoreCase) == true
                                                              || product.Category?.Name.Contains("dental", StringComparison.OrdinalIgnoreCase) == true).Take(10))
                OralCareProducts.Add(new ProductCardView(product, UserRole, _featuredIds.TryGetValue(product.Id, out var id) && !string.IsNullOrWhiteSpace(id)));
            HasOralCare = OralCareProducts.Count > 0;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private async Task<T> FetchAsync<T>(string path)
    {
        var uri = UtilityApi.CreateUri(ApiBaseUrl, path);
        using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new JsonException($"API returned an invalid response for {path}.");
    }

    private async Task ToggleFeaturedAsync(DiscoveryProduct? product)
    {
        if (!IsAdmin || product is null || IsLoading)
            return;
        IsLoading = true;
        try
        {
            if (_featuredIds.TryGetValue(product.Id, out var featuredId) && !string.IsNullOrWhiteSpace(featuredId))
            {
                var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=featuredProduct&id={Uri.EscapeDataString(featuredId)}");
                using var response = await UtilityApi.SendAsync(HttpMethod.Delete, uri, (object?)null);
                _featuredIds.Remove(product.Id);
                UpdateProductFeaturedState(product.Id, false);
                var existing = FeaturedProductItems.FirstOrDefault(item => item.Product.Id == product.Id);
                if (existing is not null)
                    FeaturedProductItems.Remove(existing);
                HasFeatured = FeaturedProductItems.Count > 0;
            }
            else
            {
                var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=featuredProduct");
                using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new { productId = product.Id });
                var created = await response.Content.ReadFromJsonAsync<DiscoveryFeaturedProduct>(JsonOptions)
                    ?? throw new JsonException("Featured product API returned an invalid response.");
                _featuredIds[product.Id] = created.Id;
                UpdateProductFeaturedState(product.Id, true);
                if (!FeaturedProductItems.Any(item => item.Product.Id == product.Id))
                    FeaturedProductItems.Insert(0, new ProductCardView(product, UserRole, true));
                HasFeatured = FeaturedProductItems.Count > 0;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private void UpdateProductFeaturedState(string productId, bool isFeatured)
    {
        var cards = FeaturedProductItems
            .Concat(OralCareProducts)
            .Concat(BrandGroups.SelectMany(group => group.Products));
        foreach (var card in cards.Where(card => card.Product.Id == productId))
            card.IsFeatured = isFeatured;
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        RequestFailed?.Invoke(this, exception);
    }

    public sealed class ProductCardView : INotifyPropertyChanged
    {
        private bool _isFeatured;

        public ProductCardView(DiscoveryProduct product, string role, bool isFeatured = false)
        {
            Product = product;
            Name = product.Name;
            ImageUrl = product.ImageUrl;
            PriceText = product.PriceText(role);
            _isFeatured = isFeatured;
        }
        public DiscoveryProduct Product { get; }
        public string Name { get; }
        public string ImageUrl { get; }
        public string PriceText { get; }
        public bool IsFeatured
        {
            get => _isFeatured;
            set
            {
                if (_isFeatured == value)
                    return;
                _isFeatured = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFeatured)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeaturedButtonText)));
            }
        }
        public string FeaturedButtonText => IsFeatured ? "Remove featured" : "Feature product";
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class BrandProductGroup
    {
        public BrandProductGroup(string name, IEnumerable<ProductCardView> products)
        {
            Name = name;
            Products = new ObservableCollection<ProductCardView>(products);
        }
        public string Name { get; }
        public ObservableCollection<ProductCardView> Products { get; }
    }
}
