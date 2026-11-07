namespace EEE.Components.Utility;

internal static class NavigationUtility
{
    public static async Task NavigateAsync(UtilityNavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Command is not null)
        {
            if (item.Command.CanExecute(null))
                item.Command.Execute(null);
            return;
        }

        if (string.IsNullOrWhiteSpace(item.Route))
            throw new InvalidOperationException($"Navigation item '{item.Name}' has no route or command.");

        if (Shell.Current is null)
            throw new InvalidOperationException("Shell navigation is unavailable. Add the page route to AppShell before using this navigation item.");

        await Shell.Current.GoToAsync(item.Route);
    }
}
