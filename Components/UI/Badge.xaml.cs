namespace EEE.Components.UI;

public partial class Badge : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(Badge), string.Empty);

    public Badge() => InitializeComponent();

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
}
