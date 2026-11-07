using System.Collections.ObjectModel;

namespace EEE.Components.Utility;

public partial class Cart : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Cart), string.Empty);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(Cart), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(Cart), "customer");
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(ObservableCollection<CommerceCartItem>), typeof(Cart), defaultValueCreator: _ => new ObservableCollection<CommerceCartItem>());

    public Cart()
    {
        InitializeComponent();
        Client.CheckoutReady += (sender, checkout) => CheckoutReady?.Invoke(this, checkout);
        Client.PaymentConfirmed += (sender, args) => PaymentConfirmed?.Invoke(this, args);
        Client.RequestFailed += (sender, exception) => RequestFailed?.Invoke(this, exception);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public ObservableCollection<CommerceCartItem> Items { get => (ObservableCollection<CommerceCartItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public event Action<object?, CartClient.CheckoutResponse>? CheckoutReady;
    public event EventHandler? PaymentConfirmed;
    public event Action<object?, Exception>? RequestFailed;
}
