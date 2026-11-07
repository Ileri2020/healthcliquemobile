using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class AdminUserManager : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(AdminUserManager), string.Empty);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(AdminUserManager), false);
    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(nameof(SearchText), typeof(string), typeof(AdminUserManager), string.Empty, propertyChanged: OnSearchChanged);
    public static readonly BindableProperty SelectedUserProperty = BindableProperty.Create(nameof(SelectedUser), typeof(AdminUserRecord), typeof(AdminUserManager), null, BindingMode.TwoWay, propertyChanged: OnSelectedUserChanged);
    public static readonly BindableProperty EditRoleProperty = BindableProperty.Create(nameof(EditRole), typeof(string), typeof(AdminUserManager), string.Empty);
    public static readonly BindableProperty EditVerificationStatusProperty = BindableProperty.Create(nameof(EditVerificationStatus), typeof(string), typeof(AdminUserManager), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(AdminUserManager), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanRefreshProperty = BindableProperty.Create(nameof(CanRefresh), typeof(bool), typeof(AdminUserManager), true);
    public static readonly BindableProperty CanSaveProperty = BindableProperty.Create(nameof(CanSave), typeof(bool), typeof(AdminUserManager), false);
    public static readonly BindableProperty HasSelectedUserProperty = BindableProperty.Create(nameof(HasSelectedUser), typeof(bool), typeof(AdminUserManager), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(AdminUserManager), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(AdminUserManager), string.Empty);

    public AdminUserManager()
    {
        InitializeComponent();
        Users = [];
        FilteredUsers = [];
        Roles = ["customer", "professional", "wholesaler", "admin"];
        VerificationStatuses = ["unverified", "pending", "verified", "rejected"];
        RefreshCommand = new Command(async () => await LoadUsersAsync(), () => CanRefresh);
        SaveCommand = new Command(async () => await SaveUserAsync(), () => CanSave);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public AdminUserRecord? SelectedUser { get => (AdminUserRecord?)GetValue(SelectedUserProperty); set => SetValue(SelectedUserProperty, value); }
    public string EditRole { get => (string)GetValue(EditRoleProperty); set => SetValue(EditRoleProperty, value); }
    public string EditVerificationStatus { get => (string)GetValue(EditVerificationStatusProperty); set => SetValue(EditVerificationStatusProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanRefresh { get => (bool)GetValue(CanRefreshProperty); private set => SetValue(CanRefreshProperty, value); }
    public bool CanSave { get => (bool)GetValue(CanSaveProperty); private set => SetValue(CanSaveProperty, value); }
    public bool HasSelectedUser { get => (bool)GetValue(HasSelectedUserProperty); private set => SetValue(HasSelectedUserProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<AdminUserRecord> Users { get; }
    public ObservableCollection<AdminUserRecord> FilteredUsers { get; }
    public ObservableCollection<string> Roles { get; }
    public ObservableCollection<string> VerificationStatuses { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public event Action<object?, Exception>? OperationFailed;

    private static void OnSearchChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AdminUserManager)bindable).ApplyFilter();

    private static void OnSelectedUserChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AdminUserManager)bindable;
        var user = (AdminUserRecord?)newValue;
        control.HasSelectedUser = user is not null;
        control.EditRole = user?.Role ?? string.Empty;
        control.EditVerificationStatus = user?.VerificationStatus ?? string.Empty;
        control.CanSave = user is not null && !control.IsLoading;
        (control.SaveCommand as Command)?.ChangeCanExecute();
    }

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AdminUserManager)bindable;
        control.CanRefresh = !(bool)newValue;
        control.CanSave = !(bool)newValue && control.SelectedUser is not null;
        (control.RefreshCommand as Command)?.ChangeCanExecute();
        (control.SaveCommand as Command)?.ChangeCanExecute();
    }

    private async Task LoadUsersAsync()
    {
        if (!IsAdmin)
        {
            SetStatus("Administrator access is required.");
            return;
        }
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=user");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            await using var stream = await response.Content.ReadAsStreamAsync();
            var users = await JsonSerializer.DeserializeAsync<List<AdminUserRecord>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (users is null)
                throw new JsonException("The user endpoint returned an empty or invalid response.");
            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
            ApplyFilter();
            SetStatus($"Loaded {Users.Count} users.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveUserAsync()
    {
        if (!IsAdmin)
        {
            SetStatus("Administrator access is required.");
            return;
        }
        if (SelectedUser is null || string.IsNullOrWhiteSpace(SelectedUser.Id))
        {
            SetStatus("Select a user with a valid ID.");
            return;
        }

        var user = SelectedUser;
        var payload = new { role = EditRole, verificationStatus = EditVerificationStatus };
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=user&id={Uri.EscapeDataString(user.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, uri, payload);
            user.Role = EditRole;
            user.VerificationStatus = EditVerificationStatus;
            SelectedUser = null;
            ApplyFilter();
            SetStatus($"Updated {user.Email}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var matches = string.IsNullOrEmpty(query)
            ? Users
            : Users.Where(user => user.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || user.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
        FilteredUsers.Clear();
        foreach (var user in matches)
            FilteredUsers.Add(user);
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
