using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class GoogleSignIn : ContentView
{
    public static readonly BindableProperty SignInCommandProperty = BindableProperty.Create(nameof(SignInCommand), typeof(ICommand), typeof(GoogleSignIn));
    public static readonly BindableProperty HostSignInCommandProperty = BindableProperty.Create(nameof(HostSignInCommand), typeof(ICommand), typeof(GoogleSignIn));
    public static readonly BindableProperty ButtonTextProperty = BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(GoogleSignIn), "Continue with Google");
    public static readonly BindableProperty CanSignInProperty = BindableProperty.Create(nameof(CanSignIn), typeof(bool), typeof(GoogleSignIn), true, propertyChanged: OnCommandStateChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(GoogleSignIn), false, propertyChanged: OnCommandStateChanged);

    public GoogleSignIn()
    {
        InitializeComponent();
        SignInCommand = new Command(async () =>
        {
            if (IsLoading)
                return;
            IsLoading = true;
            try
            {
                if (HostSignInCommand is not null && HostSignInCommand.CanExecute(null))
                    HostSignInCommand.Execute(null);
                else
                    SignInRequested?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        }, () => CanSignIn && !IsLoading);
    }

    public ICommand? HostSignInCommand { get => (ICommand?)GetValue(HostSignInCommandProperty); set => SetValue(HostSignInCommandProperty, value); }
    public ICommand SignInCommand { get; }
    public string ButtonText { get => (string)GetValue(ButtonTextProperty); set => SetValue(ButtonTextProperty, value); }
    public bool CanSignIn { get => (bool)GetValue(CanSignInProperty); set => SetValue(CanSignInProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public event EventHandler? SignInRequested;

    private static void OnCommandStateChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((GoogleSignIn)bindable).UpdateCommand();

    private void UpdateCommand() => (SignInCommand as Command)?.ChangeCanExecute();
}
