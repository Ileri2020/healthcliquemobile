using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class AccountUpgrade : ContentView
{
    private static readonly HttpClient HttpClient = new();

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(AccountUpgrade), string.Empty, propertyChanged: OnRoleChanged);
    public static readonly BindableProperty IsCustomerProperty = BindableProperty.Create(nameof(IsCustomer), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(nameof(Role), typeof(string), typeof(AccountUpgrade), string.Empty, propertyChanged: OnRoleChanged);
    public static readonly BindableProperty IsRoleStepProperty = BindableProperty.Create(nameof(IsRoleStep), typeof(bool), typeof(AccountUpgrade), true);
    public static readonly BindableProperty IsFormStepProperty = BindableProperty.Create(nameof(IsFormStep), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty IsSuccessStepProperty = BindableProperty.Create(nameof(IsSuccessStep), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty IsProfessionalProperty = BindableProperty.Create(nameof(IsProfessional), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty IsWholesalerProperty = BindableProperty.Create(nameof(IsWholesaler), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty ProfessionalTypeProperty = BindableProperty.Create(nameof(ProfessionalType), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty RegNumberProperty = BindableProperty.Create(nameof(RegNumber), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty FacilityNameProperty = BindableProperty.Create(nameof(FacilityName), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty FacilityAddressProperty = BindableProperty.Create(nameof(FacilityAddress), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty FacilityRegNumberProperty = BindableProperty.Create(nameof(FacilityRegNumber), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(AccountUpgrade), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(AccountUpgrade), true);
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(AccountUpgrade), "Apply Upgrade");
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(AccountUpgrade), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(AccountUpgrade), string.Empty);
    public static readonly BindableProperty HeadingProperty = BindableProperty.Create(nameof(Heading), typeof(string), typeof(AccountUpgrade), "Upgrade Your Account");
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(AccountUpgrade), "Get access to bulk services, professional rates, and specialized medical tools.");

    public AccountUpgrade()
    {
        InitializeComponent();
        ProfessionalTypes = ["pharmacist", "doctor", "nurse"];
        OpenCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(Close);
        SelectProfessionalCommand = new Command(() => SelectRole("professional"));
        SelectWholesalerCommand = new Command(() => SelectRole("wholesaler"));
        BackCommand = new Command(() => SetStep(1));
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
        UpdateRoleState();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public bool IsCustomer { get => (bool)GetValue(IsCustomerProperty); private set => SetValue(IsCustomerProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Role { get => (string)GetValue(RoleProperty); private set => SetValue(RoleProperty, value); }
    public bool IsRoleStep { get => (bool)GetValue(IsRoleStepProperty); private set => SetValue(IsRoleStepProperty, value); }
    public bool IsFormStep { get => (bool)GetValue(IsFormStepProperty); private set => SetValue(IsFormStepProperty, value); }
    public bool IsSuccessStep { get => (bool)GetValue(IsSuccessStepProperty); private set => SetValue(IsSuccessStepProperty, value); }
    public bool IsProfessional { get => (bool)GetValue(IsProfessionalProperty); private set => SetValue(IsProfessionalProperty, value); }
    public bool IsWholesaler { get => (bool)GetValue(IsWholesalerProperty); private set => SetValue(IsWholesalerProperty, value); }
    public string ProfessionalType { get => (string)GetValue(ProfessionalTypeProperty); set => SetValue(ProfessionalTypeProperty, value); }
    public string RegNumber { get => (string)GetValue(RegNumberProperty); set => SetValue(RegNumberProperty, value); }
    public string FacilityName { get => (string)GetValue(FacilityNameProperty); set => SetValue(FacilityNameProperty, value); }
    public string FacilityAddress { get => (string)GetValue(FacilityAddressProperty); set => SetValue(FacilityAddressProperty, value); }
    public string FacilityRegNumber { get => (string)GetValue(FacilityRegNumberProperty); set => SetValue(FacilityRegNumberProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string Heading { get => (string)GetValue(HeadingProperty); private set => SetValue(HeadingProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); private set => SetValue(DescriptionProperty, value); }
    public ObservableCollection<string> ProfessionalTypes { get; }
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SelectProfessionalCommand { get; }
    public ICommand SelectWholesalerCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SubmitCommand { get; }
    public event Action<object?, AccountUpgradeRequest>? UpgradeSubmitted;
    public event Action<object?, Exception>? SubmissionFailed;

    private static void OnRoleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AccountUpgrade)bindable).UpdateRoleState();

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AccountUpgrade)bindable;
        control.CanSubmit = !(bool)newValue;
        control.SubmitText = (bool)newValue ? "Submitting..." : "Apply Upgrade";
        (control.SubmitCommand as Command)?.ChangeCanExecute();
    }

    private void UpdateRoleState()
    {
        IsCustomer = string.Equals(UserRole, "customer", StringComparison.OrdinalIgnoreCase);
        IsProfessional = Role == "professional";
        IsWholesaler = Role == "wholesaler";
        Heading = IsSuccessStep ? "Application Submitted!" : IsFormStep ? $"Verify as {Role}" : "Upgrade Your Account";
        Description = IsFormStep
            ? "Please provide your business or professional details for verification."
            : "Get access to bulk services, professional rates, and specialized medical tools.";
    }

    private void SelectRole(string role)
    {
        Role = role;
        HasStatus = false;
        SetStep(2);
    }

    private void SetStep(int step)
    {
        IsRoleStep = step == 1;
        IsFormStep = step == 2;
        IsSuccessStep = step == 3;
        UpdateRoleState();
    }

    private void Close()
    {
        IsOpen = false;
        Role = string.Empty;
        HasStatus = false;
        SetStep(1);
    }

    private async Task SubmitAsync()
    {
        if (IsLoading)
            return;
        if (string.IsNullOrWhiteSpace(UserId) || UserId == "nil")
        {
            SetError("A valid signed-in user ID is required.");
            return;
        }
        if (Role == "professional" && (string.IsNullOrWhiteSpace(ProfessionalType) || string.IsNullOrWhiteSpace(RegNumber)))
        {
            SetError("Select a professional type and enter its registration number.");
            return;
        }
        if (Role == "wholesaler" && (string.IsNullOrWhiteSpace(FacilityName) || string.IsNullOrWhiteSpace(FacilityRegNumber)))
        {
            SetError("Enter the facility name and registration number.");
            return;
        }

        var request = new AccountUpgradeRequest
        {
            UserId = UserId,
            Role = Role,
            ProfessionalType = ProfessionalType,
            RegNumber = RegNumber,
            FacilityName = FacilityName,
            FacilityAddress = FacilityAddress,
            FacilityRegNumber = FacilityRegNumber
        };
        IsLoading = true;
        HasStatus = false;
        try
        {
            var path = $"api/dbhandler?model=user&id={Uri.EscapeDataString(UserId)}";
            var payload = new
            {
                id = UserId,
                role = Role,
                verificationStatus = request.VerificationStatus,
                professionalType = request.ProfessionalType,
                regNumber = request.RegNumber,
                facilityName = request.FacilityName,
                facilityAddress = request.FacilityAddress,
                facilityRegNumber = request.FacilityRegNumber
            };
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, UtilityApi.CreateUri(ApiBaseUrl, path), payload);
            UpgradeSubmitted?.Invoke(this, request);
            SetStep(3);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception.Message);
            SubmissionFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
