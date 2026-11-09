using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class CountPage : ContentPage, IQueryAttributable
{
    private string _path = string.Empty;
    private string? _from;
    private string? _to;
    private string? _countId;
    private DateTime _date = DateTime.Today;
    private CommerceChatUser? _user;
    private bool IsAdmin => string.Equals(_user?.Role, "admin", StringComparison.OrdinalIgnoreCase);

    public CountPage()
    {
        InitializeComponent();
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await RenderAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _path = Query(query, "path") ?? string.Empty;
        _from = Query(query, "from");
        _to = Query(query, "to");
        _countId = Query(query, "id");
        if (DateTime.TryParse(Query(query, "date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            _date = date.Date;
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddHeading(IsHistory ? "Stock count history" : "Stock count", "Review expected stock and record physical counts.");
        AddNavigation();
        try
        {
            _user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = _user;
            if (_user is null
                || (!string.Equals(_user.Role, "admin", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(_user.Role, "staff", StringComparison.OrdinalIgnoreCase)))
            {
                Workspace.Children.Clear();
                AddHeading("Stock count", "Stock counts are available to signed-in staff and administrators.");
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }

            if (IsHistory)
                await RenderHistoryAsync();
            else
                await RenderCountFormAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Stock count service unavailable: {exception.Message}", error: true);
        }
    }

    private bool IsHistory => !string.IsNullOrWhiteSpace(_path);

    private async Task RenderCountFormAsync()
    {
        if (!IsAdmin)
            AddLabel("Only administrators can save stock counts. Staff may review the catalog and count history.");
        if (_countId is not null)
        {
            await RenderHistoryAsync();
            return;
        }

        Workspace.Children.Add(new Label { Text = "Count date", FontAttributes = FontAttributes.Bold });
        var datePicker = new DatePicker { Date = _date, MaximumDate = DateTime.Today.AddYears(2) };
        datePicker.DateSelected += async (_, args) =>
        {
            var selectedDate = args.NewDate.GetValueOrDefault().Date;
            if (selectedDate == _date.Date)
                return;
            _date = selectedDate;
            await RenderAsync();
        };
        Workspace.Children.Add(datePicker);

        var response = await InventoryApi.GetAsync<StockCountProductsResponse>(
            PageLayout.ApiBaseUrl, $"api/inventory/count?date={_date:yyyy-MM-dd}");
        var products = response.Products;
        if (products.Count == 0)
        {
            AddLabel("No products are available for this count date.");
            return;
        }

        const string allShelves = "All shelves";
        var shelfNames = new[] { allShelves }.Concat(response.Shelves.Select(shelf => shelf.Name)).ToList();
        var shelfPicker = new Picker { Title = "Shelf", ItemsSource = shelfNames, SelectedItem = allShelves };
        Workspace.Children.Add(shelfPicker);
        if (!string.IsNullOrWhiteSpace(response.SavedCount?.ShelfName))
            shelfPicker.SelectedItem = response.SavedCount.ShelfName;

        var search = new Entry { Placeholder = "Filter products" };
        Workspace.Children.Add(search);
        var rows = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(rows);
        var productCards = new Dictionary<StockCountProduct, Border>();

        void RenderRows()
        {
            if (productCards.Count == 0)
            {
                foreach (var product in products)
                {
                    var counted = new Entry
                    {
                        Placeholder = "Counted pieces",
                        Keyboard = Keyboard.Numeric,
                        Text = product.SavedCountedPcs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
                    };
                    var existingExpiry = (product.SavedExpiry ?? product.ShortestExpiry)?.ToLocalTime().Date;
                    var expiry = new DatePicker { Date = existingExpiry ?? _date };
                    var panel = new VerticalStackLayout
                    {
                        Padding = 10,
                        Spacing = 5,
                        Children =
                        {
                            new Label { Text = product.ProductName, FontAttributes = FontAttributes.Bold },
                            new Label { Text = $"Expected: {product.AvailablePieces:N0} pcs · Shelf: {product.ShelfName ?? "Unassigned"}" },
                            counted,
                            new Label { Text = "Expiry date" },
                            expiry
                        },
                        BackgroundColor = Color.FromArgb("#F3F4F6")
                    };
                    var card = new Border
                    {
                        Padding = 2,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                        Content = panel
                    };
                    rows.Children.Add(card);
                    productCards.Add(product, card);
                    product.CountedPcs = counted.Text;
                    counted.TextChanged += (_, args) => product.CountedPcs = args.NewTextValue;
                    product.ExpiryInput = existingExpiry?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    expiry.DateSelected += (_, args) => product.ExpiryInput = args.NewDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }

            var selectedShelfName = shelfPicker.SelectedItem?.ToString();
            foreach (var (product, card) in productCards)
            {
                card.IsVisible =
                    (selectedShelfName == allShelves || string.Equals(product.ShelfName, selectedShelfName, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(search.Text)
                        || product.ProductName.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        search.TextChanged += (_, _) => RenderRows();
        shelfPicker.SelectedIndexChanged += (_, _) => RenderRows();
        RenderRows();
        if (IsAdmin)
            Workspace.Children.Add(CreateButton(response.SavedCount is null ? "Save stock count" : "Save count changes",
                async () => await SaveCountAsync(products.Where(product =>
                    shelfPicker.SelectedItem?.ToString() == allShelves
                    || string.Equals(product.ShelfName, shelfPicker.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase)).ToList(),
                    response,
                    shelfPicker.SelectedItem?.ToString() == allShelves ? null : shelfPicker.SelectedItem?.ToString())));
        Workspace.Children.Add(CreateButton("Count history", async () => await PageLayout.NavigateToAsync("/count/all"), outline: true));
    }

    private async Task SaveCountAsync(
        List<StockCountProduct> products,
        StockCountProductsResponse response,
        string? selectedShelf)
    {
        var lines = new List<StockCountSaveLine>();
        foreach (var product in products)
        {
            double? counted = null;
            if (!string.IsNullOrWhiteSpace(product.CountedPcs))
            {
                if (!double.TryParse(product.CountedPcs, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    || !double.IsFinite(parsed) || parsed < 0)
                {
                    await DisplayAlertAsync("Invalid count", $"Enter a valid non-negative count for {product.ProductName}.", "OK");
                    return;
                }
                counted = parsed;
            }

            var defaultExpiry = (product.SavedExpiry ?? product.ShortestExpiry)?.ToLocalTime().Date;
            var hasExpiry = DateTime.TryParse(product.ExpiryInput, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiryDate);
            var expiryChanged = hasExpiry && expiryDate.Date != defaultExpiry;
            if (counted is null && !expiryChanged)
                continue;

            lines.Add(new StockCountSaveLine
            {
                ProductName = product.ProductName,
                ShelfName = selectedShelf ?? product.ShelfName,
                ShelfId = response.Shelves.FirstOrDefault(shelf => shelf.Name == selectedShelf)?.Id,
                ExpectedPcs = product.AvailablePieces,
                CountedPcs = counted,
                Expiry = hasExpiry ? new DateTime(expiryDate.Year, expiryDate.Month, expiryDate.Day, 0, 0, 0, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture) : null
            });
        }

        if (lines.Count == 0)
        {
            await DisplayAlertAsync("Stock count", "Enter at least one product count or update an expiry date.", "OK");
            return;
        }
        try
        {
            var payload = new
            {
                date = _date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                shelfName = selectedShelf,
                shelfId = response.Shelves.FirstOrDefault(shelf => shelf.Name == selectedShelf)?.Id,
                lines
            };
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, HttpMethod.Post, "api/inventory/count", payload);
            await DisplayAlertAsync("Stock count", "Stock count saved successfully.", "OK");
            await RenderAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Unable to save stock count", exception.Message, "OK");
        }
    }

    private async Task RenderHistoryAsync()
    {
        if (!string.IsNullOrWhiteSpace(_countId))
        {
            var session = await InventoryApi.GetAsync<StockCountSession>(
                PageLayout.ApiBaseUrl, $"api/inventory/count/range?id={Uri.EscapeDataString(_countId)}");
            await RenderSessionAsync(session);
            return;
        }

        var query = BuildHistoryQuery();
        if (_path.Equals("all", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(_from) && string.IsNullOrWhiteSpace(_to))
        {
            var summaries = await InventoryApi.GetAsync<List<StockCountHistorySummary>>(PageLayout.ApiBaseUrl, query);
            var fullSessions = await Task.WhenAll(summaries.Select(async item =>
                await InventoryApi.GetAsync<StockCountSession>(
                    PageLayout.ApiBaseUrl, $"api/inventory/count/range?id={Uri.EscapeDataString(item.Id)}")));
            foreach (var session in fullSessions.OrderByDescending(item => item.Date))
                AddSessionSummary(session);
            if (fullSessions.Length == 0)
                AddLabel("No saved stock counts found.");
            return;
        }

        var loaded = await InventoryApi.GetAsync<List<StockCountSession>>(PageLayout.ApiBaseUrl, query);
        foreach (var session in loaded.OrderByDescending(item => item.Date))
            AddSessionSummary(session);
        if (loaded.Count == 0)
            AddLabel("No saved stock counts found.");
    }

    private string BuildHistoryQuery()
    {
        if (_path.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(_from) || !string.IsNullOrWhiteSpace(_to))
                return $"api/inventory/count/range?from={Uri.EscapeDataString(_from ?? _to!)}&to={Uri.EscapeDataString(_to ?? _from!)}";
            return "api/inventory/count/range?history=true";
        }

        var range = _path.Split("_to_", StringSplitOptions.None);
        if (range.Length == 2)
            return $"api/inventory/count/range?from={Uri.EscapeDataString(range[0])}&to={Uri.EscapeDataString(range[1])}";
        return $"api/inventory/count/range?date={Uri.EscapeDataString(_path)}";
    }

    private void AddSessionSummary(StockCountSession session)
    {
        var card = new Border
        {
            Padding = 12,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    new Label { Text = session.Date.ToLocalTime().ToString("MMM d, yyyy"), FontAttributes = FontAttributes.Bold },
                    new Label { Text = $"Shelf: {session.ShelfName ?? "All shelves"} · {session.Lines.Count} product(s)" },
                    new Label { Text = session.Normalized ? "Baseline normalized" : "Pending review" }
                }
            }
        };
        Workspace.Children.Add(card);
        Workspace.Children.Add(CreateButton("View count", async () =>
            await PageLayout.NavigateToAsync($"/count/{session.Date:yyyy-MM-dd}?id={Uri.EscapeDataString(session.Id)}")));
    }

    private async Task RenderSessionAsync(StockCountSession session)
    {
        Workspace.Children.Add(CreateButton("Back to history", async () => await PageLayout.NavigateToAsync("/count/all"), outline: true));
        AddLabel($"{session.Date.ToLocalTime():MMM d, yyyy} · {session.ShelfName ?? "All shelves"}", 20, true);
        if (session.Lines.Count == 0)
            AddLabel("This stock count contains no product lines.");
        foreach (var line in session.Lines)
        {
            var difference = line.DifferencePcs ?? (line.CountedPcs.HasValue ? line.CountedPcs.Value - line.ExpectedPcs : (double?)null);
            var panel = new VerticalStackLayout
            {
                Padding = 10,
                Spacing = 4,
                Children =
                {
                    new Label { Text = line.ProductName, FontAttributes = FontAttributes.Bold },
                    new Label { Text = $"Expected: {line.ExpectedPcs:N0} · Counted: {line.CountedPcs?.ToString("N0") ?? "Not counted"}" },
                    new Label { Text = difference.HasValue ? $"Difference: {difference.Value:+0;-0;0} pcs" : "Difference: —" },
                    new Label { Text = line.NormalizedPcs.HasValue ? $"Approved baseline: {line.NormalizedPcs.Value:N0}" : "Baseline not normalized" }
                }
            };
            Workspace.Children.Add(new Border
            {
                Padding = 2,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Content = panel
            });
            if (IsAdmin && line.CountedPcs.HasValue && !line.NormalizedPcs.HasValue)
                Workspace.Children.Add(CreateButton("Normalize this product", async () =>
                    await NormalizeAsync(session.Id, line.Id)));
        }

        if (IsAdmin && !session.Normalized)
            Workspace.Children.Add(CreateButton("Normalize all counted products", async () => await NormalizeAsync(session.Id, null, normalizeAll: true)));
    }

    private async Task NormalizeAsync(string countId, string? lineId, bool normalizeAll = false)
    {
        try
        {
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, HttpMethod.Post, "api/inventory/count/normalize",
                new { countId, lineId, normalizeAll });
            await DisplayAlertAsync("Stock count", "Count baseline updated.", "OK");
            await RenderAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Unable to normalize count", exception.Message, "OK");
        }
    }

    private void AddNavigation()
    {
        var links = new HorizontalStackLayout { Spacing = 8 };
        links.Children.Add(CreateButton("New count", async () => await PageLayout.NavigateToAsync("/count")));
        links.Children.Add(CreateButton("Count history", async () => await PageLayout.NavigateToAsync("/count/all")));
        links.Children.Add(CreateButton("Expenses", async () => await PageLayout.NavigateToAsync("/expenses")));
        Workspace.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = links, HeightRequest = 48 });
    }

    private void AddHeading(string title, string subtitle)
    {
        AddLabel(title, 28, true);
        AddLabel(subtitle);
    }

    private Label AddLabel(string text, double size = 14, bool bold = false, bool error = false)
    {
        var label = new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        };
        Workspace.Children.Add(label);
        return label;
    }

    private static string? Query(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) ? Uri.UnescapeDataString(value?.ToString() ?? string.Empty) : null;

    private static Button CreateButton(string text, Func<Task> onClick, bool outline = false)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
