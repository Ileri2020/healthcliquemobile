namespace EEE.Components.UI;

public partial class Table : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<object>), typeof(Table));
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Table));
    public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(nameof(HeaderContent), typeof(View), typeof(Table));
    public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(nameof(BodyContent), typeof(View), typeof(Table), propertyChanged: OnBodyContentChanged);
    public static readonly BindableProperty FooterContentProperty = BindableProperty.Create(nameof(FooterContent), typeof(View), typeof(Table));
    public static readonly BindableProperty CaptionContentProperty = BindableProperty.Create(nameof(CaptionContent), typeof(View), typeof(Table));
    public static readonly BindableProperty EmptyViewProperty = BindableProperty.Create(nameof(EmptyView), typeof(object), typeof(Table), "No items to display.");
    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(nameof(SelectionMode), typeof(SelectionMode), typeof(Table), SelectionMode.None);
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(Table), defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty HasCustomBodyProperty = BindableProperty.Create(nameof(HasCustomBody), typeof(bool), typeof(Table), false);
    public static readonly BindableProperty HasDefaultBodyProperty = BindableProperty.Create(nameof(HasDefaultBody), typeof(bool), typeof(Table), true);

    public Table() => InitializeComponent();

    public IEnumerable<object>? ItemsSource { get => (IEnumerable<object>?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    public View? HeaderContent { get => (View?)GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public View? BodyContent { get => (View?)GetValue(BodyContentProperty); set => SetValue(BodyContentProperty, value); }
    public View? FooterContent { get => (View?)GetValue(FooterContentProperty); set => SetValue(FooterContentProperty, value); }
    public View? CaptionContent { get => (View?)GetValue(CaptionContentProperty); set => SetValue(CaptionContentProperty, value); }
    public object? EmptyView { get => GetValue(EmptyViewProperty); set => SetValue(EmptyViewProperty, value); }
    public SelectionMode SelectionMode { get => (SelectionMode)GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public bool HasCustomBody { get => (bool)GetValue(HasCustomBodyProperty); private set => SetValue(HasCustomBodyProperty, value); }
    public bool HasDefaultBody { get => (bool)GetValue(HasDefaultBodyProperty); private set => SetValue(HasDefaultBodyProperty, value); }

    private static void OnBodyContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (Table)bindable;
        table.HasCustomBody = newValue is View;
        table.HasDefaultBody = newValue is not View;
    }
}
