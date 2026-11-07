using System.Text.Json.Serialization;

namespace EEE.Components.Utility;

public sealed class PartnerBrand
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    [JsonPropertyName("_count")]
    public PartnerBrandCount? ProductCount { get; set; }
    public int Products => ProductCount?.Products ?? 0;
}

public sealed class PartnerBrandCount
{
    public int Products { get; set; }
}

public sealed class CommunityUser
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public sealed class CommunityPost
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ContentUrl { get; set; }
    public string Type { get; set; } = "image";
    public string Category { get; set; } = "General";
    public DateTime CreatedAt { get; set; }
    public CommunityUser? User { get; set; }
    public string AuthorName => string.IsNullOrWhiteSpace(User?.Name) ? "Author" : User.Name;
    public string MediaLabel => Type.Equals("audio", StringComparison.OrdinalIgnoreCase) ? "Play audio" : "Open video";
    public string DateLabel => CreatedAt == default ? string.Empty : CreatedAt.ToLocalTime().ToString("MMM d, yyyy");
}

public sealed class CommunityReviewProduct
{
    public List<CommunityReview> Reviews { get; set; } = [];
}

public sealed class CommunityLike
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public sealed class CommunityComment
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = "user";
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DateLabel => CreatedAt == default ? string.Empty : CreatedAt.ToLocalTime().ToString("d");
}

public sealed class CommunityReview
{
    public string Id { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public CommunityUser? User { get; set; }
    public string AuthorName => string.IsNullOrWhiteSpace(User?.Name) ? "Anonymous User" : User.Name;
    public string DateLabel => CreatedAt == default ? string.Empty : CreatedAt.ToLocalTime().ToString("d");
    public string Stars => new string('★', Math.Clamp(Rating, 0, 5)) + new string('☆', 5 - Math.Clamp(Rating, 0, 5));
}

public sealed class ProductSignupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "customer";
    public string ProfessionalType { get; set; } = string.Empty;
    public string RegNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public string FacilityRegNumber { get; set; } = string.Empty;
}
