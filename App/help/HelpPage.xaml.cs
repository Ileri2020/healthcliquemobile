using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/help";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "Help & FAQ", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "How can we help you?", FontSize = 16, Margin = new Thickness(0, 8, 0, 0) });
        Workspace.Children.Add(new Label { Text = "For support, please contact us at support@healthcliquecare.com or visit our website at healthcliquecare.com.", FontSize = 14 });
        Workspace.Children.Add(new Label { Text = "Common Questions", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "• How do I place an order?\n• How do I track my order?\n• How do I return a product?\n• How do I contact support?", FontSize = 14 });
    }
}

