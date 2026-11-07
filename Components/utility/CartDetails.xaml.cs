using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CartDetails : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private CommerceCart? _cart;
    private CartClient.CheckoutResponse? _checkout;
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty CartIdProperty = BindableProperty.Create(nameof(CartId), typeof(string), typeof(CartDetails), string.Empty, propertyChanged: OnCartIdChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(CartDetails), "customer", propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(CartDetails), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty SelectedAddressProperty = BindableProperty.Create(nameof(SelectedAddress), typeof(CommerceShippingAddress), typeof(CartDetails), null, BindingMode.TwoWay, propertyChanged: OnAddressChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty HasCartProperty = BindableProperty.Create(nameof(HasCart), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty HasPrescriptionProperty = BindableProperty.Create(nameof(HasPrescription), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty CanCheckoutProperty = BindableProperty.Create(nameof(CanCheckout), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty HasCheckoutProperty = BindableProperty.Create(nameof(HasCheckout), typeof(bool), typeof(CartDetails), false);
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty CartNameProperty = BindableProperty.Create(nameof(CartName), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty PrescriptionUrlProperty = BindableProperty.Create(nameof(PrescriptionUrl), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty PharmacistSummaryProperty = BindableProperty.Create(nameof(PharmacistSummary), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty SubtotalTextProperty = BindableProperty.Create(nameof(SubtotalText), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty DeliveryTextProperty = BindableProperty.Create(nameof(DeliveryText), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty TotalTextProperty = BindableProperty.Create(nameof(TotalText), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty CheckoutSummaryProperty = BindableProperty.Create(nameof(CheckoutSummary), typeof(string), typeof(CartDetails), string.Empty);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(CartDetails), string.Empty);

    public CartDetails()
    {
        InitializeComponent();
        Items = [];
        Addresses = [];
        SaveNameCommand = new Command(async () => await SaveNameAsync(), () => HasCart && !IsLoading);
        SaveSummaryCommand = new Command(async () => await SaveSummaryAsync(), () => HasCart && IsAdmin && !IsLoading);
        CheckoutCommand = new Command(async () => await CheckoutAsync(), () => CanCheckout && !IsLoading);
        ConfirmPaymentCommand = new Command(async () => await ConfirmPaymentAsync(), () => HasCheckout && !IsLoading);
        Loaded += async (_, _) => await LoadCartAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string CartId { get => (string)GetValue(CartIdProperty); set => SetValue(CartIdProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public CommerceShippingAddress? SelectedAddress { get => (CommerceShippingAddress?)GetValue(SelectedAddressProperty); set => SetValue(SelectedAddressProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasCart { get => (bool)GetValue(HasCartProperty); private set => SetValue(HasCartProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public bool HasPrescription { get => (bool)GetValue(HasPrescriptionProperty); private set => SetValue(HasPrescriptionProperty, value); }
    public bool CanCheckout { get => (bool)GetValue(CanCheckoutProperty); private set => SetValue(CanCheckoutProperty, value); }
    public bool HasCheckout { get => (bool)GetValue(HasCheckoutProperty); private set => SetValue(HasCheckoutProperty, value); }
    public string Status { get => (string)GetValue(StatusProperty); private set => SetValue(StatusProperty, value); }
    public string CartName { get => (string)GetValue(CartNameProperty); set => SetValue(CartNameProperty, value); }
    public string PrescriptionUrl { get => (string)GetValue(PrescriptionUrlProperty); private set => SetValue(PrescriptionUrlProperty, value); }
    public string PharmacistSummary { get => (string)GetValue(PharmacistSummaryProperty); set => SetValue(PharmacistSummaryProperty, value); }
    public string SubtotalText { get => (string)GetValue(SubtotalTextProperty); private set => SetValue(SubtotalTextProperty, value); }
    public string DeliveryText { get => (string)GetValue(DeliveryTextProperty); private set => SetValue(DeliveryTextProperty, value); }
    public string TotalText { get => (string)GetValue(TotalTextProperty); private set => SetValue(TotalTextProperty, value); }
    public string CheckoutSummary { get => (string)GetValue(CheckoutSummaryProperty); private set => SetValue(CheckoutSummaryProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<CartDetailItem> Items { get; }
    public ObservableCollection<CommerceShippingAddress> Addresses { get; }
    public ICommand SaveNameCommand { get; }
    public ICommand SaveSummaryCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ConfirmPaymentCommand { get; }
    public event Action<object?, CartClient.CheckoutResponse>? CheckoutReady;
    public event EventHandler? PaymentConfirmed;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnCartIdChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((CartDetails)bindable).LoadCartAsync();

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CartDetails)bindable;
        control.IsAdmin = string.Equals(control.UserRole, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(control.UserRole, "staff", StringComparison.OrdinalIgnoreCase)
            || control.IsAdmin;
    }

    private static void OnAddressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CartDetails)bindable;
        control.CanCheckout = !control.IsLoading && control._cart is not null
            && !IsLocked(control._cart) && !string.IsNullOrWhiteSpace(control.UserId)
            && control.SelectedAddress is not null;
        (control.CheckoutCommand as Command)?.ChangeCanExecute();
    }

    private async Task LoadCartAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(CartId) || IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=cart&id={Uri.EscapeDataString(CartId)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            _cart = await response.Content.ReadFromJsonAsync<CommerceCart>(JsonOptions)
                ?? throw new JsonException("Cart API returned an invalid response.");
            Addresses.Clear();
            foreach (var address in _cart.User?.Addresses ?? [])
                Addresses.Add(address);
            SelectedAddress = Addresses.FirstOrDefault();
            CartName = _cart.Name ?? string.Empty;
            Status = _cart.Status;
            PrescriptionUrl = _cart.PrescriptionUrl;
            HasPrescription = !string.IsNullOrWhiteSpace(PrescriptionUrl);
            PharmacistSummary = _cart.PharmacistSummary ?? string.Empty;
            Items.Clear();
            foreach (var item in _cart.Products)
                Items.Add(new CartDetailItem(item));
            UpdateTotals();
            HasCart = true;
            CanCheckout = !IsLocked(_cart) && !string.IsNullOrWhiteSpace(UserId) && SelectedAddress is not null;
            (CheckoutCommand as Command)?.ChangeCanExecute();
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

    private async Task SaveNameAsync()
    {
        if (_cart is null || string.IsNullOrWhiteSpace(CartName))
            return;
        await UpdateCartAsync(new { name = CartName }, "Cart name updated.");
    }

    private async Task SaveSummaryAsync()
    {
        if (_cart is null || !IsAdmin)
            return;
        await UpdateCartAsync(new { pharmacistSummary = PharmacistSummary }, "Pharmacist summary saved.");
    }

    private async Task UpdateCartAsync(object changes, string successMessage)
    {
        if (_cart is null)
            return;
        IsLoading = true;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=cart&id={Uri.EscapeDataString(_cart.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, uri, changes);
            if (changes is not null)
            {
                if (successMessage.StartsWith("Cart name", StringComparison.Ordinal))
                    _cart.Name = CartName;
                else
                    _cart.PharmacistSummary = PharmacistSummary;
            }
            SetStatus(successMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckoutAsync()
    {
        if (_cart is null || string.IsNullOrWhiteSpace(UserId) || SelectedAddress is null)
            return;
        IsLoading = true;
        try
        {
            var payload = new
            {
                userId = UserId,
                cartId = _cart.Id,
                items = _cart.Products.Select(item => new
                {
                    productId = item.Product?.Id,
                    quantity = item.Quantity,
                    customName = item.CustomName,
                    customPrice = item.CustomPrice,
                    isSpecial = item.IsSpecial
                }).ToArray(),
                deliveryFee = _cart.DeliveryFee ?? 0,
                deliveryAddressId = SelectedAddress.Id,
                couponCode = (string?)null,
                discountAmount = _cart.DiscountAmount
            };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/payment"), payload);
            _checkout = await response.Content.ReadFromJsonAsync<CartClient.CheckoutResponse>(JsonOptions)
                ?? throw new JsonException("Checkout API returned an invalid response.");
            CheckoutSummary = $"Payment reference: {_checkout.TxRef}; amount ₦{_checkout.Amount:N0}.";
            HasCheckout = true;
            CheckoutReady?.Invoke(this, _checkout);
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

    private async Task ConfirmPaymentAsync()
    {
        if (_checkout is null)
            return;
        IsLoading = true;
        try
        {
            var payload = new { tx_ref = _checkout.TxRef, method = "monnify" };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/payment?action=confirm"), payload);
            var result = await response.Content.ReadFromJsonAsync<PaymentConfirmation>(JsonOptions);
            if (result?.Success != true)
                throw new InvalidOperationException(result?.Message ?? "The payment was not confirmed.");
            _checkout = null;
            HasCheckout = false;
            IsLoading = false;
            await LoadCartAsync();
            PaymentConfirmed?.Invoke(this, EventArgs.Empty);
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

    private void UpdateTotals()
    {
        var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
        SubtotalText = $"₦{subtotal:N0}";
        DeliveryText = $"₦{_cart?.DeliveryFee ?? 0:N0}";
        TotalText = $"₦{_cart?.Total ?? subtotal:N0}";
    }

    private static bool IsLocked(CommerceCart cart) =>
        cart.Status is "paid" or "completed" or "unconfirmed" || cart.Payment is not null;

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    public sealed class CartDetailItem
    {
        public CartDetailItem(CommerceCartItem item)
        {
            Item = item;
            DisplayName = item.IsSpecial ? item.CustomName ?? item.Name : item.Product?.Name ?? item.CustomName ?? "Unnamed product";
            var price = item.CustomPrice ?? item.Product?.Price ?? item.Price;
            UnitPrice = price;
            LineTotalText = $"₦{(price * item.Quantity):N0}";
        }
        public CommerceCartItem Item { get; }
        public string DisplayName { get; }
        public double UnitPrice { get; }
        public string LineTotalText { get; }
        public int Quantity => Item.Quantity;
    }

    private sealed class PaymentConfirmation
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
