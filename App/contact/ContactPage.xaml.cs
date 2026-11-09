using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class ContactPage : ContentPage
{
    public ContactPage()
    {
        InitializeComponent();
        ContactMessageForm.EndpointUrl = UtilityApi.CreateUri(PageLayout.ApiBaseUrl, "api/data/notification").ToString();
        ContactMessageForm.SubmissionFailed += async (_, exception) =>
            await DisplayAlertAsync("Message could not be sent", exception.Message, "OK");
        SupportChat.ApiBaseUrl = PageLayout.ApiBaseUrl;
        SupportChat.RequestFailed += async (_, exception) =>
            await DisplayAlertAsync("Chat unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await LoadIdentityAsync();
    }

    private async Task LoadIdentityAsync()
    {
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            SupportChat.CurrentUserId = user?.Id ?? string.Empty;
            SupportChat.CurrentUserName = user?.Name ?? string.Empty;
            SupportChat.CurrentUserRole = user?.Role ?? "customer";
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Account status unavailable", exception.Message, "OK");
        }
    }
}
