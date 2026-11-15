using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/about";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "About HealthClique", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "HealthClique is a trusted pharmacy platform providing quality healthcare products and services.", FontSize = 14 });
        Workspace.Children.Add(new Label { Text = "Our Mission", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "To make quality healthcare accessible and affordable for everyone.", FontSize = 14 });
    }
}

