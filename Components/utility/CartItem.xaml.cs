using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CartItem : ContentView
{
    public static readonly BindableProperty ItemIdProperty = BindableProperty.Create(nameof(ItemId), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(nameof(ImageUrl), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty PriceProperty = BindableProperty.Create(nameof(Price), typeof(double), typeof(CartItem), 0d, propertyChanged: OnValuesChanged);
    public static readonly BindableProperty QuantityProperty = BindableProperty.Create(nameof(Quantity), typeof(int), typeof(CartItem), 1, propertyChanged: OnValuesChanged);
    public static readonly BindableProperty BulkPriceIdProperty = BindableProperty.Create(nameof(BulkPriceId), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty BulkNameProperty = BindableProperty.Create(nameof(BulkName), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(nameof(Role), typeof(string), typeof(CartItem), "customer", propertyChanged: OnValuesChanged);
    public static readonly BindableProperty PriceTextProperty = BindableProperty.Create(nameof(PriceText), typeof(string), typeof(CartItem), string.Empty);
    public static readonly BindableProperty TotalTextProperty = BindableProperty.Create(nameof(TotalText), typeof(string), typeof(CartItem), string.Empty);

    public CartItem()
    {
        InitializeComponent();
        IncreaseCommand = new Command(() => Quantity++);
        DecreaseCommand = new Command(() => Quantity = Math.Max(1, Quantity - 1));
        RemoveCommand = new Command(() => Removed?.Invoke(this, EventArgs.Empty));
        UpdatePrices();
    }

    public string ItemId { get => (string)GetValue(ItemIdProperty); set => SetValue(ItemIdProperty, value); }
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public string ImageUrl { get => (string)GetValue(ImageUrlProperty); set => SetValue(ImageUrlProperty, value); }
    public double Price { get => (double)GetValue(PriceProperty); set => SetValue(PriceProperty, value); }
    public int Quantity { get => (int)GetValue(QuantityProperty); set => SetValue(QuantityProperty, value); }
    public string BulkPriceId { get => (string)GetValue(BulkPriceIdProperty); set => SetValue(BulkPriceIdProperty, value); }
    public string BulkName { get => (string)GetValue(BulkNameProperty); set => SetValue(BulkNameProperty, value); }
    public string Role { get => (string)GetValue(RoleProperty); set => SetValue(RoleProperty, value); }
    public string PriceText { get => (string)GetValue(PriceTextProperty); private set => SetValue(PriceTextProperty, value); }
    public string TotalText { get => (string)GetValue(TotalTextProperty); private set => SetValue(TotalTextProperty, value); }
    public ICommand IncreaseCommand { get; }
    public ICommand DecreaseCommand { get; }
    public ICommand RemoveCommand { get; }
    public event Action<object?, int>? QuantityChanged;
    public event EventHandler? Removed;

    private static void OnValuesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CartItem)bindable).UpdatePrices();

    private void UpdatePrices()
    {
        var multiplier = Role switch
        {
            "professional" => 1.2,
            "wholesaler" or "admin" or "staff" => 1.1,
            _ => 1.35
        };
        var unitPrice = Price * multiplier;
        PriceText = $"Price: ₦{unitPrice.ToString("N0")}";
        TotalText = $"₦{(unitPrice * Quantity).ToString("N0")}";
        if (Quantity > 0)
            QuantityChanged?.Invoke(this, Quantity);
    }
}
