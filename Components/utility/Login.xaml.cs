using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public enum LoginStep
{
    Login,
    VerifyEmail,
    SetPassword,
    SignUp
}

public sealed class VerificationResponse
{
    public string Message { get; set; } = string.Empty;
}

public partial class Login : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty PasswordProperty = BindableProperty.Create(nameof(Password), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty VerificationCodeProperty = BindableProperty.Create(nameof(VerificationCode), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty NewPasswordProperty = BindableProperty.Create(nameof(NewPassword), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty ConfirmPasswordProperty = BindableProperty.Create(nameof(ConfirmPassword), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(LoginStep), typeof(Login), LoginStep.Login, propertyChanged: OnStepChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Login), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(Login), true);
    public static readonly BindableProperty HasErrorProperty = BindableProperty.Create(nameof(HasError), typeof(bool), typeof(Login), false);
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty HasSuccessProperty = BindableProperty.Create(nameof(HasSuccess), typeof(bool), typeof(Login), false);
    public static readonly BindableProperty SuccessMessageProperty = BindableProperty.Create(nameof(SuccessMessage), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(Login), "Login →");
    public static readonly BindableProperty VerificationTextProperty = BindableProperty.Create(nameof(VerificationText), typeof(string), typeof(Login), "Send verification code");
    public static readonly BindableProperty SetPasswordTextProperty = BindableProperty.Create(nameof(SetPasswordText), typeof(string), typeof(Login), "Set password");
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Login), "Login to HealthClique");
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(Login), string.Empty);
    public static readonly BindableProperty GoogleSignInHostCommandProperty = BindableProperty.Create(nameof(GoogleSignInHostCommand), typeof(ICommand), typeof(Login));
    public static readonly BindableProperty FacebookSignInHostCommandProperty = BindableProperty.Create(nameof(FacebookSignInHostCommand), typeof(ICommand), typeof(Login));
    public static readonly BindableProperty SignUpHostCommandProperty = BindableProperty.Create(nameof(SignUpHostCommand), typeof(ICommand), typeof(Login));

    public Login()
    {
        InitializeComponent();
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
        SendVerificationCommand = new Command(async () => await SendVerificationCodeAsync(), () => CanSubmit);
        SetPasswordCommand = new Command(async () => await VerifyAndSetPasswordAsync(), () => CanSubmit);
        CancelVerificationCommand = new Command(() => Step = LoginStep.Login);
        ReturnToLoginCommand = new Command(() => Step = LoginStep.Login);
        SignUpCommand = new Command(() =>
        {
            if (SignUpHostCommand?.CanExecute(null) == true)
                SignUpHostCommand.Execute(null);
            else
            {
                Step = LoginStep.SignUp;
                SignUpRequested?.Invoke(this, EventArgs.Empty);
            }
        });
        GoogleSignInCommand = new Command(() => RequestSocialSignIn("google", GoogleSignInHostCommand));
        FacebookSignInCommand = new Command(() => RequestSocialSignIn("facebook", FacebookSignInHostCommand));
        UpdateStepText();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string Password { get => (string)GetValue(PasswordProperty); set => SetValue(PasswordProperty, value); }
    public string VerificationCode { get => (string)GetValue(VerificationCodeProperty); set => SetValue(VerificationCodeProperty, value); }
    public string NewPassword { get => (string)GetValue(NewPasswordProperty); set => SetValue(NewPasswordProperty, value); }
    public string ConfirmPassword { get => (string)GetValue(ConfirmPasswordProperty); set => SetValue(ConfirmPasswordProperty, value); }
    public LoginStep Step { get => (LoginStep)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public bool HasError { get => (bool)GetValue(HasErrorProperty); private set => SetValue(HasErrorProperty, value); }
    public string ErrorMessage { get => (string)GetValue(ErrorMessageProperty); private set => SetValue(ErrorMessageProperty, value); }
    public bool HasSuccess { get => (bool)GetValue(HasSuccessProperty); private set => SetValue(HasSuccessProperty, value); }
    public string SuccessMessage { get => (string)GetValue(SuccessMessageProperty); private set => SetValue(SuccessMessageProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public string VerificationText { get => (string)GetValue(VerificationTextProperty); private set => SetValue(VerificationTextProperty, value); }
    public string SetPasswordText { get => (string)GetValue(SetPasswordTextProperty); private set => SetValue(SetPasswordTextProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); private set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); private set => SetValue(DescriptionProperty, value); }
    public ICommand? GoogleSignInHostCommand { get => (ICommand?)GetValue(GoogleSignInHostCommandProperty); set => SetValue(GoogleSignInHostCommandProperty, value); }
    public ICommand? FacebookSignInHostCommand { get => (ICommand?)GetValue(FacebookSignInHostCommandProperty); set => SetValue(FacebookSignInHostCommandProperty, value); }
    public ICommand? SignUpHostCommand { get => (ICommand?)GetValue(SignUpHostCommandProperty); set => SetValue(SignUpHostCommandProperty, value); }
    public ICommand SubmitCommand { get; }
    public ICommand SendVerificationCommand { get; }
    public ICommand SetPasswordCommand { get; }
    public ICommand CancelVerificationCommand { get; }
    public ICommand ReturnToLoginCommand { get; }
    public ICommand SignUpCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public ICommand FacebookSignInCommand { get; }
    public bool IsLoginStep => Step == LoginStep.Login;
    public bool IsVerifyStep => Step == LoginStep.VerifyEmail;
    public bool IsSetPasswordStep => Step == LoginStep.SetPassword;
    public bool IsSignUpStep => Step == LoginStep.SignUp;
    public event Action<object?, CommerceChatUser>? LoginSucceeded;
    public event Action<object?, string>? SocialSignInRequested;
    public event EventHandler? SignUpRequested;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnStepChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Login)bindable).UpdateStepText();

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Login)bindable;
        control.CanSubmit = !(bool)newValue;
        control.SubmitText = (bool)newValue ? "Logging in..." : "Login →";
        control.VerificationText = (bool)newValue ? "Sending..." : "Send verification code";
        control.SetPasswordText = (bool)newValue ? "Saving..." : "Set password";
        (control.SubmitCommand as Command)?.ChangeCanExecute();
        (control.SendVerificationCommand as Command)?.ChangeCanExecute();
        (control.SetPasswordCommand as Command)?.ChangeCanExecute();
    }

    private void UpdateStepText()
    {
        OnPropertyChanged(nameof(IsLoginStep));
        OnPropertyChanged(nameof(IsVerifyStep));
        OnPropertyChanged(nameof(IsSetPasswordStep));
        OnPropertyChanged(nameof(IsSignUpStep));
        if (Step == LoginStep.Login)
        {
            Title = "Login to HealthClique";
            Description = string.Empty;
        }
        else if (Step == LoginStep.VerifyEmail)
        {
            Title = "Verify your email";
            Description = "We need to verify your email address.";
        }
        else if (Step == LoginStep.SetPassword)
        {
            Title = "Set your password";
            Description = "Check your email for the verification code.";
        }
        else
        {
            Title = "Create a HealthClique account";
            Description = "Continue with your app's configured account registration flow.";
        }
    }

    private async Task SubmitAsync()
    {
        if (!IsValidEmail(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Enter a valid email address and password.");
            return;
        }
        SetLoading(true);
        try
        {
            HasError = false;
            HasSuccess = false;
            var user = await UtilityApi.SignInAsync(ApiBaseUrl, Email.Trim(), Password);
            LoginSucceeded?.Invoke(this, user);
            SuccessMessage = "Login successful.";
            HasSuccess = true;
            ResetCredentials();
        }
        catch (HttpRequestException exception) when (exception.Message.Contains("requiresEmailVerification", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = exception.Message;
            HasError = true;
            Step = LoginStep.VerifyEmail;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task SendVerificationCodeAsync()
    {
        if (!IsValidEmail(Email))
        {
            SetError("Enter a valid email address.");
            return;
        }
        SetLoading(true);
        try
        {
            HasError = false;
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/auth/send-verification-code"), new { email = Email.Trim() });
            SuccessMessage = "Verification code sent to your email.";
            HasSuccess = true;
            Step = LoginStep.SetPassword;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task VerifyAndSetPasswordAsync()
    {
        if (NewPassword != ConfirmPassword)
        {
            SetError("Passwords do not match.");
            return;
        }
        if (NewPassword.Length < 6)
        {
            SetError("Password must be at least 6 characters long.");
            return;
        }
        SetLoading(true);
        try
        {
            HasError = false;
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, "api/auth/verify-code-set-password"), new
            {
                email = Email.Trim(),
                code = VerificationCode,
                password = NewPassword
            });
            var result = await response.Content.ReadFromJsonAsync<VerificationResponse>(JsonOptions)
                ?? throw new JsonException("Password verification API returned an invalid response.");
            SuccessMessage = string.IsNullOrWhiteSpace(result.Message) ? "Password updated." : result.Message;
            HasSuccess = true;
            Step = LoginStep.Login;
            ResetCredentials();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void RequestSocialSignIn(string provider, ICommand? command)
    {
        if (command?.CanExecute(null) == true)
            command.Execute(null);
        else
            SocialSignInRequested?.Invoke(this, provider);
    }

    private void SetLoading(bool value) => IsLoading = value;

    private void SetError(Exception exception)
    {
        ErrorMessage = exception.Message;
        HasError = true;
        HasSuccess = false;
        RequestFailed?.Invoke(this, exception);
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        HasSuccess = false;
    }

    private void ResetCredentials()
    {
        Email = string.Empty;
        Password = string.Empty;
        VerificationCode = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.IndexOf('@') < email.LastIndexOf('.');
}
