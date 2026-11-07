using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Hero : ContentView
{
    private static readonly string[] HeroMessages =
    [
        "Order authentic medications online",
        "Consult with expert pharmacists",
        "Track your medical supplies delivery",
        "Access wholesale healthcare products",
        "Read the latest clinical insights",
        "Manage your prescriptions easily",
        "NAFDAC-approved pharmacy products",
        "Professional health support",
        "Direct supply for wholesalers",
        "Verified medical research and news"
    ];
    private IDispatcherTimer? _messageTimer;
    private int _messageIndex;
    private bool _isCompactLayout;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Hero), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(Hero), "visitor");
    public static readonly BindableProperty IsAuthenticatedProperty = BindableProperty.Create(nameof(IsAuthenticated), typeof(bool), typeof(Hero), false);
    public static readonly BindableProperty HeroImageUrlProperty = BindableProperty.Create(nameof(HeroImageUrl), typeof(string), typeof(Hero), "pharmacist.png", propertyChanged: OnHeroImageChanged);
    public static readonly BindableProperty HasHeroImageProperty = BindableProperty.Create(nameof(HasHeroImage), typeof(bool), typeof(Hero), false);
    public static readonly BindableProperty RotatingMessageProperty = BindableProperty.Create(nameof(RotatingMessage), typeof(string), typeof(Hero), HeroMessages[0]);
    public static readonly BindableProperty IsLoginPromptOpenProperty = BindableProperty.Create(nameof(IsLoginPromptOpen), typeof(bool), typeof(Hero), false);

    public Hero()
    {
        InitializeComponent();
        NavigateCommand = new Command<string>(path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
                NavigationRequested?.Invoke(this, path);
        });
        ContactCommand = new Command(() =>
        {
            if (IsAuthenticated)
                NavigationRequested?.Invoke(this, "/contact");
            else
                IsLoginPromptOpen = true;
        });
        HeroSearch.ProductSelected += (_, product) => ProductSelected?.Invoke(this, product);
        PrescriptionCommand = new Command(() => PrescriptionRequested?.Invoke(this, EventArgs.Empty));
        SpecialOrderCommand = new Command(() => SpecialOrderRequested?.Invoke(this, EventArgs.Empty));
        CallCommand = new Command(async () =>
        {
            try
            {
                await Launcher.Default.OpenAsync("tel:+2348188762448");
            }
            catch (Exception exception) when (exception is FeatureNotSupportedException or UriFormatException or InvalidOperationException)
            {
                ActionFailed?.Invoke(this, exception);
            }
        });
        Loaded += (_, _) => StartMessageRotation();
        Unloaded += (_, _) => StopMessageRotation();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsAuthenticated { get => (bool)GetValue(IsAuthenticatedProperty); set => SetValue(IsAuthenticatedProperty, value); }
    public string HeroImageUrl { get => (string)GetValue(HeroImageUrlProperty); set => SetValue(HeroImageUrlProperty, value); }
    public bool HasHeroImage { get => (bool)GetValue(HasHeroImageProperty); private set => SetValue(HasHeroImageProperty, value); }
    public string RotatingMessage { get => (string)GetValue(RotatingMessageProperty); private set => SetValue(RotatingMessageProperty, value); }
    public bool IsLoginPromptOpen { get => (bool)GetValue(IsLoginPromptOpenProperty); set => SetValue(IsLoginPromptOpenProperty, value); }
    public ICommand NavigateCommand { get; }
    public ICommand ContactCommand { get; }
    public ICommand PrescriptionCommand { get; }
    public ICommand SpecialOrderCommand { get; }
    public ICommand CallCommand { get; }
    public event Action<object?, string>? NavigationRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;
    public event EventHandler? PrescriptionRequested;
    public event EventHandler? SpecialOrderRequested;
    public event Action<object?, Exception>? ActionFailed;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var isCompact = width < 760;
        if (_isCompactLayout == isCompact || width <= 0)
            return;

        _isCompactLayout = isCompact;
        HeroGrid.ColumnDefinitions.Clear();
        HeroGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        if (isCompact)
        {
            HeroCopy.SetValue(Grid.RowSpanProperty, 1);
            Grid.SetColumn(HeroImagePanel, 0);
            Grid.SetRow(HeroImagePanel, 1);
            HeroImagePanel.SetValue(Grid.RowSpanProperty, 1);
        }
        else
        {
            HeroGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            HeroCopy.SetValue(Grid.RowSpanProperty, 2);
            Grid.SetColumn(HeroImagePanel, 1);
            Grid.SetRow(HeroImagePanel, 0);
            HeroImagePanel.SetValue(Grid.RowSpanProperty, 2);
        }
    }

    private static void OnHeroImageChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Hero)bindable).HasHeroImage = !string.IsNullOrWhiteSpace((string?)newValue);

    private void StartMessageRotation()
    {
        if (_messageTimer is not null)
            return;
        _messageTimer = Dispatcher.CreateTimer();
        _messageTimer.Interval = TimeSpan.FromSeconds(3);
        _messageTimer.Tick += (_, _) =>
        {
            _messageIndex = (_messageIndex + 1) % HeroMessages.Length;
            RotatingMessage = HeroMessages[_messageIndex];
        };
        _messageTimer.Start();
    }

    private void StopMessageRotation()
    {
        _messageTimer?.Stop();
        _messageTimer = null;
    }
}
