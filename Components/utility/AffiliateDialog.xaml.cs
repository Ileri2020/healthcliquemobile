using System.Net.Http;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class AffiliateDialog : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(AffiliateDialog), string.Empty);
    public static readonly BindableProperty EndpointUrlProperty = BindableProperty.Create(nameof(EndpointUrl), typeof(string), typeof(AffiliateDialog), "api/affiliate");
    public static readonly BindableProperty TriggerTextProperty = BindableProperty.Create(nameof(TriggerText), typeof(string), typeof(AffiliateDialog), "Become an Affiliate");
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(AffiliateDialog), "Join Our Affiliate Program");
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(AffiliateDialog), "Share Health Clique with your community and earn rewards.");
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(AffiliateDialog), false);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(AffiliateDialog), string.Empty);
    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(AffiliateDialog), string.Empty);
    public static readonly BindableProperty PhoneProperty = BindableProperty.Create(nameof(Phone), typeof(string), typeof(AffiliateDialog), string.Empty);
    public static readonly BindableProperty AcceptedTermsProperty = BindableProperty.Create(nameof(AcceptedTerms), typeof(bool), typeof(AffiliateDialog), false);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(AffiliateDialog), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(nameof(CanSubmit), typeof(bool), typeof(AffiliateDialog), true);
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(AffiliateDialog), "Submit Application");
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(AffiliateDialog), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(AffiliateDialog), string.Empty);

    public AffiliateDialog()
    {
        InitializeComponent();
        OpenCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(() => IsOpen = false);
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string EndpointUrl { get => (string)GetValue(EndpointUrlProperty); set => SetValue(EndpointUrlProperty, value); }
    public string TriggerText { get => (string)GetValue(TriggerTextProperty); set => SetValue(TriggerTextProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string Phone { get => (string)GetValue(PhoneProperty); set => SetValue(PhoneProperty, value); }
    public bool AcceptedTerms { get => (bool)GetValue(AcceptedTermsProperty); set => SetValue(AcceptedTermsProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSubmit { get => (bool)GetValue(CanSubmitProperty); private set => SetValue(CanSubmitProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SubmitCommand { get; }
    public event Action<object?, EventArgs>? ApplicationSubmitted;
    public event Action<object?, Exception>? SubmissionFailed;

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AffiliateDialog)bindable;
        control.CanSubmit = !(bool)newValue;
        control.SubmitText = (bool)newValue ? "Submitting..." : "Submit Application";
        (control.SubmitCommand as Command)?.ChangeCanExecute();
    }

    private async Task SubmitAsync()
    {
        if (IsLoading)
            return;
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            SetError("Enter your name and a valid email address.");
            return;
        }
        if (!AcceptedTerms)
        {
            SetError("You must accept the terms before submitting.");
            return;
        }

        IsLoading = true;
        HasStatus = false;
        try
        {
            var payload = new { name = Name.Trim() };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, EndpointUrl), payload);
            ApplicationSubmitted?.Invoke(this, EventArgs.Empty);
            StatusMessage = "Your application was submitted.";
            HasStatus = true;
            IsOpen = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception.Message);
            SubmissionFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
