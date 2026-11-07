using System.Collections;

namespace EEE.Components.UI;

public partial class Select : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Select), "Select an option");
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(Select));
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(Select), defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty AllowClearProperty = BindableProperty.Create(nameof(AllowClear), typeof(bool), typeof(Select), false);
    public static readonly BindableProperty CustomContentProperty = BindableProperty.Create(nameof(CustomContent), typeof(View), typeof(Select), propertyChanged: OnCustomContentChanged);
    public static readonly BindableProperty HasCustomContentProperty = BindableProperty.Create(nameof(HasCustomContent), typeof(bool), typeof(Select), false);
    public static readonly BindableProperty HasDefaultContentProperty = BindableProperty.Create(nameof(HasDefaultContent), typeof(bool), typeof(Select), true);

    public Select() => InitializeComponent();

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public bool AllowClear { get => (bool)GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }
    public View? CustomContent { get => (View?)GetValue(CustomContentProperty); set => SetValue(CustomContentProperty, value); }
    public bool HasCustomContent { get => (bool)GetValue(HasCustomContentProperty); private set => SetValue(HasCustomContentProperty, value); }
    public bool HasDefaultContent { get => (bool)GetValue(HasDefaultContentProperty); private set => SetValue(HasDefaultContentProperty, value); }

    private static void OnCustomContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var select = (Select)bindable;
        select.HasCustomContent = newValue is View;
        select.HasDefaultContent = newValue is not View;
    }
}
