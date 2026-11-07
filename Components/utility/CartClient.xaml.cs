using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CartClient : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(CartClient), string.Empty);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(CartClient), string.Empty, propertyChanged: OnUserChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(CartClient), "customer", propertyChanged: OnTotalsChanged);
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(ObservableCollection<CommerceCartItem>), typeof(CartClient), defaultValueCreator: _ => new ObservableCollection<CommerceCartItem>(), propertyChanged: OnItemsChanged);
    public static readonly BindableProperty SelectedAddressProperty = BindableProperty.Create(nameof(SelectedAddress), typeof(CommerceShippingAddress), typeof(CartClient), null, BindingMode.TwoWay, propertyChanged: OnAddressChanged);
    public static readonly BindableProperty IncludeDeliveryProperty = BindableProperty.Create(nameof(IncludeDelivery), typeof(bool), typeof(CartClient), true, propertyChanged: OnTotalsChanged);
    public static readonly BindableProperty TermsAcceptedProperty = BindableProperty.Create(nameof(TermsAccepted), typeof(bool), typeof(CartClient), false, propertyChanged: OnTotalsChanged);
    public static readonly BindableProperty CouponCodeProperty = BindableProperty.Create(nameof(CouponCode), typeof(string), typeof(CartClient), string.Empty);
    public static readonly BindableProperty ItemCountProperty = BindableProperty.Create(nameof(ItemCount), typeof(int), typeof(CartClient), 0);
    public static readonly BindableProperty HasItemsProperty = BindableProperty.Create(nameof(HasItems), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty IsEmptyProperty = BindableProperty.Create(nameof(IsEmpty), typeof(bool), typeof(CartClient), true);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty CanCheckoutProperty = BindableProperty.Create(nameof(CanCheckout), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty HasCheckoutProperty = BindableProperty.Create(nameof(HasCheckout), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty HasCouponStatusProperty = BindableProperty.Create(nameof(HasCouponStatus), typeof(bool), typeof(CartClient), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(CartClient), string.Empty);
    public static readonly BindableProperty CouponStatusProperty = BindableProperty.Create(nameof(CouponStatus), typeof(string), typeof(CartClient), string.Empty);
    public static readonly BindableProperty SubtotalTextProperty = BindableProperty.Create(nameof(SubtotalText), typeof(string), typeof(CartClient), "₦0");
    public static readonly BindableProperty DiscountTextProperty = BindableProperty.Create(nameof(DiscountText), typeof(string), typeof(CartClient), "₦0");
    public static readonly BindableProperty DeliveryTextProperty = BindableProperty.Create(nameof(DeliveryText), typeof(string), typeof(CartClient), "₦0");
    public static readonly BindableProperty TotalTextProperty = BindableProperty.Create(nameof(TotalText), typeof(string), typeof(CartClient), "₦0");
    public static readonly BindableProperty CheckoutSummaryProperty = BindableProperty.Create(nameof(CheckoutSummary), typeof(string), typeof(CartClient), string.Empty);

    private double _deliveryFee;
    private CouponInfo? _coupon;
    private CheckoutResponse? _checkout;
    private bool _changingItems;

    public CartClient()
    {
        InitializeComponent();
        Addresses = [];
        CheckoutCommand = new Command(async () => await CheckoutAsync(), () => CanCheckout && !IsLoading);
        ApplyCouponCommand = new Command(async () => await ApplyCouponAsync(), () => !IsLoading);
        ClearCommand = new Command(Clear);
        IncreaseCommand = new Command<CommerceCartItem>(item => ChangeQuantity(item, 1));
        DecreaseCommand = new Command<CommerceCartItem>(item => ChangeQuantity(item, -1));
        RemoveItemCommand = new Command<CommerceCartItem>(item => { if (item is not null) Items.Remove(item); });
        ConfirmPaymentCommand = new Command(async () => await ConfirmPaymentAsync(), () => HasCheckout && !IsLoading);
        RefreshTotals();
        Loaded += async (_, _) => await LoadAddressesAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public ObservableCollection<CommerceCartItem> Items { get => (ObservableCollection<CommerceCartItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public ObservableCollection<CommerceShippingAddress> Addresses { get; }
    public CommerceShippingAddress? SelectedAddress { get => (CommerceShippingAddress?)GetValue(SelectedAddressProperty); set => SetValue(SelectedAddressProperty, value); }
    public bool IncludeDelivery { get => (bool)GetValue(IncludeDeliveryProperty); set => SetValue(IncludeDeliveryProperty, value); }
    public bool TermsAccepted { get => (bool)GetValue(TermsAcceptedProperty); set => SetValue(TermsAcceptedProperty, value); }
    public string CouponCode { get => (string)GetValue(CouponCodeProperty); set => SetValue(CouponCodeProperty, value); }
    public int ItemCount { get => (int)GetValue(ItemCountProperty); private set => SetValue(ItemCountProperty, value); }
    public bool HasItems { get => (bool)GetValue(HasItemsProperty); private set => SetValue(HasItemsProperty, value); }
    public bool IsEmpty { get => (bool)GetValue(IsEmptyProperty); private set => SetValue(IsEmptyProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanCheckout { get => (bool)GetValue(CanCheckoutProperty); private set => SetValue(CanCheckoutProperty, value); }
    public bool HasCheckout { get => (bool)GetValue(HasCheckoutProperty); private set => SetValue(HasCheckoutProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public bool HasCouponStatus { get => (bool)GetValue(HasCouponStatusProperty); private set => SetValue(HasCouponStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string CouponStatus { get => (string)GetValue(CouponStatusProperty); private set => SetValue(CouponStatusProperty, value); }
    public string SubtotalText { get => (string)GetValue(SubtotalTextProperty); private set => SetValue(SubtotalTextProperty, value); }
    public string DiscountText { get => (string)GetValue(DiscountTextProperty); private set => SetValue(DiscountTextProperty, value); }
    public string DeliveryText { get => (string)GetValue(DeliveryTextProperty); private set => SetValue(DeliveryTextProperty, value); }
    public string TotalText { get => (string)GetValue(TotalTextProperty); private set => SetValue(TotalTextProperty, value); }
    public string CheckoutSummary { get => (string)GetValue(CheckoutSummaryProperty); private set => SetValue(CheckoutSummaryProperty, value); }
    public ICommand CheckoutCommand { get; }
    public ICommand ApplyCouponCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ConfirmPaymentCommand { get; }
    public ICommand IncreaseCommand { get; }
    public ICommand DecreaseCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public event Action<object?, CheckoutResponse>? CheckoutReady;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnUserChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((CartClient)bindable).LoadAddressesAsync();

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CartClient)bindable;
        if (oldValue is ObservableCollection<CommerceCartItem> oldItems)
            oldItems.CollectionChanged -= control.OnCartItemsChanged;
        if (newValue is ObservableCollection<CommerceCartItem> newItems)
            newItems.CollectionChanged += control.OnCartItemsChanged;
        control.RefreshTotals();
    }

    private static void OnTotalsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CartClient)bindable).RefreshTotals();

    private static void OnAddressChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((CartClient)bindable).LoadDeliveryFeeAsync();

    private void OnCartItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (CommerceCartItem item in e.OldItems)
                item.PropertyChanged -= OnCartItemPropertyChanged;
        if (e.NewItems is not null)
            foreach (CommerceCartItem item in e.NewItems)
                item.PropertyChanged += OnCartItemPropertyChanged;
        RefreshTotals();
    }

    private void OnCartItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshTotals();

    private void ChangeQuantity(CommerceCartItem? item, int amount)
    {
        if (item is null)
            return;
        item.Quantity = Math.Max(1, item.Quantity + amount);
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        if (Items is null)
            return;
        var multiplier = RoleMarkup(UserRole);
        var subtotal = Items.Sum(item => (item.IsSpecial ? item.CustomPrice ?? item.Price : item.BulkPriceId is not null
            ? item.Product?.BulkPrices.FirstOrDefault(price => price.Id == item.BulkPriceId)?.Price ?? item.Price
            : item.Price) * (item.IsSpecial ? 1 : multiplier) * item.Quantity);
        subtotal = Math.Ceiling(subtotal / 5) * 5;
        var discount = _coupon is null ? 0 : _coupon.Type == "percentage" ? subtotal * _coupon.Discount / 100 : _coupon.Discount;
        discount = Math.Ceiling(discount / 5) * 5;
        var delivery = IncludeDelivery ? Math.Ceiling(_deliveryFee / 5) * 5 : 0;
        var total = Math.Max(0, subtotal - discount + delivery);

        _changingItems = true;
        ItemCount = Items.Sum(item => Math.Max(0, item.Quantity));
        _changingItems = false;
        HasItems = Items.Count > 0;
        IsEmpty = !HasItems;
        SubtotalText = $"₦{subtotal:N0}";
        DiscountText = $"−₦{discount:N0}";
        DeliveryText = $"₦{delivery:N0}";
        TotalText = $"₦{total:N0}";
        CanCheckout = HasItems && !string.IsNullOrWhiteSpace(UserId) && UserId != "nil" && SelectedAddress is not null && TermsAccepted;
        (CheckoutCommand as Command)?.ChangeCanExecute();
    }

    private async Task LoadAddressesAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId) || UserId == "nil" || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=shippingAddress&userId={Uri.EscapeDataString(UserId)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var addresses = await response.Content.ReadFromJsonAsync<List<CommerceShippingAddress>>(JsonOptions);
            if (addresses is null)
                throw new JsonException("Address API returned an invalid response.");
            Addresses.Clear();
            foreach (var address in addresses)
                Addresses.Add(address);
            SelectedAddress ??= Addresses.FirstOrDefault();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
    }

    private async Task LoadDeliveryFeeAsync()
    {
        if (SelectedAddress is null || string.IsNullOrWhiteSpace(SelectedAddress.State) || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        try
        {
            var state = Uri.EscapeDataString(SelectedAddress.State);
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=deliveryFee&state={state}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var fees = await response.Content.ReadFromJsonAsync<List<DeliveryFeeInfo>>(JsonOptions);
            var fee = fees?.FirstOrDefault();
            if (fee is null)
                throw new InvalidOperationException($"No delivery fee is configured for {SelectedAddress.State}.");
            _deliveryFee = fee.Price;
            RefreshTotals();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
    }

    private async Task ApplyCouponAsync()
    {
        if (string.IsNullOrWhiteSpace(CouponCode))
            return;
        IsLoading = true;
        HasCouponStatus = false;
        try
        {
            var code = Uri.EscapeDataString(CouponCode.Trim().ToUpperInvariant());
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=coupon&code={code}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var coupons = await response.Content.ReadFromJsonAsync<List<CouponInfo>>(JsonOptions);
            _coupon = coupons?.FirstOrDefault(item => item.Active && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > DateTime.UtcNow));
            if (_coupon is null)
            {
                CouponStatus = "Invalid, inactive, or expired promo code.";
                HasCouponStatus = true;
                return;
            }
            CouponStatus = $"Coupon {_coupon.Code} applied.";
            HasCouponStatus = true;
            RefreshTotals();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            CouponStatus = exception.Message;
            HasCouponStatus = true;
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckoutAsync()
    {
        if (IsLoading)
            return;
        RefreshTotals();
        if (!CanCheckout)
        {
            SetError("Sign in, select a delivery address, and accept the terms before checkout.");
            return;
        }

        IsLoading = true;
        HasStatus = false;
        try
        {
            var markup = RoleMarkup(UserRole);
            var payload = new
            {
                userId = UserId,
                items = Items.Select(item => new
                {
                    productId = item.ProductId,
                    quantity = item.Quantity,
                    bulkPriceId = item.BulkPriceId,
                    isSpecial = item.IsSpecial,
                    customPrice = item.CustomPrice,
                    customName = item.CustomName
                }).ToArray(),
                deliveryFee = IncludeDelivery ? _deliveryFee : 0,
                deliveryAddressId = SelectedAddress!.Id,
                couponCode = _coupon?.Code,
                discountAmount = _coupon is null ? 0 : Math.Ceiling((_coupon.Type == "percentage" ? Items.Sum(item => item.Price * markup * item.Quantity) * _coupon.Discount / 100 : _coupon.Discount) / 5) * 5
            };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/payment"), payload);
            _checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions)
                ?? throw new JsonException("Checkout API returned an invalid response.");
            HasCheckout = true;
            CheckoutSummary = $"Order {_checkout.CartId} is ready for payment. Reference: {_checkout.TxRef}; amount ₦{_checkout.Amount:N0}.";
            CheckoutReady?.Invoke(this, _checkout);
            (ConfirmPaymentCommand as Command)?.ChangeCanExecute();
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
                throw new InvalidOperationException(result?.Message ?? "The server could not confirm this payment.");
            Items.Clear();
            _checkout = null;
            HasCheckout = false;
            HasStatus = false;
            CheckoutSummary = string.Empty;
            PaymentConfirmed?.Invoke(this, EventArgs.Empty);
            RefreshTotals();
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

    private void Clear()
    {
        Items.Clear();
        _coupon = null;
        _checkout = null;
        HasCheckout = false;
        HasCouponStatus = false;
        RefreshTotals();
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    private static double RoleMarkup(string? role) => role?.ToLowerInvariant() switch
    {
        "professional" => 1.2,
        "wholesaler" or "admin" or "staff" => 1.1,
        _ => 1.35
    };

    private sealed class CouponInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = "fixed";
        public double Discount { get; set; }
        public bool Active { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    private sealed class DeliveryFeeInfo
    {
        public double Price { get; set; }
    }

    public sealed class CheckoutResponse
    {
        public string CartId { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("tx_ref")]
        public string TxRef { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Currency { get; set; } = "NGN";
    }

    private sealed class PaymentConfirmation
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
