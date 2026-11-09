using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

internal static class InventoryApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T> GetAsync<T>(string baseUrl, string path, CancellationToken cancellationToken = default)
    {
        using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(baseUrl, path), (object?)null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new JsonException($"The inventory endpoint returned an empty response for '{path}'.");
    }

    public static async Task<T> SendAsync<T>(
        string baseUrl,
        HttpMethod method,
        string path,
        object body,
        CancellationToken cancellationToken = default)
    {
        using var response = await UtilityApi.SendAsync(method, UtilityApi.CreateUri(baseUrl, path), body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new JsonException($"The inventory endpoint returned an empty response for '{path}'.");
    }

    public static async Task SendAsync(
        string baseUrl,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        using var response = await UtilityApi.SendAsync(method, UtilityApi.CreateUri(baseUrl, path), body, cancellationToken);
    }

    public static string DateText(DateTime? date) => date?.ToLocalTime().ToString("MMM d, yyyy") ?? "—";
    public static string MoneyText(double? amount) => $"₦{amount.GetValueOrDefault():N0}";
}

public sealed class InventorySaleRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? RangeFrom { get; set; }
    public DateTime? RangeTo { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public double? CashPaid { get; set; }
    public double? PosPayment { get; set; }
    public double? Change { get; set; }
    public double? Total { get; set; }
    public List<string> Products { get; set; } = [];
    public List<InventorySaleLine> Rows { get; set; } = [];
    public List<InventorySaleLine> Sales { get; set; } = [];
}

public sealed class InventorySaleLine
{
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double? Qty { get; set; }
    public double? TotalPcs { get; set; }
    public double? Amount { get; set; }
    public double? Price { get; set; }
    public double? SalesPrice { get; set; }
    public double? Total { get; set; }
}

public sealed class InventorySalesCatalog
{
    public List<string> ProductNames { get; set; } = [];
    public Dictionary<string, InventorySalePricing> StockPricing { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class InventorySalePricing
{
    public double? PcsSalesPrice { get; set; }
    public double? PackSalesPrice { get; set; }
    public double? CartonSalesPrice { get; set; }
    public double? WholesalePcsSalesPrice { get; set; }
}

public sealed class InventoryStockRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? RangeFrom { get; set; }
    public DateTime? RangeTo { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RepName { get; set; } = string.Empty;
    public double? Total { get; set; }
    public double? AmountPaid { get; set; }
    public double? Balance { get; set; }
    public List<InventoryStockLine> Stocks { get; set; } = [];
}

public sealed class InventoryStockLine
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RepName { get; set; } = string.Empty;
    public bool Carton { get; set; }
    public double? CartonQty { get; set; }
    public double? PacksPerCarton { get; set; }
    public bool Pack { get; set; }
    public double? PackQty { get; set; }
    public double? PcsCount { get; set; }
    public double? PcsQty { get; set; }
    public double? TotalPcs { get; set; }
    public double? CostPrice { get; set; }
    public double? CartonSalesPrice { get; set; }
    public double? PackSalesPrice { get; set; }
    public double? PcsSalesPrice { get; set; }
    public DateTime? Expiry { get; set; }
}

public sealed class InventoryProductAvailability
{
    public string ProductName { get; set; } = string.Empty;
    public double? AvailablePieces { get; set; }
    public DateTime? Expiry { get; set; }
    public DateTime? ShortestExpiry { get; set; }
    public double? PacksPerCarton { get; set; }
    public double? PiecesPerPack { get; set; }
    public double? AvailablePcs { get; set; }
}

public sealed class InventoryCountProductsResponse
{
    public List<InventoryCountProduct> Products { get; set; } = [];
}

public sealed class InventoryCountProduct
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductKey { get; set; } = string.Empty;
    public double ExpectedPcs { get; set; }
    public double? CountedPcs { get; set; }
    public double? AvailablePieces { get; set; }
    public double? SavedCountedPcs { get; set; }
    public DateTime? Expiry { get; set; }
    public DateTime? SavedExpiry { get; set; }
    public string ShelfName { get; set; } = string.Empty;
}

public sealed class InventoryCountRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int TotalLines { get; set; }
    public int CountedLines { get; set; }
    public InventoryShelf? Shelf { get; set; }
    public List<InventoryCountLine> Lines { get; set; } = [];
}

public sealed class InventoryCountLine
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double ExpectedPcs { get; set; }
    public double? CountedPcs { get; set; }
    public double? DifferencePcs { get; set; }
    public double? Difference { get; set; }
}

public sealed class InventoryShelf
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class InventoryUnstockedReport
{
    public List<InventoryUnstockedSale> Rows { get; set; } = [];
    public List<InventoryUnstockedSale> LossRows { get; set; } = [];
}

public sealed class InventoryUnstockedSale
{
    public string Id { get; set; } = string.Empty;
    public string SaleId { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double? Quantity { get; set; }
    public double? Amount { get; set; }
    public double? Loss { get; set; }
}
