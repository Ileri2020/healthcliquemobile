namespace EEE.Components.UI;

public class AvatarImage : Image
{
    public AvatarImage() => Aspect = Aspect.AspectFill;
}

public class AvatarFallback : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AvatarFallback), string.Empty, propertyChanged: OnTextChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public AvatarFallback()
    {
        _label = new Microsoft.Maui.Controls.Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontAttributes = FontAttributes.Bold
        };
        Content = _label;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AvatarFallback)bindable)._label.Text = (string?)newValue;
}
