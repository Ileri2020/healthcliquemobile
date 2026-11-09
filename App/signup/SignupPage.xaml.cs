using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class SignupPage : ContentPage
{
    public SignupPage()
    {
        InitializeComponent();
        SignupForm.ApiBaseUrl = PageLayout.ApiBaseUrl;
        SignupForm.SignupSucceeded += async (_, user) =>
        {
            CommerceSessionState.CurrentUser = user;
            await DisplayAlertAsync("Account created", "Your account is ready. Sign in to continue.", "OK");
            await PageLayout.NavigateToAsync("/login");
        };
        SignupForm.LoginRequested += async (_, _) => await PageLayout.NavigateToAsync("/login");
        SignupForm.GoogleSignInRequested += async (_, _) =>
            await DisplayAlertAsync("Social sign-in", "Continue signing in with Google on the HealthClique website.", "OK");
        SignupForm.RequestFailed += async (_, exception) =>
            await DisplayAlertAsync("Sign-up failed", exception.Message, "OK");
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
    }
}
