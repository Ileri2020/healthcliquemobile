using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace EEE.Components.Utility;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public partial class Auth : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Auth), string.Empty);
    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(Auth), string.Empty);
    public static readonly BindableProperty PasswordProperty = BindableProperty.Create(nameof(Password), typeof(string), typeof(Auth), string.Empty);
    public static readonly BindableProperty LoginCommandProperty = BindableProperty.Create(nameof(LoginCommand), typeof(ICommand), typeof(Auth));
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Auth), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(Auth), true);
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(Auth), "Sign in");
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Auth), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Auth), string.Empty);

    public Auth()
    {
        InitializeComponent();
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
    }

    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Password { get => (string)GetValue(PasswordProperty); set => SetValue(PasswordProperty, value); }
    public ICommand? LoginCommand { get => (ICommand?)GetValue(LoginCommandProperty); set => SetValue(LoginCommandProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ICommand SubmitCommand { get; }
    public event Action<object?, LoginRequest>? LoginRequested;
    public event Action<object?, CommerceChatUser>? LoginSucceeded;

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Auth)bindable;
        control.CanSubmit = !(bool)newValue;
        control.SubmitText = (bool)newValue ? "Signing in..." : "Sign in";
        (control.SubmitCommand as Command)?.ChangeCanExecute();
    }

    private async Task SubmitAsync()
    {
        if (IsLoading)
            return;
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Enter a valid email address and password.";
            HasStatus = true;
            return;
        }

        var request = new LoginRequest { Email = Email, Password = Password };
        LoginRequested?.Invoke(this, request);
        try
        {
            IsLoading = true;
            HasStatus = false;
            if (LoginCommand is IAsyncRelayCommand asyncCommand)
                await asyncCommand.ExecuteAsync(request);
            else if (LoginCommand is not null && LoginCommand.CanExecute(request))
                LoginCommand.Execute(request);
            else if (!string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                var user = await UtilityApi.SignInAsync(ApiBaseUrl, request.Email, request.Password);
                LoginSucceeded?.Invoke(this, user);
                StatusMessage = $"Signed in as {user.Name}.";
                HasStatus = true;
            }
            else
            {
                StatusMessage = "Set ApiBaseUrl or connect LoginCommand to the app's authentication service.";
                HasStatus = true;
            }
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
