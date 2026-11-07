using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Similar : ContentView
{
    private IDispatcherTimer? _timer;
    private int _currentPosition;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Similar), "Similar Products");
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(Similar), "customer", propertyChanged: OnUserRoleChanged);
    public static readonly BindableProperty ProductsProperty = BindableProperty.Create(nameof(Products), typeof(IList<DiscoveryProduct>), typeof(Similar), defaultValueCreator: _ => new List<DiscoveryProduct>(), propertyChanged: OnProductsChanged);

    public Similar()
    {
        InitializeComponent();
        Items = [];
        AddToCartCommand = new Command<SimilarProductView>(item =>
        {
            if (item is not null)
                AddToCartRequested?.Invoke(this, item.Product);
        });
        SelectCommand = new Command<SimilarProductView>(item =>
        {
            if (item is not null)
                ProductSelected?.Invoke(this, item.Product);
        });
        Loaded += (_, _) => StartCarousel();
        Unloaded += (_, _) => StopCarousel();
        UpdateItems();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public IList<DiscoveryProduct> Products { get => (IList<DiscoveryProduct>)GetValue(ProductsProperty); set => SetValue(ProductsProperty, value); }
    public ObservableCollection<SimilarProductView> Items { get; }
    public bool HasProducts => Items.Count > 0;
    public ICommand AddToCartCommand { get; }
    public ICommand SelectCommand { get; }
    public event Action<object?, DiscoveryProduct>? AddToCartRequested;
    public event Action<object?, DiscoveryProduct>? ProductSelected;

    private static void OnProductsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Similar)bindable).UpdateItems();

    private static void OnUserRoleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Similar)bindable).UpdateItems();

    private void UpdateItems()
    {
        Items.Clear();
        foreach (var product in Products.OrderBy(_ => Random.Shared.Next()))
            Items.Add(new SimilarProductView(product, UserRole));
        _currentPosition = 0;
        if (Items.Count > 0 && IsLoaded)
            ProductsCarousel.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
        OnPropertyChanged(nameof(HasProducts));
        StartCarousel();
    }

    private void StartCarousel()
    {
        StopCarousel();
        if (!IsLoaded || Items.Count < 2)
            return;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += (_, _) =>
        {
            if (Items.Count < 2)
                return;
            _currentPosition = (_currentPosition + 1) % Items.Count;
            ProductsCarousel.ScrollTo(_currentPosition, position: ScrollToPosition.Center, animate: true);
        };
        _timer.Start();
    }

    private void StopCarousel()
    {
        if (_timer is null)
            return;
        _timer.Stop();
        _timer = null;
    }
}

public sealed class SimilarProductView
{
    public SimilarProductView(DiscoveryProduct product, string role)
    {
        Product = product;
        Name = product.Name;
        ImageUrl = product.ImageUrl;
        PriceText = product.PriceText(role);
    }

    public DiscoveryProduct Product { get; }
    public string Name { get; }
    public string ImageUrl { get; }
    public string PriceText { get; }
}
