using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        LoginForm.ApiBaseUrl = PageLayout.ApiBaseUrl;
        LoginForm.LoginSucceeded += async (_, user) =>
        {
            CommerceSessionState.CurrentUser = user;
            await PageLayout.NavigateToAsync("/home");
        };
        LoginForm.SignUpRequested += async (_, _) => await PageLayout.NavigateToAsync("/signup");
        LoginForm.SocialSignInRequested += async (_, provider) =>
            await DisplayAlertAsync("Social sign-in", $"Continue signing in with {provider} on the HealthClique website.", "OK");
        LoginForm.RequestFailed += async (_, exception) =>
            await DisplayAlertAsync("Sign-in failed", exception.Message, "OK");
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
    }
}
