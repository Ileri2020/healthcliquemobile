namespace EEE.Components.Utility;

public sealed class HeavilyDiscountedProduct
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public DiscoveryProduct Product { get; set; } = new();
}

public sealed class HeavilyDiscountedProductCard
{
    public HeavilyDiscountedProductCard(HeavilyDiscountedProduct item, string? userRole, string? userId)
    {
        Item = item;
        Product = item.Product;
        Name = item.Product.Name;
        Description = item.Product.Description ?? string.Empty;
        ImageUrl = item.Product.ImageUrl;
        PriceText = item.Product.PriceText(userRole);
        ApprovalText = item.Approved ? "Approved" : "Pending approval";
        IsPending = !item.Approved;
        IsCreator = !string.IsNullOrWhiteSpace(userId) && item.CreatorId == userId;
        StatusText = item.Approved ? "Approved" : "Pending";
    }

    public HeavilyDiscountedProduct Item { get; }
    public DiscoveryProduct Product { get; }
    public string Name { get; }
    public string Description { get; }
    public string ImageUrl { get; }
    public string PriceText { get; }
    public string ApprovalText { get; }
    public string StatusText { get; }
    public string Id => Item.Id;
    public bool IsPending { get; }
    public bool IsCreator { get; }
}

public sealed class HeavilyDiscountedProductOption
{
    public HeavilyDiscountedProductOption(DiscoveryProduct product, bool isActive)
    {
        Product = product;
        Name = product.Name;
        IsActive = isActive;
    }

    public DiscoveryProduct Product { get; }
    public string Name { get; }
    public bool IsActive { get; }
    public bool CanAdd => !IsActive;
    public string AddText => IsActive ? "Added" : "Add+";
}
