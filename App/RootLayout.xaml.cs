using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.Utility;
using Microsoft.Maui.ApplicationModel;

namespace EEE.ClientApp;

[ContentProperty(nameof(PageContent))]
public partial class RootLayout : ContentView
{
    public static readonly BindableProperty PageContentProperty =
        BindableProperty.Create(nameof(PageContent), typeof(View), typeof(RootLayout));
    public static readonly BindableProperty ApiBaseUrlProperty =
        BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(RootLayout), "https://healthcliquecare.com/");
    public static readonly BindableProperty CurrentPathProperty =
        BindableProperty.Create(nameof(CurrentPath), typeof(string), typeof(RootLayout), "/",
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is RootLayout layout && layout.SideNavigationItems is not null)
                    layout.UpdateNavigationItems();
            });
    public static readonly BindableProperty IsCartOpenProperty =
        BindableProperty.Create(nameof(IsCartOpen), typeof(bool), typeof(RootLayout), false);
    private bool _navigationEventsSubscribed;

    public RootLayout()
    {
        InitializeComponent();
        SideNavigationItems = [];
        PageSidenav.Items = SideNavigationItems;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        CartItems = CommerceCartState.Shared.Items;
        OpenCartCommand = new Command(() => IsCartOpen = true);
        CloseCartCommand = new Command(() => IsCartOpen = false);
        RemoveCartItemCommand = new Command<CommerceCartItem>(item =>
        {
            if (item is not null)
                CommerceCartState.Shared.Remove(item);
        });
        CheckoutCommand = new Command(async () => await NavigateToAsync("/cart"));
        CommerceCartState.Shared.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(CommerceCartState.ItemCount) or nameof(CommerceCartState.TotalText))
            {
                OnPropertyChanged(nameof(CartButtonText));
                OnPropertyChanged(nameof(CartTotalText));
                OnPropertyChanged(nameof(HasCartItems));
                OnPropertyChanged(nameof(IsCartEmpty));
            }
        };
        PageSidenav.NavigationFailed += (_, exception) => NavigationFailed?.Invoke(this, exception);
        PageFooter.NavigationRequested += async (_, path) => await NavigateToAsync(path);
        PageFooter.CategorySelected += (_, category) => CategorySelected?.Invoke(this, category);
        PageFooter.RequestFailed += (_, exception) => RequestFailed?.Invoke(this, exception);
    }

    public View? PageContent { get => (View?)GetValue(PageContentProperty); set => SetValue(PageContentProperty, value); }
    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string CurrentPath { get => (string)GetValue(CurrentPathProperty); set => SetValue(CurrentPathProperty, value); }
    public bool IsCartOpen { get => (bool)GetValue(IsCartOpenProperty); set => SetValue(IsCartOpenProperty, value); }
    public ObservableCollection<CommerceCartItem> CartItems { get; }
    public string CartButtonText => $"Cart ({CommerceCartState.Shared.ItemCount})";
    public string CartTotalText => CommerceCartState.Shared.TotalText;
    public bool HasCartItems => CartItems.Count > 0;
    public bool IsCartEmpty => !HasCartItems;
    public ICommand OpenCartCommand { get; }
    public ICommand CloseCartCommand { get; }
    public ICommand RemoveCartItemCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ObservableCollection<UtilityNavigationItem> SideNavigationItems { get; }
    public event Action<object?, string>? NavigationRequested;
    public event Action<object?, DiscoveryCategory>? CategorySelected;
    public event Action<object?, Exception>? NavigationFailed;
    public event Action<object?, Exception>? RequestFailed;

    private async void OnLoaded(object? sender, EventArgs args)
    {
        if (!_navigationEventsSubscribed)
        {
            CommerceSessionState.CurrentUserChanged += OnCurrentUserChanged;
            _navigationEventsSubscribed = true;
        }

        UpdateNavigationItems();
        try
        {
            CommerceSessionState.CurrentUser = await UtilityApi.GetSignedInUserAsync(ApiBaseUrl);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            NavigationFailed?.Invoke(this, exception);
        }
    }

    private void OnUnloaded(object? sender, EventArgs args)
    {
        if (!_navigationEventsSubscribed)
            return;
        CommerceSessionState.CurrentUserChanged -= OnCurrentUserChanged;
        _navigationEventsSubscribed = false;
    }

    private void OnCurrentUserChanged() =>
        MainThread.BeginInvokeOnMainThread(UpdateNavigationItems);

    private void UpdateNavigationItems()
    {
        SideNavigationItems.Clear();
        var role = CommerceSessionState.CurrentUser?.Role?.Trim().ToLowerInvariant();
        var isAdmin = role == "admin";
        var hasInventoryAccess = isAdmin || role == "staff";
        var isInventoryPage = CurrentPath.StartsWith("/stock", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/sales", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/count", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/expenses", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/vitals", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/staff", StringComparison.OrdinalIgnoreCase)
            || CurrentPath.StartsWith("/inventory/", StringComparison.OrdinalIgnoreCase);

        if (isInventoryPage && hasInventoryAccess)
        {
            AddNavigation("/stock", "Stock");
            AddNavigation("/sales", "Sales");
            AddNavigation("/count", "Stock count");
            AddNavigation("/stock/products", "Products");
            AddNavigation("/stock/count", "Stock count entry");
            AddNavigation("/stock/count/all", "Stock count history");
            AddNavigation("/expenses", "Expenses");
            AddNavigation("/contact", "Contact / support");
            AddNavigation("/vitals", "Vitals");
            AddNavigation("/staff", "Register a customer");
            AddNavigation("/sales/all", "All sales");
            AddNavigation("/stock/all", "All stock");
            AddNavigation("/count/all", "Count history");
            AddNavigation("/inventory/analytics", "Inventory analytics");
            AddNavigation("/home", "Home");
            AddNavigation("/account", "My account");
            if (isAdmin)
            {
                AddNavigation("/admin", "Administration");
                AddNavigation("/admin/analytics", "Store analytics");
                AddNavigation("/sheet", "Data sheet");
            }
            return;
        }

        AddNavigation("/home", "Home");
        AddNavigation("/store", "Store");
        AddNavigation("/blog", "Clinical insights");
        AddNavigation("/cart", "Cart");
        AddNavigation("/contact", "Contact");
        AddNavigation("/account", "My account");
        AddNavigation("/about", "About");
        AddNavigation("/help", "Help");
        AddNavigation("/lunch", "Lunch");
        AddNavigation("/privacy", "Privacy");
        AddNavigation("/terms", "Terms");
        AddNavigation("/myui", "UI gallery");

        if (CommerceSessionState.CurrentUser is null)
        {
            AddNavigation("/login", "Sign in");
            AddNavigation("/signup", "Create account");
        }

        if (hasInventoryAccess)
        {
            AddNavigation("/stock", "Stock");
            AddNavigation("/sales", "Sales");
            AddNavigation("/count", "Stock count");
            AddNavigation("/stock/count", "Stock count entry");
            AddNavigation("/count/all", "Count history");
            AddNavigation("/expenses", "Expenses");
            AddNavigation("/contact", "Contact / support");
            AddNavigation("/vitals", "Vitals");
            AddNavigation("/staff", "Register a customer");
            AddNavigation("/sales/all", "All sales");
            AddNavigation("/stock/products", "Products");
            AddNavigation("/stock/all", "All stock");
            AddNavigation("/stock/count/all", "Stock count history");
            AddNavigation("/inventory/analytics", "Inventory analytics");
        }
        if (isAdmin)
        {
            AddNavigation("/admin", "Administration");
            AddNavigation("/admin/analytics", "Store analytics");
            AddNavigation("/sheet", "Data sheet");
        }
    }

    private void AddNavigation(string route, string title) =>
        SideNavigationItems.Add(CreateNavigationItem(route, title));

    public async Task NavigateToAsync(string path)
    {
        if (path.IndexOf(':') > 0 && Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is not ("http" or "https" or "mailto"))
            {
                var exception = new InvalidOperationException($"Navigation to the '{uri.Scheme}' URI scheme is not supported.");
                NavigationFailed?.Invoke(this, exception);
                return;
            }

            try
            {
                await Launcher.Default.OpenAsync(uri);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FeatureNotSupportedException or UriFormatException)
            {
                NavigationFailed?.Invoke(this, exception);
            }
            return;
        }

        var separator = path.IndexOf('?');
        var route = (separator >= 0 ? path[..separator] : path).Trim('/');
        var query = separator >= 0 ? path[(separator + 1)..] : string.Empty;
        NavigationRequested?.Invoke(this, path);
        if (route.Length == 0 || route.Equals("home", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateShellAsync("home");
            return;
        }

        if (route.Equals("store", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateShellAsync(string.IsNullOrWhiteSpace(query) ? "store" : $"store?{query}");
            return;
        }

        if (route.StartsWith("store/", StringComparison.OrdinalIgnoreCase))
        {
            var productId = route["store/".Length..];
            await NavigateShellAsync($"product?id={Uri.EscapeDataString(Uri.UnescapeDataString(productId))}");
            return;
        }

        if (route.Equals("account", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateShellAsync("account");
            return;
        }

        if (route.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateManagementPageAsync("admin", string.Empty, query);
            return;
        }

        if (route.Equals("admin/analytics", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateManagementPageAsync("analytics", string.Empty, query);
            return;
        }

        if (route.Equals("inventory/analytics", StringComparison.OrdinalIgnoreCase))
        {
            var analyticsQuery = string.IsNullOrWhiteSpace(query) ? "mode=inventory" : $"{query}&mode=inventory";
            await NavigateManagementPageAsync("analytics", string.Empty, analyticsQuery);
            return;
        }

        if (route.Equals("login", StringComparison.OrdinalIgnoreCase)
            || route.Equals("signup", StringComparison.OrdinalIgnoreCase)
            || route.Equals("staff", StringComparison.OrdinalIgnoreCase)
            || route.Equals("vitals", StringComparison.OrdinalIgnoreCase)
            || route.Equals("sheet", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateManagementPageAsync(route, string.Empty, query);
            return;
        }

        if (route.Equals("about", StringComparison.OrdinalIgnoreCase)
            || route.Equals("help", StringComparison.OrdinalIgnoreCase)
            || route.Equals("lunch", StringComparison.OrdinalIgnoreCase)
            || route.Equals("privacy", StringComparison.OrdinalIgnoreCase)
            || route.Equals("terms", StringComparison.OrdinalIgnoreCase)
            || route.Equals("myui", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateManagementPageAsync(route, string.Empty, query);
            return;
        }

        if (route.Equals("blog", StringComparison.OrdinalIgnoreCase)
            || route.Equals("cart", StringComparison.OrdinalIgnoreCase)
            || route.Equals("contact", StringComparison.OrdinalIgnoreCase))
        {
            await NavigateManagementPageAsync(route, string.Empty, query);
            return;
        }

        if (route.Equals("count", StringComparison.OrdinalIgnoreCase)
            || route.StartsWith("count/", StringComparison.OrdinalIgnoreCase))
        {
            var subPath = route.Length > "count".Length ? route["count/".Length..] : string.Empty;
            await NavigateManagementPageAsync("count", subPath, query);
            return;
        }

        if (route.Equals("expenses", StringComparison.OrdinalIgnoreCase)
            || route.StartsWith("expenses/", StringComparison.OrdinalIgnoreCase))
        {
            var subPath = route.Length > "expenses".Length ? route["expenses/".Length..] : string.Empty;
            await NavigateManagementPageAsync("expenses", subPath, query);
            return;
        }

        if (route.Equals("sales", StringComparison.OrdinalIgnoreCase)
            || route.StartsWith("sales/", StringComparison.OrdinalIgnoreCase))
        {
            var subPath = route.Length > "sales".Length ? route["sales/".Length..] : string.Empty;
            await NavigateManagementPageAsync("sales", subPath, query);
            return;
        }

        if (route.Equals("stock", StringComparison.OrdinalIgnoreCase)
            || route.StartsWith("stock/", StringComparison.OrdinalIgnoreCase))
        {
            var subPath = route.Length > "stock".Length ? route["stock/".Length..] : string.Empty;
            await NavigateManagementPageAsync("stock", subPath, query);
            return;
        }

        if (route.StartsWith("products/", StringComparison.OrdinalIgnoreCase))
        {
            var productId = route["products/".Length..];
            await NavigateShellAsync($"product?id={Uri.EscapeDataString(Uri.UnescapeDataString(productId))}");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
                throw new InvalidOperationException("Set ApiBaseUrl before opening a web page.");
            var baseUri = UtilityApi.CreateUri(ApiBaseUrl, string.Empty);
            var pageUri = new Uri(baseUri, path.TrimStart('/'));
            await Launcher.Default.OpenAsync(pageUri);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or UriFormatException or FeatureNotSupportedException)
        {
            NavigationFailed?.Invoke(this, exception);
        }
    }

    private async Task NavigateShellAsync(string route)
    {
        try
        {
            if (Shell.Current is null)
                throw new InvalidOperationException("Shell navigation is unavailable.");
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            NavigationFailed?.Invoke(this, exception);
        }
    }

    private async Task NavigateManagementPageAsync(string shellRoute, string subPath, string query)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(subPath))
            parameters.Add($"path={Uri.EscapeDataString(Uri.UnescapeDataString(subPath))}");
        if (!string.IsNullOrWhiteSpace(query))
            parameters.Add(query);
        await NavigateShellAsync(parameters.Count == 0 ? shellRoute : $"{shellRoute}?{string.Join("&", parameters)}");
    }

    private UtilityNavigationItem CreateNavigationItem(string route, string title) =>
        new()
        {
            Name = title,
            Title = title,
            Route = route,
            Command = new Command(async () => await NavigateToAsync(route))
        };
}
