using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CartDetailsDialog : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(CartDetailsDialog), string.Empty);
    public static readonly BindableProperty CartProperty = BindableProperty.Create(nameof(Cart), typeof(CommerceCart), typeof(CartDetailsDialog), null, propertyChanged: OnCartChanged);
    public static readonly BindableProperty TriggerTextProperty = BindableProperty.Create(nameof(TriggerText), typeof(string), typeof(CartDetailsDialog), "Order details");
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(CartDetailsDialog), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(CartDetailsDialog), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSendEmailProperty = BindableProperty.Create(nameof(CanSendEmail), typeof(bool), typeof(CartDetailsDialog), true);
    public static readonly BindableProperty CanConfirmProperty = BindableProperty.Create(nameof(CanConfirm), typeof(bool), typeof(CartDetailsDialog), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(CartDetailsDialog), false);
    public static readonly BindableProperty EmailMessageProperty = BindableProperty.Create(nameof(EmailMessage), typeof(string), typeof(CartDetailsDialog), string.Empty);
    public static readonly BindableProperty CustomerTextProperty = BindableProperty.Create(nameof(CustomerText), typeof(string), typeof(CartDetailsDialog), string.Empty);
    public static readonly BindableProperty PaymentTextProperty = BindableProperty.Create(nameof(PaymentText), typeof(string), typeof(CartDetailsDialog), string.Empty);
    public static readonly BindableProperty TotalsTextProperty = BindableProperty.Create(nameof(TotalsText), typeof(string), typeof(CartDetailsDialog), string.Empty);
    public static readonly BindableProperty SendEmailTextProperty = BindableProperty.Create(nameof(SendEmailText), typeof(string), typeof(CartDetailsDialog), "Send Manual Notification");
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(CartDetailsDialog), string.Empty);

    public CartDetailsDialog()
    {
        InitializeComponent();
        OpenCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(() => IsOpen = false);
        SendEmailCommand = new Command(async () => await SendEmailAsync(), () => CanSendEmail);
        ConfirmPaymentCommand = new Command(async () => await ConfirmPaymentAsync());
        RefreshCartSummary();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public CommerceCart? Cart { get => (CommerceCart?)GetValue(CartProperty); set => SetValue(CartProperty, value); }
    public string TriggerText { get => (string)GetValue(TriggerTextProperty); set => SetValue(TriggerTextProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSendEmail { get => (bool)GetValue(CanSendEmailProperty); private set => SetValue(CanSendEmailProperty, value); }
    public bool CanConfirm { get => (bool)GetValue(CanConfirmProperty); private set => SetValue(CanConfirmProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string EmailMessage { get => (string)GetValue(EmailMessageProperty); set => SetValue(EmailMessageProperty, value); }
    public string CustomerText { get => (string)GetValue(CustomerTextProperty); private set => SetValue(CustomerTextProperty, value); }
    public string PaymentText { get => (string)GetValue(PaymentTextProperty); private set => SetValue(PaymentTextProperty, value); }
    public string TotalsText { get => (string)GetValue(TotalsTextProperty); private set => SetValue(TotalsTextProperty, value); }
    public string SendEmailText { get => (string)GetValue(SendEmailTextProperty); private set => SetValue(SendEmailTextProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SendEmailCommand { get; }
    public ICommand ConfirmPaymentCommand { get; }
    public event EventHandler? PaymentConfirmed;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnCartChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CartDetailsDialog)bindable).RefreshCartSummary();

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CartDetailsDialog)bindable;
        control.CanSendEmail = !(bool)newValue;
        control.SendEmailText = (bool)newValue ? "Sending..." : "Send Manual Notification";
        (control.SendEmailCommand as Command)?.ChangeCanExecute();
    }

    private void RefreshCartSummary()
    {
        if (Cart is null)
        {
            CustomerText = "Customer information unavailable.";
            PaymentText = "No payment record found.";
            TotalsText = string.Empty;
            CanConfirm = false;
            return;
        }

        CustomerText = $"Customer: {Cart.User?.Name ?? "N/A"}  |  {Cart.User?.Email ?? "N/A"}";
        PaymentText = Cart.Payment is null
            ? "No payment record found."
            : $"Payment: {Cart.Payment.Method}  |  Reference: {Cart.Payment.TxRef}  |  Amount: ₦{Cart.Payment.Amount:N0}";
        var subtotal = Cart.Products.Sum(item => (item.CustomPrice ?? item.Product?.Price ?? item.Price) * item.Quantity);
        TotalsText = $"Subtotal ₦{subtotal:N0}   Delivery ₦{Cart.DeliveryFee ?? 0:N0}   Total ₦{Cart.Total:N0}";
        CanConfirm = (Cart.Status is "unconfirmed" or "pending")
            && Cart.Payment is not null
            && Cart.Payment.Method != "manual_transfer";
    }

    private async Task SendEmailAsync()
    {
        if (Cart is null || string.IsNullOrWhiteSpace(EmailMessage))
        {
            SetStatus("Enter a notification message.");
            return;
        }
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/admin/send-email");
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new { cartId = Cart.Id, message = EmailMessage.Trim() });
            EmailMessage = string.Empty;
            SetStatus("Notification sent to customer.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ConfirmPaymentAsync()
    {
        if (Cart?.Payment is null)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/payment?action=confirm");
            var method = Cart.Payment.Method is "monnify" or "flutterwave"
                ? Cart.Payment.Method
                : "monnify";
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new
            {
                tx_ref = Cart.Payment.TxRef,
                method
            });
            var result = await response.Content.ReadFromJsonAsync<PaymentConfirmation>(JsonOptions);
            if (result?.Success != true)
                throw new InvalidOperationException(result?.Message ?? "The server could not confirm payment.");
            Cart.Status = "paid";
            CanConfirm = false;
            SetStatus("Payment confirmed and order marked as paid.");
            PaymentConfirmed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    private sealed class PaymentConfirmation
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
