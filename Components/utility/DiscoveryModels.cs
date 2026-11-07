using System.Text.Json.Serialization;

namespace EEE.Components.Utility;

public sealed class DiscoveryProduct
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Price { get; set; }
    public string? Form { get; set; }
    public string? NumberPcs { get; set; }
    public string? Weight { get; set; }
    public string? RegulatoryClassification { get; set; }
    public string[] Images { get; set; } = [];
    public DiscoveryCategory? Category { get; set; }
    public DiscoveryBrand? Brand { get; set; }
    public List<DiscoveryIngredient> ActiveIngredients { get; set; } = [];
    public List<DiscoveryStock> Stock { get; set; } = [];
    public List<CommerceBulkPrice> BulkPrices { get; set; } = [];

    public string ImageUrl => Images.FirstOrDefault(image => !string.IsNullOrWhiteSpace(image)) ?? string.Empty;
    public int StockCount => Stock.Sum(item => item.AddedQuantity);
    public bool InStock => StockCount > 0;

    public double PriceForRole(string? role)
    {
        var oldestStock = Stock.OrderBy(item => item.CreatedAt).FirstOrDefault(item => item.AddedQuantity > 0)
            ?? Stock.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        var basePrice = oldestStock?.CostPerProduct ?? oldestStock?.PricePerProduct ?? Price;
        var markup = role?.ToLowerInvariant() switch
        {
            "professional" => 1.2,
            "wholesaler" or "admin" or "staff" => 1.1,
            _ => 1.35
        };
        return basePrice * markup;
    }

    public string PriceText(string? role) => $"₦{Math.Ceiling(PriceForRole(role) / 5) * 5:N0}";
}

public sealed class DiscoveryStock
{
    public int AddedQuantity { get; set; }
    public double? CostPerProduct { get; set; }
    public double? PricePerProduct { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DiscoveryCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public List<DiscoveryCategoryProduct> Products { get; set; } = [];
    public DiscoveryCategoryCount? Count { get; set; }
    [JsonPropertyName("_count")]
    public DiscoveryCategoryCount? ProductCounts { get; set; }
    public int ProductCount => Count?.Products ?? ProductCounts?.Products ?? Products.Count;
    public string ImageUrl => !string.IsNullOrWhiteSpace(Image)
        ? Image
        : Products.SelectMany(product => product.Images).FirstOrDefault(image => !string.IsNullOrWhiteSpace(image)) ?? string.Empty;
}

public sealed class DiscoveryCategoryProduct
{
    public string[] Images { get; set; } = [];
}

public sealed class DiscoveryCategoryCount
{
    public int Products { get; set; }
}

public sealed class DiscoveryBrand
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class DiscoveryIngredient
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class DiscoveryFeaturedProduct
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public DiscoveryProduct? Product { get; set; }
}

public sealed class DiscoveryDataColumn
{
    public DiscoveryDataColumn() { }
    public DiscoveryDataColumn(string key, string title)
    {
        Key = key;
        Title = title;
    }

    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public sealed class DiscoveryDataTableRow
{
    public object? Source { get; set; }
    public List<DiscoveryDataTableCell> Cells { get; set; } = [];
}

public sealed class DiscoveryDataTableCell
{
    public string Text { get; set; } = string.Empty;
    public string SortValue { get; set; } = string.Empty;
}

public sealed class DiscoveryIngredientGroup
{
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string[] Images { get; set; } = [];
}

public sealed class DiscoveryPost
{
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ContentUrl { get; set; }
    public string Type { get; set; } = "image";
    public string Category { get; set; } = "General";
    public DateTime CreatedAt { get; set; }
}
