using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class ChatInterface : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private IDispatcherTimer? _pollTimer;
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(ChatInterface), string.Empty);
    public static readonly BindableProperty CurrentUserIdProperty = BindableProperty.Create(nameof(CurrentUserId), typeof(string), typeof(ChatInterface), string.Empty, propertyChanged: OnIdentityChanged);
    public static readonly BindableProperty CurrentUserNameProperty = BindableProperty.Create(nameof(CurrentUserName), typeof(string), typeof(ChatInterface), string.Empty);
    public static readonly BindableProperty CurrentUserRoleProperty = BindableProperty.Create(nameof(CurrentUserRole), typeof(string), typeof(ChatInterface), "customer", propertyChanged: OnIdentityChanged);
    public static readonly BindableProperty IsStaffProperty = BindableProperty.Create(nameof(IsStaff), typeof(bool), typeof(ChatInterface), false);
    public static readonly BindableProperty ChatColumnProperty = BindableProperty.Create(nameof(ChatColumn), typeof(int), typeof(ChatInterface), 0);
    public static readonly BindableProperty ChatColumnSpanProperty = BindableProperty.Create(nameof(ChatColumnSpan), typeof(int), typeof(ChatInterface), 2);
    public static readonly BindableProperty SelectedUserProperty = BindableProperty.Create(nameof(SelectedUser), typeof(CommerceChatUser), typeof(ChatInterface), null, propertyChanged: OnSelectedUserChanged);
    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(nameof(SearchText), typeof(string), typeof(ChatInterface), string.Empty, propertyChanged: OnSearchChanged);
    public static readonly BindableProperty NewMessageProperty = BindableProperty.Create(nameof(NewMessage), typeof(string), typeof(ChatInterface), string.Empty, propertyChanged: OnMessageChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(ChatInterface), false);
    public static readonly BindableProperty CanSendProperty = BindableProperty.Create(nameof(CanSend), typeof(bool), typeof(ChatInterface), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ChatInterface), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ChatInterface), string.Empty);

    public ChatInterface()
    {
        InitializeComponent();
        Users = [];
        FilteredUsers = [];
        Messages = [];
        SelectUserCommand = new Command<CommerceChatUser>(SelectUser);
        SendCommand = new Command(async () => await SendAsync(), () => CanSend && !IsLoading);
        Loaded += async (_, _) =>
        {
            await LoadUsersAsync();
            await LoadMessagesAsync();
            StartPolling();
        };
        Unloaded += (_, _) => _pollTimer?.Stop();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string CurrentUserId { get => (string)GetValue(CurrentUserIdProperty); set => SetValue(CurrentUserIdProperty, value); }
    public string CurrentUserName { get => (string)GetValue(CurrentUserNameProperty); set => SetValue(CurrentUserNameProperty, value); }
    public string CurrentUserRole { get => (string)GetValue(CurrentUserRoleProperty); set => SetValue(CurrentUserRoleProperty, value); }
    public bool IsStaff { get => (bool)GetValue(IsStaffProperty); private set => SetValue(IsStaffProperty, value); }
    public int ChatColumn { get => (int)GetValue(ChatColumnProperty); private set => SetValue(ChatColumnProperty, value); }
    public int ChatColumnSpan { get => (int)GetValue(ChatColumnSpanProperty); private set => SetValue(ChatColumnSpanProperty, value); }
    public CommerceChatUser? SelectedUser { get => (CommerceChatUser?)GetValue(SelectedUserProperty); private set => SetValue(SelectedUserProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public string NewMessage { get => (string)GetValue(NewMessageProperty); set => SetValue(NewMessageProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSend { get => (bool)GetValue(CanSendProperty); private set => SetValue(CanSendProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<CommerceChatUser> Users { get; }
    public ObservableCollection<CommerceChatUser> FilteredUsers { get; }
    public ObservableCollection<ChatMessageView> Messages { get; }
    public ICommand SelectUserCommand { get; }
    public ICommand SendCommand { get; }
    public event Action<object?, Exception>? RequestFailed;

    private static void OnIdentityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ChatInterface)bindable;
        control.IsStaff = control.CurrentUserRole is "admin" or "staff" or "professional";
        control.ChatColumn = control.IsStaff ? 1 : 0;
        control.ChatColumnSpan = control.IsStaff ? 1 : 2;
        control.CanSend = !string.IsNullOrWhiteSpace(control.CurrentUserId) && control.CurrentUserId != "nil";
        control.ApplyUserFilter();
        (control.SendCommand as Command)?.ChangeCanExecute();
    }

    private static void OnSelectedUserChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((ChatInterface)bindable).LoadMessagesAsync();

    private static void OnSearchChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ChatInterface)bindable).ApplyUserFilter();

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ChatInterface)bindable;
        control.CanSend = !string.IsNullOrWhiteSpace(control.CurrentUserId)
            && control.CurrentUserId != "nil"
            && control.SelectedUser is not null
            && !string.IsNullOrWhiteSpace((string)newValue);
        (control.SendCommand as Command)?.ChangeCanExecute();
    }

    private async Task LoadUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(CurrentUserId) || CurrentUserId == "nil")
            return;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=user&limit=100");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var users = await response.Content.ReadFromJsonAsync<List<CommerceChatUser>>(JsonOptions);
            if (users is null)
                throw new JsonException("User API returned an invalid response.");
            Users.Clear();
            foreach (var user in users.Where(user => user.Id != CurrentUserId))
            {
                if (!IsStaff && user.Role is not ("admin" or "staff"))
                    continue;
                Users.Add(user);
            }
            ApplyUserFilter();
            if (!IsStaff)
                SelectedUser = Users.FirstOrDefault();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
    }

    private async Task LoadMessagesAsync()
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(CurrentUserId) || CurrentUserId == "nil")
            return;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=message&limit=500");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var messages = await response.Content.ReadFromJsonAsync<List<CommerceMessage>>(JsonOptions)
                ?? throw new JsonException("Message API returned an invalid response.");
            var conversation = messages
                .Where(message => IsStaff
                    ? (message.SenderId == SelectedUser.Id && message.ReceiverId == CurrentUserId)
                        || (message.SenderId == CurrentUserId && message.ReceiverId == SelectedUser.Id)
                    : (message.SenderId == CurrentUserId && message.ReceiverId == SelectedUser.Id)
                        || (message.SenderId == SelectedUser.Id && message.ReceiverId == CurrentUserId))
                .OrderBy(message => message.CreatedAt)
                .ToArray();
            Messages.Clear();
            foreach (var message in conversation)
                Messages.Add(new ChatMessageView(message, message.SenderId == CurrentUserId));
            await MarkIncomingMessagesReadAsync(conversation);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
    }

    private async Task MarkIncomingMessagesReadAsync(IEnumerable<CommerceMessage> messages)
    {
        foreach (var message in messages.Where(message => message.ReceiverId == CurrentUserId && !message.IsRead))
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=message&id={Uri.EscapeDataString(message.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, uri, new { id = message.Id, isRead = true });
        }
    }

    private async Task SendAsync()
    {
        if (SelectedUser is null || !CanSend || IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        var content = NewMessage.Trim();
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=message");
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new
            {
                content,
                senderId = CurrentUserId,
                receiverId = SelectedUser.Id
            });
            NewMessage = string.Empty;
            await LoadMessagesAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception.Message);
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectUser(CommerceChatUser? user)
    {
        if (user is not null)
            SelectedUser = user;
    }

    private void ApplyUserFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        FilteredUsers.Clear();
        foreach (var user in Users.Where(user => string.IsNullOrEmpty(query)
                     || user.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || user.Email.Contains(query, StringComparison.OrdinalIgnoreCase)))
            FilteredUsers.Add(user);
    }

    private void StartPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = Dispatcher.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(30);
        _pollTimer.Tick += async (_, _) =>
        {
            if (IsStaff)
                await LoadUsersAsync();
            await LoadMessagesAsync();
        };
        _pollTimer.Start();
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    public sealed class ChatMessageView
    {
        public ChatMessageView(CommerceMessage message, bool isOwn)
        {
            Message = message;
            Content = message.Content;
            TimeText = message.CreatedAt.ToLocalTime().ToString("MMM d, HH:mm");
            MessageAlignment = isOwn ? LayoutOptions.End : LayoutOptions.Start;
            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            MessageBackground = (Color)Application.Current!.Resources[isOwn
                ? dark ? "ThemePrimaryDark" : "ThemePrimaryLight"
                : dark ? "ThemeSurfaceDark" : "ThemeSurfaceLight"];
        }
        public CommerceMessage Message { get; }
        public string Content { get; }
        public string TimeText { get; }
        public LayoutOptions MessageAlignment { get; }
        public Color MessageBackground { get; }
    }
}
