using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class Signup : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty PasswordProperty = BindableProperty.Create(nameof(Password), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(nameof(Role), typeof(string), typeof(Signup), "customer", propertyChanged: OnRoleChanged);
    public static readonly BindableProperty ProfessionalTypeProperty = BindableProperty.Create(nameof(ProfessionalType), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty RegNumberProperty = BindableProperty.Create(nameof(RegNumber), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty FacilityNameProperty = BindableProperty.Create(nameof(FacilityName), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty FacilityAddressProperty = BindableProperty.Create(nameof(FacilityAddress), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty FacilityRegNumberProperty = BindableProperty.Create(nameof(FacilityRegNumber), typeof(string), typeof(Signup), string.Empty);
    public static readonly BindableProperty IsSubmittingProperty = BindableProperty.Create(nameof(IsSubmitting), typeof(bool), typeof(Signup), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Signup), false);
    public static readonly BindableProperty HasSuccessProperty = BindableProperty.Create(nameof(HasSuccess), typeof(bool), typeof(Signup), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Signup), string.Empty);

    public Signup()
    {
        InitializeComponent();
        ProfessionalTypes = ["pharmacist", "doctor", "nurse"];
        SelectRoleCommand = new Command<string>(value => Role = value ?? "customer");
        SubmitCommand = new Command(async () => await SubmitAsync(), () => !IsSubmitting);
        LoginCommand = new Command(() => LoginRequested?.Invoke(this, EventArgs.Empty));
        GoogleSignInCommand = new Command(() => GoogleSignInRequested?.Invoke(this, EventArgs.Empty));
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string Password { get => (string)GetValue(PasswordProperty); set => SetValue(PasswordProperty, value); }
    public string Role { get => (string)GetValue(RoleProperty); set => SetValue(RoleProperty, value); }
    public string ProfessionalType { get => (string)GetValue(ProfessionalTypeProperty); set => SetValue(ProfessionalTypeProperty, value); }
    public string RegNumber { get => (string)GetValue(RegNumberProperty); set => SetValue(RegNumberProperty, value); }
    public string FacilityName { get => (string)GetValue(FacilityNameProperty); set => SetValue(FacilityNameProperty, value); }
    public string FacilityAddress { get => (string)GetValue(FacilityAddressProperty); set => SetValue(FacilityAddressProperty, value); }
    public string FacilityRegNumber { get => (string)GetValue(FacilityRegNumberProperty); set => SetValue(FacilityRegNumberProperty, value); }
    public bool IsSubmitting { get => (bool)GetValue(IsSubmittingProperty); private set => SetValue(IsSubmittingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public bool HasSuccess { get => (bool)GetValue(HasSuccessProperty); private set => SetValue(HasSuccessProperty, value); }
    public bool HasError => HasStatus && !HasSuccess;
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool IsProfessional => Role == "professional";
    public bool IsWholesaler => Role == "wholesaler";
    public bool CanSubmit => !IsSubmitting;
    public string SubmitText => IsSubmitting ? "Creating account..." : "Sign up";
    public ButtonVariant CustomerVariant => Role == "customer" ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public ButtonVariant ProfessionalVariant => Role == "professional" ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public ButtonVariant WholesalerVariant => Role == "wholesaler" ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public IReadOnlyList<string> ProfessionalTypes { get; }
    public ICommand SelectRoleCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand LoginCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public event Action<object?, CommerceChatUser>? SignupSucceeded;
    public event EventHandler? LoginRequested;
    public event EventHandler? GoogleSignInRequested;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnRoleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Signup)bindable;
        control.OnPropertyChanged(nameof(IsProfessional));
        control.OnPropertyChanged(nameof(IsWholesaler));
        control.OnPropertyChanged(nameof(CustomerVariant));
        control.OnPropertyChanged(nameof(ProfessionalVariant));
        control.OnPropertyChanged(nameof(WholesalerVariant));
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl before creating an account."));
            return;
        }
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) || Password.Length < 8)
        {
            SetError(new ArgumentException("Enter your name, a valid email address, and a password of at least 8 characters."));
            return;
        }
        if (IsProfessional && (string.IsNullOrWhiteSpace(ProfessionalType) || string.IsNullOrWhiteSpace(RegNumber)))
        {
            SetError(new ArgumentException("Professional type and registration number are required."));
            return;
        }
        if (IsWholesaler && (string.IsNullOrWhiteSpace(FacilityName) || string.IsNullOrWhiteSpace(FacilityAddress) || string.IsNullOrWhiteSpace(FacilityRegNumber)))
        {
            SetError(new ArgumentException("Complete all facility registration fields."));
            return;
        }

        IsSubmitting = true;
        HasStatus = false;
        HasSuccess = false;
        UpdateSubmitState();
        try
        {
            var request = new ProductSignupRequest
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                Password = Password,
                Role = Role,
                ProfessionalType = ProfessionalType,
                RegNumber = RegNumber.Trim(),
                FacilityName = FacilityName.Trim(),
                FacilityAddress = FacilityAddress.Trim(),
                FacilityRegNumber = FacilityRegNumber.Trim(),
            };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/auth/register"), request);
            var user = await response.Content.ReadFromJsonAsync<CommerceChatUser>(JsonOptions)
                ?? throw new JsonException("The registration endpoint returned an empty response.");
            Password = string.Empty;
            HasStatus = true;
            HasSuccess = true;
            StatusMessage = "Account created. Sign in to continue.";
            OnPropertyChanged(nameof(HasError));
            SignupSucceeded?.Invoke(this, user);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsSubmitting = false;
            UpdateSubmitState();
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        HasSuccess = false;
        StatusMessage = exception.Message;
        OnPropertyChanged(nameof(HasError));
        RequestFailed?.Invoke(this, exception);
    }

    private void UpdateSubmitState()
    {
        OnPropertyChanged(nameof(SubmitText));
        OnPropertyChanged(nameof(CanSubmit));
        (SubmitCommand as Command)?.ChangeCanExecute();
    }
}

public sealed class SignupForm : Signup { }
