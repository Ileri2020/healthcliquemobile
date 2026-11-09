using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EEE.Components.Utility;

public sealed class CommerceProduct
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public string[] Images { get; set; } = [];
    public CommerceCategory? Category { get; set; }
    public List<CommerceBulkPrice> BulkPrices { get; set; } = [];
}

public sealed class CommerceCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class CommerceBulkPrice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double Price { get; set; }
}

public sealed class CommerceCartItem : INotifyPropertyChanged
{
    private int _quantity = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = string.Empty;
    public CommerceProduct? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public double Price { get; set; }
    public double? CustomPrice { get; set; }
    public string? CustomName { get; set; }
    public string? BulkPriceId { get; set; }
    public string? BulkName { get; set; }
    public string Category { get; set; } = "Medical";
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value)
                return;
            _quantity = value;
            OnPropertyChanged();
        }
    }
    public bool IsSpecial { get; set; }
    public string DisplayName => IsSpecial ? CustomName ?? Name : Product?.Name ?? CustomName ?? Name;
    public string SummaryPrice => $"₦{((CustomPrice ?? Product?.Price ?? Price) * Quantity):N0}";
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class CommerceShippingAddress
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string DisplayText => string.Join(", ", new[] { Address, City, State }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class CommerceCart
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public double Total { get; set; }
    public double? DeliveryFee { get; set; }
    public double? DiscountAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string? PrescriptionUrl { get; set; }
    public string? PharmacistSummary { get; set; }
    public List<CommerceCartItem> Products { get; set; } = [];
    public CommercePayment? Payment { get; set; }
    public CommerceChatUser? User { get; set; }
}

public sealed class CommercePayment
{
    public string Method { get; set; } = string.Empty;
    [JsonPropertyName("tx_ref")]
    public string TxRef { get; set; } = string.Empty;
    public double Amount { get; set; }
}

public sealed class CommerceChatUser
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Role { get; set; } = "customer";
    public string AvatarUrl { get; set; } = string.Empty;
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacy { get; set; }
    public bool AcceptedReturns { get; set; }
    public List<CommerceShippingAddress> Addresses { get; set; } = [];
}

public static class CommerceSessionState
{
    public static CommerceChatUser? CurrentUser { get; set; }
}

public sealed class CommerceMessage
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public CommerceChatUser? Sender { get; set; }
}

public sealed class CommerceFeaturedProduct
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public CommerceProduct? Product { get; set; }
}

public sealed class CommerceConcern
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
}
