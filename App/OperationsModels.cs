using System.Text.Json.Serialization;

namespace EEE.ClientApp.Pages;

public sealed class ExpenseRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double Total { get; set; }
    public List<ExpenseItem> Items { get; set; } = [];
}

public sealed class ExpenseItem
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public bool Span { get; set; }
    public DateTime? SpanFrom { get; set; }
    public DateTime? SpanTo { get; set; }
}

public sealed class ExpenseCreateRow
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public bool Span { get; set; }
    public string? SpanFrom { get; set; }
    public string? SpanTo { get; set; }
}

public sealed class StockCountProductsResponse
{
    public List<StockCountProduct> Products { get; set; } = [];
    public List<InventoryShelf> Shelves { get; set; } = [];
    public StockCountSession? SavedCount { get; set; }
}

public sealed class StockCountProduct
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductKey { get; set; } = string.Empty;
    public double AvailablePieces { get; set; }
    public DateTime? ShortestExpiry { get; set; }
    public double PacksPerCarton { get; set; }
    public double PiecesPerPack { get; set; }
    public bool CartonEnabled { get; set; }
    public bool PackEnabled { get; set; }
    public string? ShelfName { get; set; }
    public double? SavedCountedPcs { get; set; }
    public double? SavedCountedPacks { get; set; }
    public DateTime? SavedExpiry { get; set; }
    public string? CountedPcs { get; set; }
    public string? ExpiryInput { get; set; }
}

public sealed class StockCountSession
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? ShelfName { get; set; }
    public bool Normalized { get; set; }
    public DateTime? NormalizedAt { get; set; }
    public int TotalLines { get; set; }
    public int CountedLines { get; set; }
    public List<StockCountLine> Lines { get; set; } = [];
}

public sealed class StockCountLine
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ShelfName { get; set; }
    public double ExpectedPcs { get; set; }
    public double? CountedPcs { get; set; }
    public double? DifferencePcs { get; set; }
    public double? NormalizedPcs { get; set; }
    public DateTime? Expiry { get; set; }
}

public sealed class StockCountSaveLine
{
    public string ProductName { get; set; } = string.Empty;
    public string? ShelfName { get; set; }
    public string? ShelfId { get; set; }
    public double ExpectedPcs { get; set; }
    public double? CountedPcs { get; set; }
    public string? Expiry { get; set; }
}

public sealed class StockCountHistorySummary
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? ShelfName { get; set; }
    public bool Normalized { get; set; }
    public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("_count")]
    public StockCountLineCount? Count { get; set; }
}

public sealed class StockCountLineCount
{
    public int Lines { get; set; }
}

public sealed class StaffAccountCreateResponse
{
    public StaffCreatedUser? User { get; set; }
    public string DefaultPassword { get; set; } = string.Empty;
}

public sealed class StaffCreatedUser
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
}

public sealed class VitalReading
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int Pulse { get; set; }
    public double? BloodSugar { get; set; }
}
