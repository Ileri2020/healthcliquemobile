namespace EEE.Components.UI;

public partial class Carousel : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<object>), typeof(Carousel), propertyChanged: OnItemsSourceChanged);
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Carousel));
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(int), typeof(Carousel), 0, BindingMode.TwoWay, propertyChanged: OnPositionChanged);
    public static readonly BindableProperty LoopProperty = BindableProperty.Create(nameof(Loop), typeof(bool), typeof(Carousel), false);
    public static readonly BindableProperty IsSwipeEnabledProperty = BindableProperty.Create(nameof(IsSwipeEnabled), typeof(bool), typeof(Carousel), true);

    public Carousel()
    {
        InitializeComponent();
        Api = new CarouselApi();
        Api.PositionChanged += OnApiPositionChanged;
        UpdateApiItemCount();
    }

    public IEnumerable<object>? ItemsSource { get => (IEnumerable<object>?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    public int Position { get => (int)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
    public bool Loop { get => (bool)GetValue(LoopProperty); set => SetValue(LoopProperty, value); }
    public bool IsSwipeEnabled { get => (bool)GetValue(IsSwipeEnabledProperty); set => SetValue(IsSwipeEnabledProperty, value); }
    public CarouselApi Api { get; }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Carousel)bindable).UpdateApiItemCount();

    private static void OnPositionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var carousel = (Carousel)bindable;
        if (carousel.Api is not null && carousel.Api.Position != (int)newValue)
            carousel.Api.ScrollTo((int)newValue);
    }

    private void OnApiPositionChanged(object? sender, int position)
    {
        if (Position != position)
            Position = position;
    }

    private void UpdateApiItemCount()
    {
        if (Api is not null)
            Api.ItemCount = ItemsSource?.Count() ?? 0;
    }
}
