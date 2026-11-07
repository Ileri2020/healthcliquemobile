namespace EEE.Components.UI;

public class HoverCardTrigger : ContentView
{
    public static readonly BindableProperty TriggerContentProperty =
        BindableProperty.Create(nameof(TriggerContent), typeof(View), typeof(HoverCardTrigger));
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(HoverCardTrigger), false, BindingMode.TwoWay);

    public HoverCardTrigger()
    {
        var content = new ContentView();
        content.SetBinding(ContentProperty, new Binding(nameof(TriggerContent), source: this));
        content.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => IsOpen = !IsOpen) });
        Content = content;
    }

    public View? TriggerContent { get => (View?)GetValue(TriggerContentProperty); set => SetValue(TriggerContentProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
}

public class HoverCardContent : ContentView { }
