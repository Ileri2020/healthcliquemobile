using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class TermsPage : ContentPage
{
    public TermsPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/terms";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "Terms of Service", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "Last updated: 2024", FontSize = 12, TextColor = Colors.Gray });
        Workspace.Children.Add(new Label { Text = "By using HealthClique, you agree to these terms. Please read them carefully.", FontSize = 14, Margin = new Thickness(0, 8, 0, 0) });
        Workspace.Children.Add(new Label { Text = "Use of Service", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "You may use HealthClique only for lawful purposes and in accordance with these Terms. You agree not to misuse the platform.", FontSize = 14 });
        Workspace.Children.Add(new Label { Text = "Purchases", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "All purchases are subject to product availability. We reserve the right to refuse or cancel orders at our discretion.", FontSize = 14 });
    }
}

