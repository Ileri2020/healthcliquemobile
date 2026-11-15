using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class MyUiPage : ContentPage
{
    public MyUiPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/myui";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "UI Gallery", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "Component showcase and UI testing area.", FontSize = 14 });

        // Sample button
        Workspace.Children.Add(new Label { Text = "Buttons", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Button { Text = "Primary Button", BackgroundColor = Color.FromArgb("#16a34a"), TextColor = Colors.White });
        Workspace.Children.Add(new Button { Text = "Secondary Button", BackgroundColor = Color.FromArgb("#374151"), TextColor = Colors.White });

        // Sample labels
        Workspace.Children.Add(new Label { Text = "Typography", FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 0) });
        Workspace.Children.Add(new Label { Text = "Heading Text", FontSize = 22, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "Body text — regular weight, readable size.", FontSize = 14 });
        Workspace.Children.Add(new Label { Text = "Small caption text.", FontSize = 11, TextColor = Colors.Gray });
    }
}

