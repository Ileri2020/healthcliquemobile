using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class BlogPage : ContentPage
{
    private static readonly string[] Categories = ["General", "Pharmaceutical", "Wellness", "Research", "News"];

    public BlogPage()
    {
        InitializeComponent();
        foreach (var category in Categories)
        {
            var button = new Button { Text = category, Padding = new Thickness(12, 8) };
            button.Clicked += (_, _) => PostList.Category = category;
            CategoryButtons.Children.Add(button);
        }

        PostPublisher.ApiBaseUrl = PageLayout.ApiBaseUrl;
        PostList.ApiBaseUrl = PageLayout.ApiBaseUrl;
        PostPublisher.PostCreated += (_, _) => PostList.RefreshCommand.Execute(null);
        PostPublisher.PublishFailed += async (_, exception) =>
            await DisplayAlertAsync("Post could not be published", exception.Message, "OK");
        PostList.RequestFailed += async (_, exception) =>
            await DisplayAlertAsync("Posts unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await LoadIdentityAsync();
    }

    private async Task LoadIdentityAsync()
    {
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            var isAdmin = string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase);
            PostList.UserId = user?.Id ?? string.Empty;
            PostList.IsAdmin = isAdmin;
            PostPublisher.AuthorId = user?.Id ?? string.Empty;
            PostPublisher.IsAdmin = isAdmin;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Account status unavailable", exception.Message, "OK");
        }
    }
}
