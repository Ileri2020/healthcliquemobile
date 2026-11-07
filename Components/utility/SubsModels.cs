namespace EEE.Components.Utility;

public sealed class PrescriptionIdentifiedItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Grams { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public string[] ActiveIngredients { get; set; } = [];
    public string Category { get; set; } = "General";
    public string Instructions { get; set; } = string.Empty;
}

public sealed class PrescriptionMatchGroup
{
    public PrescriptionIdentifiedItem IdentifiedItem { get; set; } = new();
    public List<PrescriptionMatchOption> Options { get; set; } = [];
}

public sealed class PrescriptionMatchOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public string[] Images { get; set; } = [];
    public int RequestedQuantity { get; set; } = 1;
    public string RequestedGrams { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscoveryBrand? Brand { get; set; }
    public List<DiscoveryIngredient> ActiveIngredients { get; set; } = [];
    public string ImageUrl => Images.FirstOrDefault(image => !string.IsNullOrWhiteSpace(image)) ?? string.Empty;
    public string IngredientSummary => string.Join(", ", ActiveIngredients.Select(item => item.Name));
    public string PriceText => $"₦{Price:N0}";
}

public sealed class PrescriptionCartRequest
{
    public string DrugName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? PrescriptionFileName { get; set; }
}

public sealed class UserEditProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Role { get; set; } = "customer";
    public string AvatarUrl { get; set; } = string.Empty;
    public string ProfessionalType { get; set; } = string.Empty;
    public string RegNumber { get; set; } = string.Empty;
    public string LicenseImage { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public string FacilityRegNumber { get; set; } = string.Empty;
}

public sealed class UserAgreementState
{
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacy { get; set; }
    public bool AcceptedReturns { get; set; }
    public bool AllAccepted => AcceptedTerms && AcceptedPrivacy && AcceptedReturns;
}

public sealed class TestimonialAuthor
{
    public string Avatar { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class TestimonialItem
{
    public TestimonialAuthor Author { get; set; } = new();
    public string? Href { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class ZoomVerse
{
    public string Verse { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
}
