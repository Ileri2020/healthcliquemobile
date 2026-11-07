using System.Collections;
using System.Windows.Input;

namespace EEE.Components.UI;

public class SelectGroup : VerticalStackLayout { }

public class SelectValue : ContentView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(object), typeof(SelectValue), propertyChanged: OnValueChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public SelectValue()
    {
        _label = new Microsoft.Maui.Controls.Label();
        Content = _label;
    }

    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SelectValue)bindable)._label.Text = newValue?.ToString();
}

public class SelectTrigger : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SelectTrigger), "Select an option");
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(SelectTrigger));
    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(SelectTrigger), defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty AllowClearProperty =
        BindableProperty.Create(nameof(AllowClear), typeof(bool), typeof(SelectTrigger), false);

    public SelectTrigger()
    {
        var picker = new Picker();
        picker.SetBinding(Picker.TitleProperty, new Binding(nameof(Title), source: this));
        picker.SetBinding(Picker.ItemsSourceProperty, new Binding(nameof(ItemsSource), source: this));
        picker.SetBinding(Picker.SelectedItemProperty, new Binding(nameof(SelectedItem), source: this, mode: BindingMode.TwoWay));
        Content = picker;
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public bool AllowClear { get => (bool)GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }
}

public class SelectContent : VerticalStackLayout { }

public class SelectLabel : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SelectLabel), string.Empty, propertyChanged: OnTextChanged);

    private readonly Microsoft.Maui.Controls.Label _label;

    public SelectLabel()
    {
        _label = new Microsoft.Maui.Controls.Label { FontAttributes = FontAttributes.Bold };
        Content = _label;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SelectLabel)bindable)._label.Text = (string?)newValue;
}

public class SelectItem : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SelectItem), string.Empty, propertyChanged: OnTextChanged);
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(object), typeof(SelectItem));
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SelectItem));

    private readonly Button _button;

    public SelectItem()
    {
        _button = new Button();
        _button.SetBinding(Button.TextProperty, new Binding(nameof(Text), source: this));
        _button.SetBinding(Button.CommandProperty, new Binding(nameof(Command), source: this));
        _button.SetBinding(Button.CommandParameterProperty, new Binding(nameof(Value), source: this));
        Content = _button;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SelectItem)bindable)._button.Text = (string?)newValue;
}

public class SelectSeparator : BoxView
{
    public SelectSeparator()
    {
        HeightRequest = 1;
        HorizontalOptions = LayoutOptions.Fill;
    }
}

public class SelectScrollUpButton : Button
{
    public SelectScrollUpButton() => Text = "Scroll up";
}

public class SelectScrollDownButton : Button
{
    public SelectScrollDownButton() => Text = "Scroll down";
}
