using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Post : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _likeId;
    private bool _isLoadingEngagement;
    private int _engagementVersion;

    public static readonly BindableProperty PostDataProperty = BindableProperty.Create(nameof(PostData), typeof(CommunityPost), typeof(Post), propertyChanged: OnPostChanged);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Post), string.Empty, propertyChanged: OnContextChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(Post), string.Empty, propertyChanged: OnContextChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(Post), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsLikedProperty = BindableProperty.Create(nameof(IsLiked), typeof(bool), typeof(Post), false);
    public static readonly BindableProperty LikeCountProperty = BindableProperty.Create(nameof(LikeCount), typeof(int), typeof(Post), 0);
    public static readonly BindableProperty IsCommentsOpenProperty = BindableProperty.Create(nameof(IsCommentsOpen), typeof(bool), typeof(Post), false);
    public static readonly BindableProperty CommentTextProperty = BindableProperty.Create(nameof(CommentText), typeof(string), typeof(Post), string.Empty);
    public static readonly BindableProperty IsSubmittingCommentProperty = BindableProperty.Create(nameof(IsSubmittingComment), typeof(bool), typeof(Post), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Post), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Post), string.Empty);

    public Post()
    {
        InitializeComponent();
        Comments = [];
        LikeCommand = new Command(async () => await ToggleLikeAsync(), () => !_isLoadingEngagement);
        CommentCommand = new Command(async () =>
        {
            IsCommentsOpen = true;
            CommentRequested?.Invoke(this, PostData);
            await LoadCommentsAsync();
        });
        DeleteCommand = new Command(async () => await DeleteAsync(), () => IsAdmin && PostData is not null);
        OpenMediaCommand = new Command(async () => await OpenMediaAsync());
        ShareCommand = new Command(async () => await ShareAsync());
        SubmitCommentCommand = new Command(async () => await SubmitCommentAsync(), () => !IsSubmittingComment);
    }

    public CommunityPost? PostData { get => (CommunityPost?)GetValue(PostDataProperty); set => SetValue(PostDataProperty, value); }
    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLiked { get => (bool)GetValue(IsLikedProperty); private set { SetValue(IsLikedProperty, value); OnPropertyChanged(nameof(LikeButtonText)); } }
    public int LikeCount { get => (int)GetValue(LikeCountProperty); private set { SetValue(LikeCountProperty, value); OnPropertyChanged(nameof(LikeButtonText)); } }
    public bool IsCommentsOpen { get => (bool)GetValue(IsCommentsOpenProperty); set => SetValue(IsCommentsOpenProperty, value); }
    public string CommentText { get => (string)GetValue(CommentTextProperty); set => SetValue(CommentTextProperty, value); }
    public bool IsSubmittingComment { get => (bool)GetValue(IsSubmittingCommentProperty); private set => SetValue(IsSubmittingCommentProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasTitle => !string.IsNullOrWhiteSpace(PostData?.Title);
    public ImageSource? AvatarSource
    {
        get
        {
            var source = PostData?.User?.AvatarUrl;
            if (string.IsNullOrWhiteSpace(source))
                return null;
            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                ? ImageSource.FromUri(uri)
                : ImageSource.FromFile(source);
        }
    }
    public bool IsImage => PostData?.Type.Equals("image", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrWhiteSpace(PostData.ContentUrl);
    public bool IsExternalMedia => PostData?.Type.Equals("video", StringComparison.OrdinalIgnoreCase) == true || PostData?.Type.Equals("audio", StringComparison.OrdinalIgnoreCase) == true;
    public string DateCategory => $"{PostData?.Category ?? "General"} • {PostData?.DateLabel}";
    public string LikeButtonText => $"{(IsLiked ? "Liked" : "Like")}  {LikeCount}";
    public string CommentsSummary => Comments.Count == 0 ? "No comments yet." : $"{Comments.Count} comment(s)";
    public string CommentSubmitText => IsSubmittingComment ? "Posting..." : "Post comment";
    public bool CanSubmitComment => !IsSubmittingComment;
    public ObservableCollection<CommunityComment> Comments { get; }
    public ICommand LikeCommand { get; }
    public ICommand CommentCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenMediaCommand { get; }
    public ICommand ShareCommand { get; }
    public ICommand SubmitCommentCommand { get; }
    public event Action<object?, CommunityPost?>? LikeRequested;
    public event Action<object?, CommunityPost?>? CommentRequested;
    public event Action<object?, CommunityPost?>? PostDeleted;
    public event Action<object?, Exception>? ActionFailed;

    private static void OnPostChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Post)bindable;
        control._engagementVersion++;
        control.IsLiked = false;
        control.LikeCount = 0;
        control._likeId = null;
        control.Comments.Clear();
        control.NotifyPostProperties();
        (control.DeleteCommand as Command)?.ChangeCanExecute();
        _ = control.LoadEngagementAsync();
    }

    private static void OnContextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Post)bindable;
        control._engagementVersion++;
        _ = control.LoadEngagementAsync();
    }

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        (((Post)bindable).DeleteCommand as Command)?.ChangeCanExecute();

    private void NotifyPostProperties()
    {
        OnPropertyChanged(nameof(HasTitle));
        OnPropertyChanged(nameof(AvatarSource));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsExternalMedia));
        OnPropertyChanged(nameof(DateCategory));
        OnPropertyChanged(nameof(CommentsSummary));
    }

    private async Task LoadEngagementAsync()
    {
        if (_isLoadingEngagement || PostData is null || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        var version = _engagementVersion;
        _isLoadingEngagement = true;
        HasStatus = false;
        (LikeCommand as Command)?.ChangeCanExecute();
        try
        {
            var id = Uri.EscapeDataString(PostData.Id);
            var likesTask = GetAsync<List<CommunityLike>>($"api/dbhandler?model=likes&id={id}");
            var commentsTask = GetAsync<List<CommunityComment>>($"api/dbhandler?model=comments&id={id}");
            await Task.WhenAll(likesTask, commentsTask);
            if (version != _engagementVersion)
                return;
            var likes = await likesTask;
            var comments = await commentsTask;
            LikeCount = likes.Count;
            var myLike = likes.FirstOrDefault(like => like.UserId == UserId && !string.IsNullOrWhiteSpace(UserId));
            _likeId = myLike?.Id;
            IsLiked = myLike is not null;
            SetComments(comments);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            _isLoadingEngagement = false;
            (LikeCommand as Command)?.ChangeCanExecute();
            if (version != _engagementVersion)
                _ = LoadEngagementAsync();
        }
    }

    private async Task LoadCommentsAsync()
    {
        if (PostData is null || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        try
        {
            var comments = await GetAsync<List<CommunityComment>>(
                $"api/dbhandler?model=comments&id={Uri.EscapeDataString(PostData.Id)}");
            SetComments(comments);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task<List<T>> GetAsync<T>(string path)
    {
        using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
        return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions)
            ?? throw new JsonException("The community endpoint returned an empty response.");
    }

    private void SetComments(IEnumerable<CommunityComment> comments)
    {
        Comments.Clear();
        foreach (var comment in comments.OrderBy(item => item.CreatedAt))
            Comments.Add(comment);
        OnPropertyChanged(nameof(CommentsSummary));
    }

    private async Task ToggleLikeAsync()
    {
        if (PostData is null)
            return;
        if (string.IsNullOrWhiteSpace(UserId))
        {
            SetError(new InvalidOperationException("Sign in to like a post."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl before liking a post."));
            return;
        }
        _isLoadingEngagement = true;
        (LikeCommand as Command)?.ChangeCanExecute();
        HasStatus = false;
        try
        {
            if (IsLiked && !string.IsNullOrWhiteSpace(_likeId))
            {
                using var response = await UtilityApi.SendAsync(HttpMethod.Delete,
                    UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=likes&id={Uri.EscapeDataString(_likeId)}"), (object?)null);
                IsLiked = false;
                LikeCount = Math.Max(0, LikeCount - 1);
                _likeId = null;
            }
            else
            {
                using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                    UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=likes"),
                    new { userId = UserId, contentId = PostData.Id });
                var like = await response.Content.ReadFromJsonAsync<CommunityLike>(JsonOptions)
                    ?? throw new JsonException("The like endpoint returned an empty response.");
                _likeId = like.Id;
                IsLiked = true;
                LikeCount++;
            }
            LikeRequested?.Invoke(this, PostData);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            _isLoadingEngagement = false;
            (LikeCommand as Command)?.ChangeCanExecute();
        }
    }

    private async Task SubmitCommentAsync()
    {
        if (PostData is null || string.IsNullOrWhiteSpace(CommentText))
        {
            SetError(new ArgumentException("Enter a comment before posting."));
            return;
        }
        if (string.IsNullOrWhiteSpace(UserId))
        {
            SetError(new InvalidOperationException("Sign in to join the discussion."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl before posting a comment."));
            return;
        }
        IsSubmittingComment = true;
        UpdateCommentSubmitState();
        HasStatus = false;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=comments"),
                new { userId = UserId, contentId = PostData.Id, comment = CommentText.Trim() });
            CommentText = string.Empty;
            await LoadCommentsAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsSubmittingComment = false;
            UpdateCommentSubmitState();
        }
    }

    private void UpdateCommentSubmitState()
    {
        OnPropertyChanged(nameof(CommentSubmitText));
        OnPropertyChanged(nameof(CanSubmitComment));
        (SubmitCommentCommand as Command)?.ChangeCanExecute();
    }

    private async Task DeleteAsync()
    {
        if (!IsAdmin || PostData is null || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete,
                UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=post&id={Uri.EscapeDataString(PostData.Id)}"), (object?)null);
            PostDeleted?.Invoke(this, PostData);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task OpenMediaAsync()
    {
        if (string.IsNullOrWhiteSpace(PostData?.ContentUrl))
            return;
        try
        {
            await Launcher.Default.OpenAsync(new Uri(PostData.ContentUrl));
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or UriFormatException or InvalidOperationException)
        {
            SetError(exception);
        }
    }

    private async Task ShareAsync()
    {
        if (PostData is null)
            return;
        try
        {
            var origin = string.IsNullOrWhiteSpace(ApiBaseUrl) ? string.Empty : new Uri(ApiBaseUrl).GetLeftPart(UriPartial.Authority);
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = PostData.Title ?? "HealthClique clinical post",
                Text = $"{origin}/blog#{PostData.Id}",
            });
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or InvalidOperationException or UriFormatException)
        {
            SetError(exception);
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        ActionFailed?.Invoke(this, exception);
    }
}
