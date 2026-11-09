using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class HeavilyDiscountedCarousel : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private List<DiscoveryProduct> _products = [];
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(HeavilyDiscountedCarousel), string.Empty, propertyChanged: OnSourceChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(HeavilyDiscountedCarousel), string.Empty, propertyChanged: OnSourceChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(HeavilyDiscountedCarousel), "visitor", propertyChanged: OnSourceChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty IsManageOpenProperty = BindableProperty.Create(nameof(IsManageOpen), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty ProductSearchProperty = BindableProperty.Create(nameof(ProductSearch), typeof(string), typeof(HeavilyDiscountedCarousel), string.Empty, BindingMode.TwoWay, propertyChanged: OnProductSearchChanged);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(HeavilyDiscountedCarousel), string.Empty);
    public static readonly BindableProperty HasDisplayItemsProperty = BindableProperty.Create(nameof(HasDisplayItems), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty ShouldShowProperty = BindableProperty.Create(nameof(ShouldShow), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty ShowEmptyMessageProperty = BindableProperty.Create(nameof(ShowEmptyMessage), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty IsProfessionalProperty = BindableProperty.Create(nameof(IsProfessional), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty CurrentProductPageProperty = BindableProperty.Create(nameof(CurrentProductPage), typeof(int), typeof(HeavilyDiscountedCarousel), 1);
    public static readonly BindableProperty ProductPageTextProperty = BindableProperty.Create(nameof(ProductPageText), typeof(string), typeof(HeavilyDiscountedCarousel), "1 / 1");
    public static readonly BindableProperty CanGoPreviousProperty = BindableProperty.Create(nameof(CanGoPrevious), typeof(bool), typeof(HeavilyDiscountedCarousel), false);
    public static readonly BindableProperty CanGoNextProperty = BindableProperty.Create(nameof(CanGoNext), typeof(bool), typeof(HeavilyDiscountedCarousel), false);

    public HeavilyDiscountedCarousel()
    {
        InitializeComponent();
        Items = [];
        RowOne = [];
        RowTwo = [];
        ProductOptions = [];
        MySubmissions = [];
        RefreshCommand = new Command(async () => await LoadAsync());
        NavigateCommand = new Command<string>(path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
                NavigationRequested?.Invoke(this, path);
        });
        OpenManageCommand = new Command(() => IsManageOpen = true);
        CloseManageCommand = new Command(() => IsManageOpen = false);
        AddProductCommand = new Command<DiscoveryProduct>(async product =>
        {
            if (product is not null)
                await AddProductAsync(product);
        });
        PreviousProductsCommand = new Command(() =>
        {
            CurrentProductPage = Math.Max(1, CurrentProductPage - 1);
            RefreshProductOptions();
        }, () => CanGoPrevious);
        NextProductsCommand = new Command(() =>
        {
            CurrentProductPage++;
            RefreshProductOptions();
        }, () => CanGoNext);
        ApproveCommand = new Command<HeavilyDiscountedProduct>(async item =>
        {
            if (item is not null)
                await SetApprovalAsync(item, !item.Approved);
        });
        DeleteCommand = new Command<HeavilyDiscountedProduct>(async item =>
        {
            if (item is not null)
                await DeleteAsync(item);
        });
        SelectProductCommand = new Command<DiscoveryProduct>(product =>
        {
            if (product is not null)
                ProductSelected?.Invoke(this, product);
        });
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool IsManageOpen { get => (bool)GetValue(IsManageOpenProperty); set => SetValue(IsManageOpenProperty, value); }
    public string ProductSearch { get => (string)GetValue(ProductSearchProperty); set => SetValue(ProductSearchProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasDisplayItems { get => (bool)GetValue(HasDisplayItemsProperty); private set => SetValue(HasDisplayItemsProperty, value); }
    public bool ShouldShow { get => (bool)GetValue(ShouldShowProperty); private set => SetValue(ShouldShowProperty, value); }
    public bool ShowEmptyMessage { get => (bool)GetValue(ShowEmptyMessageProperty); private set => SetValue(ShowEmptyMessageProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); private set => SetValue(IsAdminProperty, value); }
    public bool IsProfessional { get => (bool)GetValue(IsProfessionalProperty); private set => SetValue(IsProfessionalProperty, value); }
    public int CurrentProductPage { get => (int)GetValue(CurrentProductPageProperty); private set => SetValue(CurrentProductPageProperty, value); }
    public string ProductPageText { get => (string)GetValue(ProductPageTextProperty); private set => SetValue(ProductPageTextProperty, value); }
    public bool CanGoPrevious { get => (bool)GetValue(CanGoPreviousProperty); private set => SetValue(CanGoPreviousProperty, value); }
    public bool CanGoNext { get => (bool)GetValue(CanGoNextProperty); private set => SetValue(CanGoNextProperty, value); }
    public ObservableCollection<HeavilyDiscountedProduct> Items { get; }
    public ObservableCollection<HeavilyDiscountedProductCard> RowOne { get; }
    public ObservableCollection<HeavilyDiscountedProductCard> RowTwo { get; }
    public ObservableCollection<HeavilyDiscountedProductOption> ProductOptions { get; }
    public ObservableCollection<HeavilyDiscountedProductCard> MySubmissions { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand OpenManageCommand { get; }
    public ICommand CloseManageCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand PreviousProductsCommand { get; }
    public ICommand NextProductsCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectProductCommand { get; }
    public event Action<object?, string>? NavigationRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((HeavilyDiscountedCarousel)bindable).LoadAsync();

    private static void OnProductSearchChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeavilyDiscountedCarousel)bindable;
        control.CurrentProductPage = 1;
        control.RefreshProductOptions();
    }

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        UpdateRoleFlags();
        IsLoading = true;
        ShouldShow = true;
        HasStatus = false;
        try
        {
            var itemsTask = FetchAsync<List<HeavilyDiscountedProduct>>("api/heavily-discounted");
            var productsTask = FetchAsync<List<DiscoveryProduct>>("api/dbhandler?model=product&minimal=true");
            await Task.WhenAll(itemsTask, productsTask);
            var items = await itemsTask;
            _products = await productsTask;
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
            RefreshRows();
            RefreshProductOptions();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            RefreshRows();
            RefreshProductOptions();
        }
    }

    private async Task<T> FetchAsync<T>(string path)
    {
        using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new JsonException($"API returned an invalid response for {path}.");
    }

    private void UpdateRoleFlags()
    {
        IsAdmin = UserRole.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || UserRole.Equals("staff", StringComparison.OrdinalIgnoreCase);
        IsProfessional = IsAdmin || UserRole.Equals("professional", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshRows()
    {
        var visible = Items.Where(item => item.Approved || IsAdmin || item.CreatorId == UserId).ToList();
        var cards = visible.Select(item => new HeavilyDiscountedProductCard(item, UserRole, UserId)).ToList();
        RowOne.Clear();
        RowTwo.Clear();
        for (var index = 0; index < cards.Count; index++)
        {
            if (index % 2 == 0)
                RowOne.Add(cards[index]);
            else
                RowTwo.Insert(0, cards[index]);
        }
        HasDisplayItems = cards.Count > 0;
        MySubmissions.Clear();
        foreach (var card in cards.Where(card => IsAdmin || card.IsCreator))
            MySubmissions.Add(card);
        ShowEmptyMessage = !IsLoading && cards.Count == 0 && IsProfessional;
        ShouldShow = IsLoading || cards.Count > 0 || IsProfessional;
    }

    private void RefreshProductOptions()
    {
        var matches = _products
            .Where(product => product.Name.Contains(ProductSearch.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        var pageCount = Math.Max(1, (int)Math.Ceiling(matches.Count / 8d));
        CurrentProductPage = Math.Clamp(CurrentProductPage, 1, pageCount);
        ProductPageText = $"{CurrentProductPage} / {pageCount}";
        CanGoPrevious = CurrentProductPage > 1;
        CanGoNext = CurrentProductPage < pageCount;
        ProductOptions.Clear();
        foreach (var product in matches.Skip((CurrentProductPage - 1) * 8).Take(8))
            ProductOptions.Add(new HeavilyDiscountedProductOption(product, Items.Any(item => item.ProductId == product.Id)));
        (PreviousProductsCommand as Command)?.ChangeCanExecute();
        (NextProductsCommand as Command)?.ChangeCanExecute();
    }

    private async Task AddProductAsync(DiscoveryProduct product)
    {
        if (!IsProfessional || string.IsNullOrWhiteSpace(UserId))
        {
            SetError(new InvalidOperationException("Sign in as a professional to submit products for rapid sale."));
            return;
        }
        if (Items.Any(item => item.ProductId == product.Id))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/heavily-discounted"), new { productId = product.Id });
            IsLoading = false;
            await LoadAsync();
            StatusMessage = $"{product.Name} added for review.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            RefreshRows();
        }
    }

    private async Task SetApprovalAsync(HeavilyDiscountedProduct item, bool approved)
    {
        if (!IsAdmin)
            return;
        IsLoading = true;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, UtilityApi.CreateUri(ApiBaseUrl, "api/heavily-discounted"), new { id = item.Id, approved });
            item.Approved = approved;
            IsLoading = false;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            RefreshRows();
        }
    }

    private async Task DeleteAsync(HeavilyDiscountedProduct item)
    {
        if (!IsAdmin && item.CreatorId != UserId)
            return;
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null && !await page.DisplayAlertAsync("Remove rapid sale", "Remove this product from rapid sale?", "Remove", "Cancel"))
            return;
        IsLoading = true;
        try
        {
            var path = $"api/heavily-discounted?id={Uri.EscapeDataString(item.Id)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
            IsLoading = false;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            RefreshRows();
        }
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        RequestFailed?.Invoke(this, exception);
    }
}
