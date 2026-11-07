namespace EEE.Components.UI;

public partial class Avatar : ContentView
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(Avatar), propertyChanged: OnContentChanged);
    public static readonly BindableProperty FallbackTextProperty =
        BindableProperty.Create(nameof(FallbackText), typeof(string), typeof(Avatar), string.Empty, propertyChanged: OnContentChanged);
    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(Avatar), 40d);
    public static readonly BindableProperty HasSourceProperty =
        BindableProperty.Create(nameof(HasSource), typeof(bool), typeof(Avatar), false);
    public static readonly BindableProperty HasFallbackProperty =
        BindableProperty.Create(nameof(HasFallback), typeof(bool), typeof(Avatar), true);

    public Avatar() => InitializeComponent();

    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public string FallbackText { get => (string)GetValue(FallbackTextProperty); set => SetValue(FallbackTextProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool HasSource { get => (bool)GetValue(HasSourceProperty); private set => SetValue(HasSourceProperty, value); }
    public bool HasFallback { get => (bool)GetValue(HasFallbackProperty); private set => SetValue(HasFallbackProperty, value); }

    private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var avatar = (Avatar)bindable;
        avatar.HasSource = avatar.Source is not null;
        avatar.HasFallback = !avatar.HasSource && !string.IsNullOrWhiteSpace(avatar.FallbackText);
    }
}
