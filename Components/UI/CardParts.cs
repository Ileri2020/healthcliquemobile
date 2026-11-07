namespace EEE.Components.UI;

public class CardHeader : VerticalStackLayout
{
    public CardHeader() => Spacing = 6;
}

public class CardTitle : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CardTitle), string.Empty, propertyChanged: OnTextChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public CardTitle()
    {
        _label = new Microsoft.Maui.Controls.Label
        {
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        };
        Content = _label;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CardTitle)bindable)._label.Text = (string?)newValue;
}

public class CardDescription : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CardDescription), string.Empty, propertyChanged: OnTextChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public CardDescription()
    {
        _label = new Microsoft.Maui.Controls.Label { FontSize = 14 };
        Content = _label;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CardDescription)bindable)._label.Text = (string?)newValue;
}

public class CardContent : ContentView { }

public class CardBody : CardContent { }

public class CardFooter : HorizontalStackLayout
{
    public CardFooter() => Spacing = 8;
}
