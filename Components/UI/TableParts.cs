namespace EEE.Components.UI;

public class TableHeader : VerticalStackLayout { }

public class TableBody : VerticalStackLayout { }

public class TableFooter : HorizontalStackLayout { }

public class TableRow : HorizontalStackLayout
{
    public TableRow() => Spacing = 12;
}

public class TableHead : ContentView
{
    public TableHead() => Padding = new Thickness(12, 8);
}

public class TableCell : ContentView
{
    public TableCell() => Padding = new Thickness(12, 8);
}

public class TableCaption : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TableCaption), string.Empty, propertyChanged: OnTextChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public TableCaption()
    {
        _label = new Microsoft.Maui.Controls.Label { FontSize = 13 };
        Content = _label;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TableCaption)bindable)._label.Text = (string?)newValue;
}
