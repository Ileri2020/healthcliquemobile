using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class NotificationUI : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private IDispatcherTimer? _pollTimer;
    private bool _isFetching;
    private int _lastUnreadCount;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(NotificationUI), string.Empty, propertyChanged: OnSessionChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(NotificationUI), string.Empty, propertyChanged: OnSessionChanged);
    public static readonly BindableProperty UserRoleProperty = BindableProperty.Create(nameof(UserRole), typeof(string), typeof(NotificationUI), string.Empty);
    public static readonly BindableProperty UnreadCountProperty = BindableProperty.Create(nameof(UnreadCount), typeof(int), typeof(NotificationUI), 0);
    public static readonly BindableProperty IsToastVisibleProperty = BindableProperty.Create(nameof(IsToastVisible), typeof(bool), typeof(NotificationUI), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(NotificationUI), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(NotificationUI), string.Empty);
    public static readonly BindableProperty ToastMessageProperty = BindableProperty.Create(nameof(ToastMessage), typeof(string), typeof(NotificationUI), string.Empty);
    public static readonly BindableProperty UnreadBadgeTextProperty = BindableProperty.Create(nameof(UnreadBadgeText), typeof(string), typeof(NotificationUI), string.Empty);

    public NotificationUI()
    {
        InitializeComponent();
        Messages = [];
        OpenChatCommand = new Command(() =>
        {
            IsToastVisible = false;
            NavigationRequested?.Invoke(this, "/contact");
        });
        DismissCommand = new Command(() => IsToastVisible = false);
        Loaded += async (_, _) =>
        {
            StartPolling();
            await LoadMessagesAsync();
        };
        Unloaded += (_, _) => StopPolling();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string UserRole { get => (string)GetValue(UserRoleProperty); set => SetValue(UserRoleProperty, value); }
    public int UnreadCount { get => (int)GetValue(UnreadCountProperty); private set => SetValue(UnreadCountProperty, value); }
    public bool IsToastVisible { get => (bool)GetValue(IsToastVisibleProperty); private set => SetValue(IsToastVisibleProperty, value); }
    public bool HasUnread => UnreadCount > 0;
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string ToastMessage { get => (string)GetValue(ToastMessageProperty); private set => SetValue(ToastMessageProperty, value); }
    public string UnreadBadgeText { get => (string)GetValue(UnreadBadgeTextProperty); private set => SetValue(UnreadBadgeTextProperty, value); }
    public ObservableCollection<CommerceMessage> Messages { get; }
    public ICommand OpenChatCommand { get; }
    public ICommand DismissCommand { get; }
    public event Action<object?, string>? NavigationRequested;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnSessionChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((NotificationUI)bindable).LoadMessagesAsync();

    private void StartPolling()
    {
        if (_pollTimer is not null)
            return;
        _pollTimer = Dispatcher.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(30);
        _pollTimer.Tick += async (_, _) => await LoadMessagesAsync();
        _pollTimer.Start();
    }

    private void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    private async Task LoadMessagesAsync()
    {
        if (_isFetching)
            return;
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(UserId) || UserId == "nil")
        {
            SetUnread([]);
            return;
        }

        _isFetching = true;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=message");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var messages = await response.Content.ReadFromJsonAsync<List<CommerceMessage>>(JsonOptions)
                ?? throw new JsonException("Messages API returned an invalid response.");
            var unread = messages.Where(message => message.ReceiverId == UserId && !message.IsRead).ToList();
            SetUnread(unread);
            HasStatus = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            _isFetching = false;
        }
    }

    private void SetUnread(IEnumerable<CommerceMessage> unreadMessages)
    {
        var unread = unreadMessages.ToList();
        Messages.Clear();
        foreach (var message in unread)
            Messages.Add(message);

        UnreadCount = unread.Count;
        UnreadBadgeText = UnreadCount > 9 ? "9+" : UnreadCount.ToString();
        OnPropertyChanged(nameof(HasUnread));
        if (UnreadCount == 0)
            IsToastVisible = false;
        if (UnreadCount > _lastUnreadCount)
        {
            var isAdmin = UserRole is "admin" or "staff" or "professional";
            var sender = isAdmin ? "a patient" : "the pharmacist";
            ToastMessage = $"You have {UnreadCount} unread message{(UnreadCount == 1 ? string.Empty : "s")} from {sender}.";
            IsToastVisible = true;
        }
        _lastUnreadCount = UnreadCount;
    }
}
