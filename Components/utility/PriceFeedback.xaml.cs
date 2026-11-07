using System.Net.Http;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class PriceFeedback : ContentView
{
    private static readonly string[] Options = ["Too Expensive", "Slightly Expensive", "Fair Pricing", "Too Cheap"];
    private static readonly string[] InlineOptions = ["High Price", "Low Price"];

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(PriceFeedback), string.Empty);
    public static readonly BindableProperty ProductIdProperty = BindableProperty.Create(nameof(ProductId), typeof(string), typeof(PriceFeedback), string.Empty);
    public static readonly BindableProperty ProductNameProperty = BindableProperty.Create(nameof(ProductName), typeof(string), typeof(PriceFeedback), string.Empty, propertyChanged: OnProductNameChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(PriceFeedback), string.Empty);
    public static readonly BindableProperty IsInlineProperty = BindableProperty.Create(nameof(IsInline), typeof(bool), typeof(PriceFeedback), false, propertyChanged: OnInlineChanged);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(PriceFeedback), false);
    public static readonly BindableProperty IsSubmittingProperty = BindableProperty.Create(nameof(IsSubmitting), typeof(bool), typeof(PriceFeedback), false);
    public static readonly BindableProperty HasSuccessProperty = BindableProperty.Create(nameof(HasSuccess), typeof(bool), typeof(PriceFeedback), false);
    public static readonly BindableProperty FeedbackProperty = BindableProperty.Create(nameof(Feedback), typeof(string), typeof(PriceFeedback), string.Empty);
    public static readonly BindableProperty CommentProperty = BindableProperty.Create(nameof(Comment), typeof(string), typeof(PriceFeedback), string.Empty);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(PriceFeedback), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(PriceFeedback), string.Empty);

    public PriceFeedback()
    {
        InitializeComponent();
        OpenCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(() => IsOpen = false);
        ChooseCommand = new Command<string>(value => Feedback = value ?? string.Empty);
        SubmitCommand = new Command(async () => await SubmitAsync(), () => !IsSubmitting);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string ProductId { get => (string)GetValue(ProductIdProperty); set => SetValue(ProductIdProperty, value); }
    public string ProductName { get => (string)GetValue(ProductNameProperty); set => SetValue(ProductNameProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public bool IsInline { get => (bool)GetValue(IsInlineProperty); set => SetValue(IsInlineProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsSubmitting { get => (bool)GetValue(IsSubmittingProperty); private set => SetValue(IsSubmittingProperty, value); }
    public bool HasSuccess { get => (bool)GetValue(HasSuccessProperty); private set => SetValue(HasSuccessProperty, value); }
    public bool HasError => HasStatus && !HasSuccess;
    public string Feedback { get => (string)GetValue(FeedbackProperty); set => SetValue(FeedbackProperty, value); }
    public string Comment { get => (string)GetValue(CommentProperty); set => SetValue(CommentProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string DialogTitle => $"Price feedback for {ProductName}";
    public string SubmitText => IsSubmitting ? "Submitting..." : "Submit feedback";
    public bool CanSubmit => !IsSubmitting;
    public bool ShowDialogTrigger => !IsInline;
    public IReadOnlyList<string> FeedbackOptions => IsInline ? InlineOptions : Options;
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ChooseCommand { get; }
    public ICommand SubmitCommand { get; }
    public event Action<object?, Exception>? RequestFailed;
    public event EventHandler? FeedbackSubmitted;

    private static void OnProductNameChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((PriceFeedback)bindable).OnPropertyChanged(nameof(DialogTitle));

    private static void OnInlineChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (PriceFeedback)bindable;
        control.OnPropertyChanged(nameof(FeedbackOptions));
        control.OnPropertyChanged(nameof(ShowDialogTrigger));
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Feedback))
        {
            SetError(new ArgumentException("Select a feedback option before submitting."));
            return;
        }
        if (string.IsNullOrWhiteSpace(UserId))
        {
            SetError(new InvalidOperationException("Sign in before submitting price feedback."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ProductId) || string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("A product and API base URL are required."));
            return;
        }

        IsSubmitting = true;
        HasStatus = false;
        HasSuccess = false;
        UpdateSubmitState();
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=priceFeedback"),
                new { userId = UserId, productId = ProductId, feedback = Feedback, comment = Comment });
            Feedback = string.Empty;
            Comment = string.Empty;
            IsOpen = false;
            StatusMessage = "Feedback submitted. Thank you!";
            HasStatus = true;
            HasSuccess = true;
            OnPropertyChanged(nameof(HasError));
            FeedbackSubmitted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsSubmitting = false;
            UpdateSubmitState();
        }
    }

    private void UpdateSubmitState()
    {
        OnPropertyChanged(nameof(SubmitText));
        OnPropertyChanged(nameof(CanSubmit));
        (SubmitCommand as Command)?.ChangeCanExecute();
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        HasSuccess = false;
        StatusMessage = exception.Message;
        OnPropertyChanged(nameof(HasError));
        RequestFailed?.Invoke(this, exception);
    }
}

public sealed class InlinePriceFeedback : PriceFeedback
{
    public InlinePriceFeedback() => IsInline = true;
}
