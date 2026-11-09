using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class TextCarousel : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TextCarousel), "INPUT YOUR CAROUSEL TEXT HERE");
    public static readonly BindableProperty SpeedProperty = BindableProperty.Create(nameof(Speed), typeof(int), typeof(TextCarousel), 3000, propertyChanged: OnTimerChanged);
    public static readonly BindableProperty ShowImageProperty = BindableProperty.Create(nameof(ShowImage), typeof(bool), typeof(TextCarousel), false);
    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(nameof(ImageUrl), typeof(string), typeof(TextCarousel), "https://res.cloudinary.com/dc5khnuiu/image/upload/v1751682480/inhqujbmn3pxu9nefbuj.jpg");
    public static readonly BindableProperty DirectionProperty = BindableProperty.Create(nameof(Direction), typeof(string), typeof(TextCarousel), "left", propertyChanged: OnTimerChanged);
    public static readonly BindableProperty CarouselHeightProperty = BindableProperty.Create(nameof(CarouselHeight), typeof(double), typeof(TextCarousel), 400d);
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TextCarousel), 34d);
    public static readonly BindableProperty TextRotationProperty = BindableProperty.Create(nameof(TextRotation), typeof(double), typeof(TextCarousel), 0d);

    private IDispatcherTimer? _timer;

    public TextCarousel()
    {
        InitializeComponent();
        StartTimer();
        Unloaded += (_, _) => _timer?.Stop();
        Loaded += (_, _) => StartTimer();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public int Speed { get => (int)GetValue(SpeedProperty); set => SetValue(SpeedProperty, value); }
    public bool ShowImage { get => (bool)GetValue(ShowImageProperty); set => SetValue(ShowImageProperty, value); }
    public string ImageUrl { get => (string)GetValue(ImageUrlProperty); set => SetValue(ImageUrlProperty, value); }
    public string Direction { get => (string)GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }
    public double CarouselHeight { get => (double)GetValue(CarouselHeightProperty); set => SetValue(CarouselHeightProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public double TextRotation { get => (double)GetValue(TextRotationProperty); private set => SetValue(TextRotationProperty, value); }

    private static void OnTimerChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TextCarousel)bindable).StartTimer();

    private void StartTimer()
    {
        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(100, Speed));
        _timer.Tick += (_, _) => TextRotation += string.Equals(Direction, "right", StringComparison.OrdinalIgnoreCase) ? 7.2 : -7.2;
        _timer.Start();
    }
}
