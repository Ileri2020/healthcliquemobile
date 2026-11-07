using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class Posts : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HashSet<string> _enabledTypes = new(StringComparer.OrdinalIgnoreCase) { "video", "audio", "image" };
    private string _sortOrder = "desc";

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Posts), string.Empty, propertyChanged: OnApiChanged);
    public static readonly BindableProperty CategoryProperty = BindableProperty.Create(nameof(Category), typeof(string), typeof(Posts), "All", propertyChanged: OnFilterChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(Posts), string.Empty);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(Posts), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Posts), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Posts), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Posts), string.Empty);

    public Posts()
    {
        InitializeComponent();
        AllPosts = [];
        VisiblePosts = [];
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        ToggleSortCommand = new Command(ToggleSort);
        ToggleTypeCommand = new Command<string>(ToggleType);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string Category { get => (string)GetValue(CategoryProperty); set => SetValue(CategoryProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string SortText => _sortOrder == "desc" ? "Newest first" : "Oldest first";
    public ButtonVariant VideoVariant => _enabledTypes.Contains("video") ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public ButtonVariant AudioVariant => _enabledTypes.Contains("audio") ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public ButtonVariant ImageVariant => _enabledTypes.Contains("image") ? ButtonVariant.Secondary : ButtonVariant.Outline;
    public bool IsEmpty => !IsLoading && VisiblePosts.Count == 0;
    public string EmptyMessage => $"No {Category} Records Found. Be the first to share clinical insights.";
    public ObservableCollection<CommunityPost> AllPosts { get; }
    public ObservableCollection<CommunityPost> VisiblePosts { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleSortCommand { get; }
    public ICommand ToggleTypeCommand { get; }
    public event Action<object?, CommunityPost?>? LikeRequested;
    public event Action<object?, CommunityPost?>? CommentRequested;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnApiChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((Posts)bindable).LoadAsync();

    private static void OnFilterChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Posts)bindable).ApplyFilter();

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Get,
                UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=post&limit=100"), (object?)null);
            var posts = await response.Content.ReadFromJsonAsync<List<CommunityPost>>(JsonOptions)
                ?? throw new JsonException("The posts endpoint returned an empty response.");
            AllPosts.Clear();
            foreach (var post in posts)
                AllPosts.Add(post);
            ApplyFilter();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            HasStatus = true;
            StatusMessage = exception.Message;
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
            (RefreshCommand as Command)?.ChangeCanExecute();
        }
    }

    private void ApplyFilter()
    {
        var selectedCategory = string.IsNullOrWhiteSpace(Category) ? "All" : Category.Trim();
        var filtered = AllPosts.Where(post =>
            _enabledTypes.Contains(post.Type.ToLowerInvariant()) &&
            (selectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase) ||
             post.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)));
        filtered = _sortOrder == "desc"
            ? filtered.OrderByDescending(post => post.CreatedAt)
            : filtered.OrderBy(post => post.CreatedAt);
        VisiblePosts.Clear();
        foreach (var post in filtered)
            VisiblePosts.Add(post);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(VideoVariant));
        OnPropertyChanged(nameof(AudioVariant));
        OnPropertyChanged(nameof(ImageVariant));
    }

    private void ToggleSort()
    {
        _sortOrder = _sortOrder == "desc" ? "asc" : "desc";
        OnPropertyChanged(nameof(SortText));
        ApplyFilter();
    }

    private void ToggleType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return;
        if (!_enabledTypes.Add(type))
            _enabledTypes.Remove(type);
        ApplyFilter();
    }

    private void OnLikeRequested(object? sender, CommunityPost? post) =>
        LikeRequested?.Invoke(this, post);

    private void OnCommentRequested(object? sender, CommunityPost? post) =>
        CommentRequested?.Invoke(this, post);

    private void OnPostDeleted(object? sender, CommunityPost? post)
    {
        if (post is null)
            return;
        AllPosts.Remove(post);
        ApplyFilter();
    }

    private void OnPostFailed(object? sender, Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }
}
