using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class LunchPage : ContentPage
{
    public LunchPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/lunch";
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += (_, _) => BuildContent();
    }

    private void BuildContent()
    {
        Workspace.Children.Add(new Label { Text = "Lunch", FontSize = 24, FontAttributes = FontAttributes.Bold });
        Workspace.Children.Add(new Label { Text = "Today's lunch menu will appear here.", FontSize = 14 });
    }
}

