using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class LocalOperationModeToggle : ContentView
{
    public static readonly BindableProperty FullWidthProperty =
        BindableProperty.Create(nameof(FullWidth), typeof(bool), typeof(LocalOperationModeToggle), false, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IsLocalProperty =
        BindableProperty.Create(nameof(IsLocal), typeof(bool), typeof(LocalOperationModeToggle), LocalOperationModeService.IsLocalEnvironment);
    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(nameof(Mode), typeof(LocalOperationMode), typeof(LocalOperationModeToggle), LocalOperationMode.Offline, propertyChanged: OnModeChanged);
    public static readonly BindableProperty OnlineVariantProperty =
        BindableProperty.Create(nameof(OnlineVariant), typeof(ButtonVariant), typeof(LocalOperationModeToggle), ButtonVariant.Ghost);
    public static readonly BindableProperty OfflineVariantProperty =
        BindableProperty.Create(nameof(OfflineVariant), typeof(ButtonVariant), typeof(LocalOperationModeToggle), ButtonVariant.Primary);

    public LocalOperationModeToggle()
    {
        InitializeComponent();
        SetOnlineCommand = new Command(() => Mode = LocalOperationMode.Online);
        SetOfflineCommand = new Command(() => Mode = LocalOperationMode.Offline);
        Mode = LocalOperationModeService.Mode;
        UpdateLayoutOptions();
        LocalOperationModeService.ModeChanged += OnServiceModeChanged;
    }

    public bool FullWidth { get => (bool)GetValue(FullWidthProperty); set => SetValue(FullWidthProperty, value); }
    public bool IsLocal { get => (bool)GetValue(IsLocalProperty); set => SetValue(IsLocalProperty, value); }
    public LocalOperationMode Mode { get => (LocalOperationMode)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public ButtonVariant OnlineVariant { get => (ButtonVariant)GetValue(OnlineVariantProperty); private set => SetValue(OnlineVariantProperty, value); }
    public ButtonVariant OfflineVariant { get => (ButtonVariant)GetValue(OfflineVariantProperty); private set => SetValue(OfflineVariantProperty, value); }
    public ICommand SetOnlineCommand { get; }
    public ICommand SetOfflineCommand { get; }

    public LayoutOptions LayoutOptions => FullWidth ? LayoutOptions.Fill : LayoutOptions.Start;
    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LocalOperationModeToggle)bindable).UpdateLayoutOptions();

    private static void OnModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var toggle = (LocalOperationModeToggle)bindable;
        toggle.UpdateVariants();
        if (oldValue is LocalOperationMode previous && previous != (LocalOperationMode)newValue)
            LocalOperationModeService.Mode = (LocalOperationMode)newValue;
    }

    private void UpdateLayoutOptions()
    {
        OnPropertyChanged(nameof(LayoutOptions));
    }

    private void UpdateVariants()
    {
        OnlineVariant = Mode == LocalOperationMode.Online ? ButtonVariant.Primary : ButtonVariant.Ghost;
        OfflineVariant = Mode == LocalOperationMode.Offline ? ButtonVariant.Primary : ButtonVariant.Ghost;
    }

    private void OnServiceModeChanged(object? sender, LocalOperationMode mode)
    {
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(() => Mode = mode);
        else
            Mode = mode;
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
            LocalOperationModeService.ModeChanged -= OnServiceModeChanged;
        else if (args.OldHandler is null)
            LocalOperationModeService.ModeChanged += OnServiceModeChanged;

        base.OnHandlerChanging(args);
    }
}
