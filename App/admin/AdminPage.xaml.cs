using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class AdminPage : ContentPage
{
    public AdminPage()
    {
        InitializeComponent();
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddLabel("Administration", 30, true);
        AddLabel("Manage users and operational workflows using the HealthClique service.");
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                AddLabel("Administrator access is required.", 16, true, true);
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }

            var links = new[]
            {
                ("Analytics", "/admin/analytics"),
                ("Data sheet", "/sheet"),
                ("Register customer", "/staff"),
                ("Vitals", "/vitals"),
                ("Sales", "/sales"),
                ("Stock", "/stock"),
                ("Count", "/count"),
                ("Expenses", "/expenses")
            };
            foreach (var (title, route) in links)
                Workspace.Children.Add(CreateButton(title, async () => await PageLayout.NavigateToAsync(route), outline: true));

            var users = new AdminUserManager { ApiBaseUrl = PageLayout.ApiBaseUrl, IsAdmin = true };
            users.OperationFailed += async (_, exception) =>
                await DisplayAlertAsync("User management failed", exception.Message, "OK");
            Workspace.Children.Add(users);
            users.RefreshCommand.Execute(null);

            var bulk = new AdminBulkManager { ApiBaseUrl = PageLayout.ApiBaseUrl, IsAdmin = true };
            bulk.OperationFailed += async (_, exception) =>
                await DisplayAlertAsync("Bulk operation failed", exception.Message, "OK");
            Workspace.Children.Add(bulk);

            var publisher = new PostButton
            {
                ApiBaseUrl = PageLayout.ApiBaseUrl,
                AuthorId = user!.Id,
                IsAdmin = true
            };
            publisher.PublishFailed += async (_, exception) =>
                await DisplayAlertAsync("Post could not be published", exception.Message, "OK");
            Workspace.Children.Add(publisher);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
        {
            AddLabel($"Administration service unavailable: {exception.Message}", error: true);
        }
    }

    private void AddLabel(string text, double size = 14, bool bold = false, bool error = false) =>
        Workspace.Children.Add(new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        });

    private static Button CreateButton(string text, Func<Task> onClick, bool outline = false)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
