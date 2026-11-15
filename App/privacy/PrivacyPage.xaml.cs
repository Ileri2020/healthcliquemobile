using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class PrivacyPage : ContentPage
{
    public PrivacyPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/privacy";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "Privacy Policy", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "Last updated: 2024", FontSize = 12, TextColor = Colors.Gray });
        Workspace.Children.Add(new Label { Text = "Your privacy is important to us. This policy explains how HealthClique collects, uses, and protects your personal information.", FontSize = 14, Margin = new Thickness(0, 8, 0, 0) });
        Workspace.Children.Add(new Label { Text = "Data We Collect", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "We collect information you provide directly to us, such as when you create an account, make a purchase, or contact us for support.", FontSize = 14 });
        Workspace.Children.Add(new Label { Text = "How We Use Your Data", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "We use your information to process orders, provide customer support, and improve our services.", FontSize = 14 });
    }
}

