using System.Windows.Input;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace EEE.Components.Utility;

public partial class LoginPopup : ContentView
{
    private const string HasPoppedPreferenceKey = "health-clique-login-popped";
    private IDispatcherTimer? _popupTimer;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(LoginPopup), string.Empty);
    public static readonly BindableProperty IsAuthenticatedProperty = BindableProperty.Create(nameof(IsAuthenticated), typeof(bool), typeof(LoginPopup), false, propertyChanged: OnAuthStateChanged);
    public static readonly BindableProperty AuthenticationStateKnownProperty = BindableProperty.Create(nameof(AuthenticationStateKnown), typeof(bool), typeof(LoginPopup), false, propertyChanged: OnAuthStateChanged);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(LoginPopup), false, BindingMode.TwoWay);
    public static readonly BindableProperty DelayProperty = BindableProperty.Create(nameof(Delay), typeof(TimeSpan), typeof(LoginPopup), TimeSpan.FromMinutes(1), propertyChanged: OnDelayChanged);
    public static readonly BindableProperty IsDesktopProperty = BindableProperty.Create(nameof(IsDesktop), typeof(bool), typeof(LoginPopup), DeviceInfo.Current.Idiom == DeviceIdiom.Desktop);
    public static readonly BindableProperty IsMobileProperty = BindableProperty.Create(nameof(IsMobile), typeof(bool), typeof(LoginPopup), DeviceInfo.Current.Idiom != DeviceIdiom.Desktop);

    public LoginPopup()
    {
        InitializeComponent();
        Loaded += (_, _) => StartPopupTimer();
        Unloaded += (_, _) => StopPopupTimer();
        DesktopLogin.LoginSucceeded += OnLoginSucceeded;
        MobileLogin.LoginSucceeded += OnLoginSucceeded;
        DesktopLogin.SignUpRequested += OnSignUpRequested;
        MobileLogin.SignUpRequested += OnSignUpRequested;
        DesktopLogin.SocialSignInRequested += OnSocialSignInRequested;
        MobileLogin.SocialSignInRequested += OnSocialSignInRequested;
        ShowCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(() => IsOpen = false);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsAuthenticated { get => (bool)GetValue(IsAuthenticatedProperty); set => SetValue(IsAuthenticatedProperty, value); }
    public bool AuthenticationStateKnown { get => (bool)GetValue(AuthenticationStateKnownProperty); set => SetValue(AuthenticationStateKnownProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public TimeSpan Delay { get => (TimeSpan)GetValue(DelayProperty); set => SetValue(DelayProperty, value); }
    public bool IsDesktop { get => (bool)GetValue(IsDesktopProperty); private set => SetValue(IsDesktopProperty, value); }
    public bool IsMobile { get => (bool)GetValue(IsMobileProperty); private set => SetValue(IsMobileProperty, value); }
    public ICommand ShowCommand { get; }
    public ICommand CloseCommand { get; }
    public event Action<object?, CommerceChatUser>? LoginSucceeded;
    public event EventHandler? SignUpRequested;
    public event Action<object?, string>? SocialSignInRequested;

    private static void OnAuthStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var popup = (LoginPopup)bindable;
        if (popup.IsAuthenticated)
            popup.IsOpen = false;
        popup.StartPopupTimer();
    }

    private static void OnDelayChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LoginPopup)bindable).StartPopupTimer();

    private void StartPopupTimer()
    {
        StopPopupTimer();
        if (!IsLoaded || !AuthenticationStateKnown || IsAuthenticated
            || Preferences.Default.Get(HasPoppedPreferenceKey, false))
            return;

        _popupTimer = Dispatcher.CreateTimer();
        _popupTimer.Interval = Delay > TimeSpan.Zero ? Delay : TimeSpan.FromMinutes(1);
        _popupTimer.Tick += (_, _) =>
        {
            StopPopupTimer();
            IsOpen = true;
            Preferences.Default.Set(HasPoppedPreferenceKey, true);
        };
        _popupTimer.Start();
    }

    private void StopPopupTimer()
    {
        _popupTimer?.Stop();
        _popupTimer = null;
    }

    private void OnLoginSucceeded(object? sender, CommerceChatUser user)
    {
        IsAuthenticated = true;
        IsOpen = false;
        LoginSucceeded?.Invoke(this, user);
    }

    private void OnSignUpRequested(object? sender, EventArgs args) =>
        SignUpRequested?.Invoke(this, EventArgs.Empty);

    private void OnSocialSignInRequested(object? sender, string provider) =>
        SocialSignInRequested?.Invoke(this, provider);
}
