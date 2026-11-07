using System.Collections.ObjectModel;

namespace EEE.Components.Utility;

public partial class Testimonials : ContentView
{
    private IDispatcherTimer? _timer;
    private int _currentPosition;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Testimonials), string.Empty);
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(Testimonials), string.Empty);
    public static readonly BindableProperty TestimonialItemsProperty = BindableProperty.Create(nameof(TestimonialItems), typeof(IList<TestimonialItem>), typeof(Testimonials), defaultValueCreator: _ => new List<TestimonialItem>(), propertyChanged: OnItemsChanged);

    public Testimonials()
    {
        InitializeComponent();
        Items = [];
        Loaded += (_, _) => StartMarquee();
        Unloaded += (_, _) => StopMarquee();
        UpdateItems();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public IList<TestimonialItem> TestimonialItems { get => (IList<TestimonialItem>)GetValue(TestimonialItemsProperty); set => SetValue(TestimonialItemsProperty, value); }
    public ObservableCollection<TestimonialItem> Items { get; }

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Testimonials)bindable).UpdateItems();

    private void UpdateItems()
    {
        Items.Clear();
        for (var copy = 0; copy < 4; copy++)
        {
            foreach (var item in TestimonialItems)
                Items.Add(item);
        }
        _currentPosition = 0;
        if (Items.Count > 0 && IsLoaded)
            TestimonialsCarousel.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
    }

    private void StartMarquee()
    {
        StopMarquee();
        if (Items.Count < 2)
            return;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(4);
        _timer.Tick += (_, _) =>
        {
            if (Items.Count == 0)
                return;
            _currentPosition = (_currentPosition + 1) % Items.Count;
            TestimonialsCarousel.ScrollTo(_currentPosition, position: ScrollToPosition.Center, animate: true);
        };
        _timer.Start();
    }

    private void StopMarquee()
    {
        if (_timer is null)
            return;
        _timer.Stop();
        _timer = null;
    }
}

public sealed class TestimonialsSection : Testimonials { }
