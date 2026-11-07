namespace EEE.Components.UI;

public partial class HoverCard : ContentView
{
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(HoverCard), false, BindingMode.TwoWay);
    public static readonly BindableProperty TriggerContentProperty = BindableProperty.Create(nameof(TriggerContent), typeof(View), typeof(HoverCard));
    public static readonly BindableProperty CardContentProperty = BindableProperty.Create(nameof(CardContent), typeof(View), typeof(HoverCard));

    public HoverCard() => InitializeComponent();

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public View? TriggerContent { get => (View?)GetValue(TriggerContentProperty); set => SetValue(TriggerContentProperty, value); }
    public View? CardContent { get => (View?)GetValue(CardContentProperty); set => SetValue(CardContentProperty, value); }

    private void OnPointerEntered(object? sender, PointerEventArgs e) => IsOpen = true;
    private void OnPointerExited(object? sender, PointerEventArgs e) => IsOpen = false;
    private void OnTriggerTapped(object? sender, TappedEventArgs e) => IsOpen = !IsOpen;
}
