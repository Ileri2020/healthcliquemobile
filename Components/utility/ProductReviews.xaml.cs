using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using EEE.Components.UI;

namespace EEE.Components.Utility;

public partial class ProductReviews : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(ProductReviews), string.Empty, propertyChanged: OnProductChanged);
    public static readonly BindableProperty ProductIdProperty = BindableProperty.Create(nameof(ProductId), typeof(string), typeof(ProductReviews), string.Empty, propertyChanged: OnProductChanged);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(ProductReviews), string.Empty);
    public static readonly BindableProperty RatingProperty = BindableProperty.Create(nameof(Rating), typeof(int), typeof(ProductReviews), 0);
    public static readonly BindableProperty CommentProperty = BindableProperty.Create(nameof(Comment), typeof(string), typeof(ProductReviews), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(ProductReviews), false);
    public static readonly BindableProperty IsSubmittingProperty = BindableProperty.Create(nameof(IsSubmitting), typeof(bool), typeof(ProductReviews), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(ProductReviews), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ProductReviews), string.Empty);

    public ProductReviews()
    {
        InitializeComponent();
        Reviews = [];
        SetRatingCommand = new Command<string>(SetRating);
        SubmitCommand = new Command(async () => await SubmitAsync(), () => !IsSubmitting);
        RefreshCommand = new Command(async () => await LoadAsync());
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string ProductId { get => (string)GetValue(ProductIdProperty); set => SetValue(ProductIdProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public int Rating { get => (int)GetValue(RatingProperty); set { SetValue(RatingProperty, value); UpdateStars(); } }
    public string Comment { get => (string)GetValue(CommentProperty); set => SetValue(CommentProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool IsSubmitting { get => (bool)GetValue(IsSubmittingProperty); private set => SetValue(IsSubmittingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool IsEmpty => !IsLoading && Reviews.Count == 0;
    public string ReviewsTitle => $"Customer Feedback ({Reviews.Count})";
    public string SubmitText => IsSubmitting ? "Posting..." : "Post review";
    public bool CanSubmit => !IsSubmitting;
    public ButtonVariant Star1Variant => Rating >= 1 ? ButtonVariant.Primary : ButtonVariant.Outline;
    public ButtonVariant Star2Variant => Rating >= 2 ? ButtonVariant.Primary : ButtonVariant.Outline;
    public ButtonVariant Star3Variant => Rating >= 3 ? ButtonVariant.Primary : ButtonVariant.Outline;
    public ButtonVariant Star4Variant => Rating >= 4 ? ButtonVariant.Primary : ButtonVariant.Outline;
    public ButtonVariant Star5Variant => Rating >= 5 ? ButtonVariant.Primary : ButtonVariant.Outline;
    public ObservableCollection<CommunityReview> Reviews { get; }
    public ICommand SetRatingCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand RefreshCommand { get; }
    public event Action<object?, Exception>? RequestFailed;

    private static void OnProductChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((ProductReviews)bindable).LoadAsync();

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(ProductId))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var path = $"api/dbhandler?model=product&id={Uri.EscapeDataString(ProductId)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
            var product = await response.Content.ReadFromJsonAsync<CommunityReviewProduct>(JsonOptions)
                ?? throw new JsonException("The product endpoint returned an empty response.");
            Reviews.Clear();
            foreach (var review in product.Reviews.OrderByDescending(item => item.CreatedAt))
                Reviews.Add(review);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ReviewsTitle));
        }
    }

    private async Task SubmitAsync()
    {
        if (Rating is < 1 or > 5)
        {
            SetError(new ArgumentException("Choose a star rating before posting your review."));
            return;
        }
        if (string.IsNullOrWhiteSpace(UserId))
        {
            SetError(new InvalidOperationException("Sign in before leaving a review."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(ProductId))
        {
            SetError(new InvalidOperationException("A product and API base URL are required."));
            return;
        }
        IsSubmitting = true;
        HasStatus = false;
        UpdateSubmitState();
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=review"),
                new { userId = UserId, productId = ProductId, rating = Rating, comment = Comment });
            Rating = 0;
            Comment = string.Empty;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsSubmitting = false;
            UpdateSubmitState();
        }
    }

    private void SetRating(string? value)
    {
        if (int.TryParse(value, out var rating) && rating is >= 1 and <= 5)
            Rating = rating;
    }

    private void UpdateStars()
    {
        OnPropertyChanged(nameof(Star1Variant));
        OnPropertyChanged(nameof(Star2Variant));
        OnPropertyChanged(nameof(Star3Variant));
        OnPropertyChanged(nameof(Star4Variant));
        OnPropertyChanged(nameof(Star5Variant));
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
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }
}
