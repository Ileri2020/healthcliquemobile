using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class PostButton : ContentView
{
    private FileResult? _file;
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(PostButton), string.Empty);
    public static readonly BindableProperty AuthorIdProperty = BindableProperty.Create(nameof(AuthorId), typeof(string), typeof(PostButton), string.Empty);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(PostButton), false);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(PostButton), string.Empty);
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(PostButton), string.Empty);
    public static readonly BindableProperty CategoryProperty = BindableProperty.Create(nameof(Category), typeof(string), typeof(PostButton), "General");
    public static readonly BindableProperty TypeProperty = BindableProperty.Create(nameof(Type), typeof(string), typeof(PostButton), "image");
    public static readonly BindableProperty SelectFileTextProperty = BindableProperty.Create(nameof(SelectFileText), typeof(string), typeof(PostButton), "Choose image or video");
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(PostButton), "Publish post");
    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(PostButton), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(PostButton), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(PostButton), string.Empty);

    public PostButton()
    {
        InitializeComponent();
        Categories = ["Pharmaceutical", "Wellness", "Research", "News", "General"];
        Types = ["image", "video", "article"];
        PickCommand = new Command(async () => await PickAsync());
        SubmitCommand = new Command(async () => await SubmitAsync());
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string AuthorId { get => (string)GetValue(AuthorIdProperty); set => SetValue(AuthorIdProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string Category { get => (string)GetValue(CategoryProperty); set => SetValue(CategoryProperty, value); }
    public string Type { get => (string)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }
    public string SelectFileText { get => (string)GetValue(SelectFileTextProperty); private set => SetValue(SelectFileTextProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<string> Types { get; }
    public ICommand PickCommand { get; }
    public ICommand SubmitCommand { get; }
    public event Action<object?, DiscoveryPost>? PostCreated;
    public event Action<object?, Exception>? PublishFailed;

    private async Task PickAsync()
    {
        try
        {
            _file = await FilePicker.Default.PickAsync();
            if (_file is null)
                return;
            var contentType = _file.ContentType ?? string.Empty;
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                Type = "video";
            else if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                Type = "image";
            SelectFileText = _file.FileName;
            CanSubmit = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            if (exception is not OperationCanceledException)
                SetError(exception);
        }
    }

    private async Task SubmitAsync()
    {
        if (!IsAdmin || string.IsNullOrWhiteSpace(AuthorId))
        {
            SetError(new InvalidOperationException("Administrator access and an author ID are required."));
            return;
        }
        if (_file is null || string.IsNullOrWhiteSpace(Title))
        {
            SetError(new InvalidOperationException("Choose a media file and enter a title."));
            return;
        }
        CanSubmit = false;
        SubmitText = "Publishing...";
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=post");
            using var response = await UtilityApi.SendFileAsync(HttpMethod.Post, uri, _file, new Dictionary<string, string>
            {
                ["title"] = Title.Trim(),
                ["description"] = Description.Trim(),
                ["category"] = Category,
                ["type"] = Type,
                ["authorId"] = AuthorId
            });
            var created = await response.Content.ReadFromJsonAsync<DiscoveryPost>(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
                ?? throw new System.Text.Json.JsonException("Post API returned an invalid record.");
            PostCreated?.Invoke(this, created);
            _file = null;
            Title = string.Empty;
            Description = string.Empty;
            SelectFileText = "Choose image or video";
            StatusMessage = "Post published.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or IOException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            CanSubmit = _file is not null && !string.IsNullOrWhiteSpace(Title);
            SubmitText = "Publish post";
        }
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        PublishFailed?.Invoke(this, exception);
    }
}
