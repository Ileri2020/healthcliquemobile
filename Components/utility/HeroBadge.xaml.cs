using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class HeroBadge : ContentView
{
    public static readonly BindableProperty BrandNameProperty = BindableProperty.Create(nameof(BrandName), typeof(string), typeof(HeroBadge), "Health Clique", propertyChanged: OnBadgeTextChanged);
    public static readonly BindableProperty FullNameProperty = BindableProperty.Create(nameof(FullName), typeof(string), typeof(HeroBadge), "Health Clique Care", propertyChanged: OnBadgeTextChanged);
    public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(nameof(BadgeText), typeof(string), typeof(HeroBadge), string.Empty);
    public static readonly BindableProperty DestinationProperty = BindableProperty.Create(nameof(Destination), typeof(string), typeof(HeroBadge), "/products");

    public HeroBadge()
    {
        InitializeComponent();
        NavigateCommand = new Command(() => NavigationRequested?.Invoke(this, Destination));
        UpdateBadgeText();
    }

    public string BrandName { get => (string)GetValue(BrandNameProperty); set => SetValue(BrandNameProperty, value); }
    public string FullName { get => (string)GetValue(FullNameProperty); set => SetValue(FullNameProperty, value); }
    public string BadgeText { get => (string)GetValue(BadgeTextProperty); private set => SetValue(BadgeTextProperty, value); }
    public string Destination { get => (string)GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }
    public ICommand NavigateCommand { get; }
    public event Action<object?, string>? NavigationRequested;

    private static void OnBadgeTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((HeroBadge)bindable).UpdateBadgeText();

    private void UpdateBadgeText() => BadgeText = $"{FullName}  |  ⭐ 2 stars on GitHub";
}
