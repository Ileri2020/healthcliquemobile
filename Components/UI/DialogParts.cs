namespace EEE.Components.UI;

public class DialogPortal : ContentView { }

public class DialogOverlay : ThemedOverlay { }

public class DialogContent : ContentView
{
    public DialogContent() => Padding = 24;
}

public class DialogHeader : VerticalStackLayout
{
    public DialogHeader() => Spacing = 6;
}

public class DialogFooter : HorizontalStackLayout
{
    public DialogFooter()
    {
        Spacing = 8;
        HorizontalOptions = LayoutOptions.End;
    }
}

public class DialogTitle : CardTitle { }

public class DialogDescription : CardDescription { }

public class DialogTrigger : OverlayTrigger
{
    protected override bool Opens => true;
}

public class DialogClose : OverlayTrigger
{
    protected override bool Opens => false;
}

public abstract class OverlayTrigger : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(OverlayTrigger), string.Empty);
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(OverlayTrigger), false, BindingMode.TwoWay);
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(System.Windows.Input.ICommand), typeof(OverlayTrigger));

    protected OverlayTrigger()
    {
        var button = new Button();
        button.SetBinding(Button.TextProperty, new Binding(nameof(Text), source: this));
        button.Command = new Command(() =>
        {
            if (Command?.CanExecute(null) == true)
                Command.Execute(null);
            else
                IsOpen = Opens;
        });
        Content = button;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public System.Windows.Input.ICommand? Command { get => (System.Windows.Input.ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    protected abstract bool Opens { get; }
}
