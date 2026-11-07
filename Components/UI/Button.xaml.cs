using System.Windows.Input;

namespace EEE.Components.UI;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Outline,
    Ghost,
    Destructive
}

public static class ButtonVariants
{
    public const ButtonVariant Primary = ButtonVariant.Primary;
    public const ButtonVariant Secondary = ButtonVariant.Secondary;
    public const ButtonVariant Outline = ButtonVariant.Outline;
    public const ButtonVariant Ghost = ButtonVariant.Ghost;
    public const ButtonVariant Destructive = ButtonVariant.Destructive;
}

public partial class Button : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(Button), string.Empty);
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Button));
    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Button));
    public static readonly BindableProperty LeadingContentProperty =
        BindableProperty.Create(nameof(LeadingContent), typeof(View), typeof(Button));
    public static readonly BindableProperty TrailingContentProperty =
        BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(Button));
    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(Button), ButtonVariant.Primary, propertyChanged: OnVariantChanged);

    public Button()
    {
        InitializeComponent();
        ApplyVariant();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public View? LeadingContent { get => (View?)GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public View? TrailingContent { get => (View?)GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    private static void OnVariantChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Button)bindable).ApplyVariant();

    private void ApplyVariant()
    {
        if (NativeButton is null)
            return;

        NativeButton.Style = (Style)Application.Current!.Resources[$"ThemeButton{Variant}"];
    }
}
