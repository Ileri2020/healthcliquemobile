using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class TermsAgreements : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _ready;
    private bool _isUpdating;
    private bool _reloadRequested;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(TermsAgreements), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(TermsAgreements), string.Empty, propertyChanged: OnUserIdChanged);
    public static readonly BindableProperty AcceptedTermsProperty = BindableProperty.Create(nameof(AcceptedTerms), typeof(bool), typeof(TermsAgreements), false, BindingMode.TwoWay, propertyChanged: OnTermsChanged);
    public static readonly BindableProperty AcceptedPrivacyProperty = BindableProperty.Create(nameof(AcceptedPrivacy), typeof(bool), typeof(TermsAgreements), false, BindingMode.TwoWay, propertyChanged: OnPrivacyChanged);
    public static readonly BindableProperty AcceptedReturnsProperty = BindableProperty.Create(nameof(AcceptedReturns), typeof(bool), typeof(TermsAgreements), false, BindingMode.TwoWay, propertyChanged: OnReturnsChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(TermsAgreements), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(TermsAgreements), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(TermsAgreements), string.Empty);

    public TermsAgreements()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _ready = true;
            _ = LoadAgreementsAsync();
        };
        OpenTermsCommand = new Command(async () => await OpenPolicyAsync("terms"));
        OpenPrivacyCommand = new Command(async () => await OpenPolicyAsync("privacy"));
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public bool AcceptedTerms { get => (bool)GetValue(AcceptedTermsProperty); set => SetValue(AcceptedTermsProperty, value); }
    public bool AcceptedPrivacy { get => (bool)GetValue(AcceptedPrivacyProperty); set => SetValue(AcceptedPrivacyProperty, value); }
    public bool AcceptedReturns { get => (bool)GetValue(AcceptedReturnsProperty); set => SetValue(AcceptedReturnsProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasUser => !string.IsNullOrWhiteSpace(UserId) && UserId != "nil";
    public bool CanToggle => !IsLoading && HasUser && !string.IsNullOrWhiteSpace(ApiBaseUrl);
    public ICommand OpenTermsCommand { get; }
    public ICommand OpenPrivacyCommand { get; }
    public event Action<object?, UserAgreementState>? AgreementsChanged;
    public event Action<object?, Exception>? RequestFailed;
    public event Action<object?, string>? NavigationRequested;

    private static void OnUserIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (TermsAgreements)bindable;
        control.OnPropertyChanged(nameof(HasUser));
        control.OnPropertyChanged(nameof(CanToggle));
        if (control._ready)
            _ = control.LoadAgreementsAsync();
    }

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (TermsAgreements)bindable;
        control.OnPropertyChanged(nameof(CanToggle));
        if (control._ready)
            _ = control.LoadAgreementsAsync();
    }

    private static void OnTermsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TermsAgreements)bindable).OnAgreementChanged("acceptedTerms", oldValue, newValue);

    private static void OnPrivacyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TermsAgreements)bindable).OnAgreementChanged("acceptedPrivacy", oldValue, newValue);

    private static void OnReturnsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TermsAgreements)bindable).OnAgreementChanged("acceptedReturns", oldValue, newValue);

    private void OnAgreementChanged(string field, object oldValue, object newValue)
    {
        OnPropertyChanged(nameof(AllAccepted));
        if (_ready && !_isUpdating)
            _ = SaveChangeAsync(field, oldValue, newValue);
    }

    public bool AllAccepted => AcceptedTerms && AcceptedPrivacy && AcceptedReturns;

    private async Task LoadAgreementsAsync()
    {
        if (!HasUser || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        if (IsLoading)
        {
            _reloadRequested = true;
            return;
        }
        var userId = UserId;
        var apiBaseUrl = ApiBaseUrl;
        IsLoading = true;
        OnPropertyChanged(nameof(CanToggle));
        try
        {
            var path = $"api/dbhandler?model=user&id={Uri.EscapeDataString(userId)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(apiBaseUrl, path), (object?)null);
            var state = await response.Content.ReadFromJsonAsync<UserAgreementState>(JsonOptions)
                ?? throw new JsonException("The account endpoint returned an empty agreement state.");
            if (userId != UserId || apiBaseUrl != ApiBaseUrl)
            {
                _reloadRequested = true;
                return;
            }
            _isUpdating = true;
            AcceptedTerms = state.AcceptedTerms;
            AcceptedPrivacy = state.AcceptedPrivacy;
            AcceptedReturns = state.AcceptedReturns;
            OnPropertyChanged(nameof(AllAccepted));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException or JsonException)
        {
            SetError(exception);
        }
        finally
        {
            _isUpdating = false;
            IsLoading = false;
            OnPropertyChanged(nameof(CanToggle));
            if (_reloadRequested)
            {
                _reloadRequested = false;
                await LoadAgreementsAsync();
            }
        }
    }

    private async Task SaveChangeAsync(string propertyName, object oldValue, object newValue)
    {
        if (IsLoading)
        {
            RevertAgreement(propertyName, oldValue);
            return;
        }
        if (string.IsNullOrWhiteSpace(UserId) || UserId == "nil" || string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            RevertAgreement(propertyName, oldValue);
            SetError(new InvalidOperationException("A signed-in user and API base URL are required to update agreements."));
            return;
        }

        var field = (bool)newValue;
        IsLoading = true;
        HasStatus = false;
        OnPropertyChanged(nameof(CanToggle));
        try
        {
            var path = $"api/dbhandler?model=user&id={Uri.EscapeDataString(UserId)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, UtilityApi.CreateUri(ApiBaseUrl, path),
                new Dictionary<string, bool> { [propertyName] = field });
            StatusMessage = "Agreement updated successfully.";
            HasStatus = true;
            AgreementsChanged?.Invoke(this, new UserAgreementState
            {
                AcceptedTerms = AcceptedTerms,
                AcceptedPrivacy = AcceptedPrivacy,
                AcceptedReturns = AcceptedReturns
            });
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException or JsonException)
        {
            RevertAgreement(propertyName, oldValue);
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanToggle));
        }
    }

    private void RevertAgreement(string propertyName, object value)
    {
        _isUpdating = true;
        try
        {
            if (propertyName == "acceptedTerms") AcceptedTerms = (bool)value;
            else if (propertyName == "acceptedPrivacy") AcceptedPrivacy = (bool)value;
            else AcceptedReturns = (bool)value;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task OpenPolicyAsync(string route)
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl to open the policy."));
            return;
        }
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, route);
            NavigationRequested?.Invoke(this, uri.ToString());
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception exception) when (exception is InvalidOperationException or UriFormatException or FeatureNotSupportedException)
        {
            SetError(exception);
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }
}
