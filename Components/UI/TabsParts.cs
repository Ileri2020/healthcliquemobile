namespace EEE.Components.UI;

public class TabsList : HorizontalStackLayout
{
    public TabsList() => Spacing = 4;
}

public class TabsTrigger : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TabsTrigger), string.Empty);
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(TabsTrigger), string.Empty);
    public static readonly BindableProperty SelectedKeyProperty =
        BindableProperty.Create(nameof(SelectedKey), typeof(string), typeof(TabsTrigger), string.Empty, BindingMode.TwoWay);

    public TabsTrigger()
    {
        var button = new Button();
        button.SetBinding(Button.TextProperty, new Binding(nameof(Text), source: this));
        button.Command = new Command(() => SelectedKey = Value);
        Content = button;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }
}

public class TabsContent : ContentView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(TabsContent), string.Empty, propertyChanged: OnSelectionChanged);
    public static readonly BindableProperty SelectedKeyProperty =
        BindableProperty.Create(nameof(SelectedKey), typeof(string), typeof(TabsContent), string.Empty, propertyChanged: OnSelectionChanged);

    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }

    private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var content = (TabsContent)bindable;
        content.IsVisible = !string.IsNullOrEmpty(content.Value) && content.Value == content.SelectedKey;
    }
}
