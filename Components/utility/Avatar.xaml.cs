namespace EEE.Components.Utility;

public partial class Avatar : ContentView
{
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(Avatar));
    public static readonly BindableProperty FallbackTextProperty = BindableProperty.Create(nameof(FallbackText), typeof(string), typeof(Avatar), string.Empty);
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(Avatar), 40d);

    public Avatar() => InitializeComponent();

    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public string FallbackText { get => (string)GetValue(FallbackTextProperty); set => SetValue(FallbackTextProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
}
