namespace EEE.Components.UI;

public partial class Input : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Input), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(Input), string.Empty);
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(Input), Keyboard.Default);
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(Input), false);
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(Input), int.MaxValue);
    public static readonly BindableProperty AllowClearProperty = BindableProperty.Create(nameof(AllowClear), typeof(bool), typeof(Input), false);
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(Input), ReturnType.Default);

    public Input() => InitializeComponent();

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public bool AllowClear { get => (bool)GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }
    public ReturnType ReturnType { get => (ReturnType)GetValue(ReturnTypeProperty); set => SetValue(ReturnTypeProperty, value); }
}
