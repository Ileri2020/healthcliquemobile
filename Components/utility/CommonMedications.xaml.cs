using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CommonMedications : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, string> _featuredIds = [];
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(CommonMedications), string.Empty);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(CommonMedications), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CommonMedications), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(CommonMedications), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(CommonMedications), string.Empty);

    public CommonMedications()
    {
        InitializeComponent();
        Products = [];
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        AddToCartCommand = new Command<CommerceProduct>(AddToCart);
        ToggleFeaturedCommand = new Command<CommerceProduct>(async product => await ToggleFeaturedAsync(product), product => IsAdmin && !IsLoading);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<FeaturedProductView> Products { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand ToggleFeaturedCommand { get; }
    public event Action<object?, CommerceProduct>? AddToCartRequested;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CommonMedications)bindable;
        (control.RefreshCommand as Command)?.ChangeCanExecute();
        (control.ToggleFeaturedCommand as Command<CommerceProduct>)?.ChangeCanExecute();
    }

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CommonMedications)bindable).UpdateCommandAvailability();

    private void UpdateCommandAvailability() =>
        (ToggleFeaturedCommand as Command<CommerceProduct>)?.ChangeCanExecute();

    private async Task LoadAsync()
    {
        if (IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var productUri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=product&include=category&minimal=true&limit=50&requirePrice=true&requireImages=true");
            var featuredUri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=featuredProduct&minimal=true&limit=50");
            using var productsResponse = await UtilityApi.SendAsync(HttpMethod.Get, productUri, (object?)null);
            using var featuredResponse = await UtilityApi.SendAsync(HttpMethod.Get, featuredUri, (object?)null);
            var products = await productsResponse.Content.ReadFromJsonAsync<List<CommerceProduct>>(JsonOptions)
                ?? throw new JsonException("Product API returned an invalid response.");
            var featured = await featuredResponse.Content.ReadFromJsonAsync<List<CommerceFeaturedProduct>>(JsonOptions)
                ?? throw new JsonException("Featured product API returned an invalid response.");

            _featuredIds.Clear();
            foreach (var item in featured)
                _featuredIds[item.ProductId] = item.Id;
            Products.Clear();
            foreach (var product in products.Where(product => product.Price > 0 && product.Images.Length > 0).Take(15))
                Products.Add(new FeaturedProductView(product, _featuredIds.ContainsKey(product.Id)));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddToCart(CommerceProduct? product)
    {
        if (product is not null)
            AddToCartRequested?.Invoke(this, product);
    }

    private async Task ToggleFeaturedAsync(CommerceProduct? product)
    {
        if (!IsAdmin || product is null || IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            if (_featuredIds.TryGetValue(product.Id, out var featuredId))
            {
                var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=featuredProduct&id={Uri.EscapeDataString(featuredId)}");
                using var response = await UtilityApi.SendAsync(HttpMethod.Delete, uri, (object?)null);
                _featuredIds.Remove(product.Id);
                UpdateProductView(product, false);
            }
            else
            {
                var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=featuredProduct");
                using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new { productId = product.Id });
                var created = await response.Content.ReadFromJsonAsync<CommerceFeaturedProduct>(JsonOptions)
                    ?? throw new JsonException("Featured product API returned an invalid response.");
                _featuredIds[product.Id] = created.Id;
                UpdateProductView(product, true);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateProductView(CommerceProduct product, bool featured)
    {
        var index = -1;
        for (var itemIndex = 0; itemIndex < Products.Count; itemIndex++)
        {
            if (Products[itemIndex].Product.Id != product.Id)
                continue;
            index = itemIndex;
            break;
        }
        if (index >= 0)
            Products[index] = new FeaturedProductView(product, featured);
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    public sealed class FeaturedProductView
    {
        public FeaturedProductView(CommerceProduct product, bool featured)
        {
            Product = product;
            Name = product.Name;
            Images = product.Images;
            Category = product.Category;
            PriceText = $"₦{product.Price:N0}";
            IsFeatured = featured;
        }
        public CommerceProduct Product { get; }
        public string Name { get; }
        public string[] Images { get; }
        public CommerceCategory? Category { get; }
        public string PriceText { get; }
        public bool IsFeatured { get; }
        public string FeaturedButtonText => IsFeatured ? "Remove from deals" : "Add to deals";
    }
}
