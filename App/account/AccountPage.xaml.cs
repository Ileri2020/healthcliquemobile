using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class AccountPage : ContentPage
{
    private bool _isSignedIn;
    private bool _isSigningUp;
    private bool _isLoading;
    private bool _hasStatus;
    private string _statusMessage = string.Empty;
    private CommerceChatUser? _currentUser;

    public AccountPage()
    {
        InitializeComponent();
        BindingContext = this;
        SignOutCommand = new Command(async () => await SignOutAsync(), () => !IsLoading);
        OpenSalesCommand = new Command(async () => await PageLayout.NavigateToAsync("/sales"));
        OpenStockCommand = new Command(async () => await PageLayout.NavigateToAsync("/stock"));
        AccountLogin.SignUpHostCommand = new Command(() => IsSigningUp = true);
        AccountLogin.LoginSucceeded += (_, user) => SetCurrentUser(user);
        AccountLogin.SocialSignInRequested += async (_, provider) =>
            await DisplayAlertAsync("Social sign-in", $"Continue signing in with {provider} on the HealthClique website.", "OK");
        AccountLogin.RequestFailed += (_, exception) => SetStatus(exception.Message);
        AccountSignup.LoginRequested += (_, _) => IsSigningUp = false;
        AccountSignup.SignupSucceeded += (_, _) =>
        {
            IsSigningUp = false;
            SetStatus("Account created. Sign in to continue.");
        };
        AccountSignup.RequestFailed += (_, exception) => SetStatus(exception.Message);
        EditProfile.ProfileSaved += (_, profile) =>
        {
            if (CurrentUser is not null)
            {
                CurrentUser.Name = profile.Name;
                CurrentUser.Email = profile.Email;
                CurrentUser.Contact = profile.Contact;
                CurrentUser.Role = profile.Role;
                CurrentUser.AvatarUrl = profile.AvatarUrl;
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(RoleText));
                OnPropertyChanged(nameof(HasInventoryAccess));
                OnPropertyChanged(nameof(IsCustomer));
                OnPropertyChanged(nameof(IsAdmin));
                CommerceSessionState.CurrentUser = CurrentUser;
            }
        };
        EditProfile.RequestFailed += (_, exception) => SetStatus(exception.Message);
        UserManager.OperationFailed += (_, exception) => SetStatus(exception.Message);
        BulkManager.OperationFailed += (_, exception) => SetStatus(exception.Message);
        UpgradeAccount.UpgradeSubmitted += (_, request) =>
        {
            if (CurrentUser is null)
                return;
            CurrentUser.Role = request.Role;
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(RoleText));
            OnPropertyChanged(nameof(HasInventoryAccess));
            OnPropertyChanged(nameof(IsCustomer));
            OnPropertyChanged(nameof(IsAdmin));
            CommerceSessionState.CurrentUser = CurrentUser;
        };
        UpgradeAccount.SubmissionFailed += (_, exception) => SetStatus(exception.Message);
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await LoadSessionAsync();
    }

    public CommerceChatUser? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser == value)
                return;
            _currentUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RoleText));
            OnPropertyChanged(nameof(HasInventoryAccess));
            OnPropertyChanged(nameof(IsCustomer));
            OnPropertyChanged(nameof(IsAdmin));
        }
    }

    public bool IsSignedIn
    {
        get => _isSignedIn;
        private set
        {
            if (_isSignedIn == value)
                return;
            _isSignedIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignedOut));
            OnPropertyChanged(nameof(IsLoginVisible));
            OnPropertyChanged(nameof(IsSignupVisible));
        }
    }

    public bool IsSignedOut => !IsSignedIn;
    public bool IsSigningUp
    {
        get => _isSigningUp;
        private set
        {
            if (_isSigningUp == value)
                return;
            _isSigningUp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLoginVisible));
            OnPropertyChanged(nameof(IsSignupVisible));
        }
    }
    public bool IsLoginVisible => !IsSignedIn && !IsSigningUp;
    public bool IsSignupVisible => !IsSignedIn && IsSigningUp;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;
            _isLoading = value;
            OnPropertyChanged();
            (SignOutCommand as Command)?.ChangeCanExecute();
        }
    }
    public bool HasStatus { get => _hasStatus; private set { _hasStatus = value; OnPropertyChanged(); } }
    public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
    public string RoleText => $"Account type: {CurrentUser?.Role ?? "customer"}";
    public bool IsAdmin => string.Equals(CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase);
    public bool HasInventoryAccess => IsAdmin || string.Equals(CurrentUser?.Role, "staff", StringComparison.OrdinalIgnoreCase);
    public bool IsCustomer => CurrentUser is not null && !HasInventoryAccess
        && !CurrentUser.Role.Equals("professional", StringComparison.OrdinalIgnoreCase)
        && !CurrentUser.Role.Equals("wholesaler", StringComparison.OrdinalIgnoreCase);
    public ICommand SignOutCommand { get; }
    public ICommand OpenSalesCommand { get; }
    public ICommand OpenStockCommand { get; }

    private async Task LoadSessionAsync()
    {
        if (IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            if (user is not null)
                SetCurrentUser(user);
            else
                SetCurrentUser(null);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SignOutAsync()
    {
        if (IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            await UtilityApi.SignOutAsync(PageLayout.ApiBaseUrl);
            SetCurrentUser(null);
            StatusMessage = "You have signed out.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetCurrentUser(CommerceChatUser? user)
    {
        CurrentUser = user;
        IsSignedIn = user is not null;
        CommerceSessionState.CurrentUser = user;
        if (user is null)
        {
            EditProfile.UserProfile = new UserEditProfile();
            UpgradeAccount.UserId = string.Empty;
            UpgradeAccount.UserRole = string.Empty;
            return;
        }

        EditProfile.UserProfile = new UserEditProfile
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Contact = user.Contact,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl
        };
        UpgradeAccount.UserId = user.Id;
        UpgradeAccount.UserRole = user.Role;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
