namespace EEE.Components.UI;

public partial class Sheet : ContentView
{
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(Sheet), false, BindingMode.TwoWay);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Sheet), string.Empty);
    public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(nameof(BodyContent), typeof(View), typeof(Sheet));
    public static readonly BindableProperty FooterContentProperty = BindableProperty.Create(nameof(FooterContent), typeof(View), typeof(Sheet));

    public Sheet() => InitializeComponent();

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public View? BodyContent { get => (View?)GetValue(BodyContentProperty); set => SetValue(BodyContentProperty, value); }
    public View? FooterContent { get => (View?)GetValue(FooterContentProperty); set => SetValue(FooterContentProperty, value); }

    public event EventHandler? Dismissed;

    private void OnCloseClicked(object? sender, EventArgs e) => Close();
    private void OnBackdropClicked(object? sender, EventArgs e) => Close();

    private void Close()
    {
        IsOpen = false;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
