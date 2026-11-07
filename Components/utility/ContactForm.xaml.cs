using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class ContactForm : ContentView
{
    private static readonly HttpClient HttpClient = new();

    public static readonly BindableProperty EndpointUrlProperty =
        BindableProperty.Create(nameof(EndpointUrl), typeof(string), typeof(ContactForm), string.Empty);
    public static readonly BindableProperty EmailToProperty =
        BindableProperty.Create(nameof(EmailTo), typeof(string), typeof(ContactForm), "adepojuololade2020@gmail.com");
    public static readonly BindableProperty UsernameProperty =
        BindableProperty.Create(nameof(Username), typeof(string), typeof(ContactForm), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty EmailProperty =
        BindableProperty.Create(nameof(Email), typeof(string), typeof(ContactForm), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create(nameof(Category), typeof(string), typeof(ContactForm), "suggestion", BindingMode.TwoWay);
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(ContactForm), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty IsSubmittingProperty =
        BindableProperty.Create(nameof(IsSubmitting), typeof(bool), typeof(ContactForm), false, propertyChanged: OnStateChanged);
    public static readonly BindableProperty StatusMessageProperty =
        BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ContactForm), string.Empty, propertyChanged: OnStateChanged);
    public static readonly BindableProperty HasStatusProperty =
        BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ContactForm), false);
    public static readonly BindableProperty CanSubmitProperty =
        BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(ContactForm), true);
    public static readonly BindableProperty SubmitTextProperty =
        BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(ContactForm), "Submit");
    public static readonly BindableProperty StatusColorProperty =
        BindableProperty.Create(nameof(StatusColor), typeof(Color), typeof(ContactForm), null);

    public ContactForm()
    {
        InitializeComponent();
        Categories = ["suggestion", "complaint", "advert", "appreciate"];
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
        UpdateState();
    }

    public string EndpointUrl { get => (string)GetValue(EndpointUrlProperty); set => SetValue(EndpointUrlProperty, value); }
    public string EmailTo { get => (string)GetValue(EmailToProperty); set => SetValue(EmailToProperty, value); }
    public string Username { get => (string)GetValue(UsernameProperty); set => SetValue(UsernameProperty, value); }
    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string Category { get => (string)GetValue(CategoryProperty); set => SetValue(CategoryProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public bool IsSubmitting { get => (bool)GetValue(IsSubmittingProperty); private set => SetValue(IsSubmittingProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public Color? StatusColor { get => (Color?)GetValue(StatusColorProperty); private set => SetValue(StatusColorProperty, value); }
    public ObservableCollection<string> Categories { get; }
    public ICommand SubmitCommand { get; }

    public event Action<object?, ContactMessage>? MessageSent;
    public event Action<object?, Exception>? SubmissionFailed;

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ContactForm)bindable).UpdateState();

    private async Task SubmitAsync()
    {
        if (IsSubmitting)
            return;

        if (string.IsNullOrWhiteSpace(EndpointUrl)
            || !Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            SetStatus("Configure an absolute HTTP(S) EndpointUrl before submitting.", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(Username)
            || string.IsNullOrWhiteSpace(Email)
            || !Email.Contains('@')
            || string.IsNullOrWhiteSpace(Message))
        {
            SetStatus("Enter your username, a valid email address, and a message.", false);
            return;
        }

        var submission = new ContactMessage
        {
            Username = Username.Trim(),
            Email = Email.Trim(),
            Category = Category,
            Message = Message.Trim(),
            EmailTo = EmailTo
        };

        IsSubmitting = true;
        try
        {
            using var response = await HttpClient.PostAsJsonAsync(endpoint, submission);
            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var detail = string.IsNullOrWhiteSpace(responseText) ? response.ReasonPhrase : responseText;
                throw new HttpRequestException($"Contact message request failed ({(int)response.StatusCode}): {detail}");
            }

            Username = string.Empty;
            Email = string.Empty;
            Category = Categories[0];
            Message = string.Empty;
            SetStatus("Message sent successfully.", true);
            MessageSent?.Invoke(this, submission);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            SetStatus($"Message could not be sent: {exception.Message}", false);
            SubmissionFailed?.Invoke(this, exception);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void SetStatus(string message, bool success)
    {
        StatusMessage = message;
        var resources = Application.Current?.Resources;
        if (resources is not null)
        {
            var key = success
                ? Application.Current!.RequestedTheme == AppTheme.Dark ? "ThemeAccentDark" : "ThemeAccentLight"
                : Application.Current!.RequestedTheme == AppTheme.Dark ? "ThemeDestructiveDark" : "ThemeDestructiveLight";
            if (resources.TryGetValue(key, out var color) && color is Color statusColor)
                StatusColor = statusColor;
        }
        HasStatus = true;
    }

    private void UpdateState()
    {
        CanSubmit = !IsSubmitting;
        SubmitText = IsSubmitting ? "Submitting..." : "Submit";
        HasStatus = !string.IsNullOrWhiteSpace(StatusMessage);
        (SubmitCommand as Command)?.ChangeCanExecute();
    }
}
