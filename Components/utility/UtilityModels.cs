using System.Windows.Input;

using System.Text.Json.Serialization;

namespace EEE.Components.Utility;

public sealed class UtilityNavigationItem
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public ICommand? Command { get; set; }
}

public sealed class SocialLink
{
    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
}

public sealed class ContactMessage
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = "suggestion";
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("emailto")]
    public string EmailTo { get; set; } = string.Empty;
}

public enum LocalOperationMode
{
    Online,
    Offline
}
