using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class ProfileImage : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private FileResult? _file;
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(ProfileImage), string.Empty);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(ProfileImage), string.Empty);
    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(nameof(ImageUrl), typeof(string), typeof(ProfileImage), string.Empty);
    public static readonly BindableProperty SelectedFileNameProperty = BindableProperty.Create(nameof(SelectedFileName), typeof(string), typeof(ProfileImage), "No image selected");
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(ProfileImage), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanUploadProperty = BindableProperty.Create(nameof(CanUpload), typeof(bool), typeof(ProfileImage), false);
    public static readonly BindableProperty UploadTextProperty = BindableProperty.Create(nameof(UploadText), typeof(string), typeof(ProfileImage), "Upload profile image");
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ProfileImage), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ProfileImage), string.Empty);

    public ProfileImage()
    {
        InitializeComponent();
        PickCommand = new Command(async () => await PickAsync(), () => !IsLoading);
        UploadCommand = new Command(async () => await UploadAsync(), () => CanUpload);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string ImageUrl { get => (string)GetValue(ImageUrlProperty); set => SetValue(ImageUrlProperty, value); }
    public string SelectedFileName { get => (string)GetValue(SelectedFileNameProperty); private set => SetValue(SelectedFileNameProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanUpload { get => (bool)GetValue(CanUploadProperty); private set => SetValue(CanUploadProperty, value); }
    public string UploadText { get => (string)GetValue(UploadTextProperty); private set => SetValue(UploadTextProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ICommand PickCommand { get; }
    public ICommand UploadCommand { get; }
    public event Action<object?, string>? ImageUploaded;
    public event Action<object?, Exception>? UploadFailed;

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ProfileImage)bindable;
        control.UploadText = (bool)newValue ? "Uploading..." : "Upload profile image";
        control.CanUpload = !(bool)newValue && control._file is not null;
        (control.PickCommand as Command)?.ChangeCanExecute();
        (control.UploadCommand as Command)?.ChangeCanExecute();
    }

    private async Task PickAsync()
    {
        try
        {
            _file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a profile image",
                FileTypes = FilePickerFileType.Images
            });
            SelectedFileName = _file?.FileName ?? "No image selected";
            CanUpload = _file is not null && !string.IsNullOrWhiteSpace(UserId) && UserId != "nil";
            HasStatus = false;
            (UploadCommand as Command)?.ChangeCanExecute();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            if (exception is not OperationCanceledException)
                SetError(exception);
        }
    }

    private async Task UploadAsync()
    {
        if (_file is null || string.IsNullOrWhiteSpace(UserId) || UserId == "nil")
        {
            SetError(new InvalidOperationException("Choose an image and sign in before uploading."));
            return;
        }
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/file/image");
            using var response = await UtilityApi.SendFileAsync(HttpMethod.Post, uri, _file, new Dictionary<string, string>
            {
                ["description"] = string.Empty,
                ["type"] = "image",
                ["userId"] = UserId,
                ["title"] = "profile image",
                ["profileImage"] = "true",
                ["for"] = "post"
            });
            var result = await response.Content.ReadFromJsonAsync<UploadedImage>(JsonOptions)
                ?? throw new JsonException("Image API returned an invalid response.");
            if (string.IsNullOrWhiteSpace(result.Url))
                throw new JsonException("Image API response did not include a URL.");
            ImageUrl = result.Url;
            _file = null;
            CanUpload = false;
            SelectedFileName = "No image selected";
            StatusMessage = "Profile image uploaded.";
            HasStatus = true;
            ImageUploaded?.Invoke(this, result.Url);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or IOException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        UploadFailed?.Invoke(this, exception);
    }

    private sealed class UploadedImage
    {
        public string Url { get; set; } = string.Empty;
    }
}
