using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class StockPage : ContentPage, IQueryAttributable
{
    private readonly List<StockLineEditor> _stockEditors = [];
    private readonly Dictionary<string, Entry> _countEntries = new(StringComparer.OrdinalIgnoreCase);
    private string _path = string.Empty;
    private string? _editId;
    private DateTime _selectedDate = DateTime.Today;
    private DateTime? _rangeTo;
    private List<InventoryCountProduct> _countProducts = [];
    private VerticalStackLayout? _countRows;
    private Entry? _countSearch;

    public StockPage()
    {
        InitializeComponent();
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await RenderAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _path = GetQueryValue(query, "path") ?? string.Empty;
        _editId = GetQueryValue(query, "edit");
        if (DateTime.TryParse(GetQueryValue(query, "date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            _selectedDate = date.Date;
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddHeading(GetPageTitle(), "Connected to the HealthClique inventory service.");
        AddNavigation();
        try
        {
            AddMessage("Checking inventory access...");
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (user is null
                || (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(user.Role, "staff", StringComparison.OrdinalIgnoreCase)))
            {
                Workspace.Children.Clear();
                AddHeading("Stock", "Stock management is available to signed-in staff and administrators.");
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }
            if (!string.IsNullOrWhiteSpace(_editId) && !string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                Workspace.Children.Clear();
                AddHeading("Stock", "Only administrators can edit saved stock purchases.");
                AddNavigation();
                return;
            }

            Workspace.Children.Clear();
            AddHeading(GetPageTitle(), "Connected to the HealthClique inventory service.");
            AddNavigation();
            if (_path.Equals("products", StringComparison.OrdinalIgnoreCase))
                await RenderProductsAsync();
            else if (_path.Equals("count", StringComparison.OrdinalIgnoreCase))
                await RenderCountAsync();
            else if (_path.StartsWith("count/", StringComparison.OrdinalIgnoreCase))
                await RenderCountHistoryAsync(_path["count/".Length..]);
            else if (_path.StartsWith("purchase/", StringComparison.OrdinalIgnoreCase))
                await RenderPurchaseAsync(_path["purchase/".Length..]);
            else if (IsRangePath(_path))
                await RenderHistoryAsync();
            else
                await RenderStockFormAsync(_editId);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            Workspace.Children.Clear();
            AddHeading(GetPageTitle(), "The inventory service could not complete this request.");
            AddMessage(exception.Message, true);
            AddNavigation();
        }
    }

    private string GetPageTitle() =>
        _path.Equals("products", StringComparison.OrdinalIgnoreCase) ? "Stock products"
        : _path.Equals("count", StringComparison.OrdinalIgnoreCase) ? "Stock count"
        : _path.StartsWith("count/", StringComparison.OrdinalIgnoreCase) ? "Stock count history"
        : _path.StartsWith("purchase/", StringComparison.OrdinalIgnoreCase) ? "Stock purchase"
        : IsRangePath(_path) ? "Stock history"
        : string.IsNullOrWhiteSpace(_editId) ? "Record stock purchase" : "Edit stock purchase";

    private void AddNavigation()
    {
        var links = new HorizontalStackLayout { Spacing = 8 };
        links.Children.Add(CreateButton("New purchase", async () => await PageLayout.NavigateToAsync("/stock")));
        links.Children.Add(CreateButton("History", async () => await PageLayout.NavigateToAsync("/stock/all")));
        links.Children.Add(CreateButton("Products", async () => await PageLayout.NavigateToAsync("/stock/products")));
        links.Children.Add(CreateButton("Stock count", async () => await PageLayout.NavigateToAsync("/stock/count")));
        links.Children.Add(CreateButton("Count history", async () => await PageLayout.NavigateToAsync("/stock/count/all")));
        Workspace.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = links, HeightRequest = 48 });
    }

    private async Task RenderStockFormAsync(string? editId)
    {
        Workspace.Children.Add(CreateLabel("Purchase date"));
        var datePicker = new DatePicker { Date = _selectedDate, MaximumDate = DateTime.Today.AddYears(2) };
        datePicker.DateSelected += (_, args) =>
        {
            _selectedDate = args.NewDate.Date;
            _rangeTo = null;
        };
        Workspace.Children.Add(datePicker);

        var company = new Entry { Placeholder = "Supplier / company name" };
        var representative = new Entry { Placeholder = "Supplier representative" };
        var amountPaid = NumberEntry("Amount paid");
        Workspace.Children.Add(company);
        Workspace.Children.Add(representative);
        Workspace.Children.Add(amountPaid);

        var lineContainer = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(CreateLabel("Products", 19, true));
        Workspace.Children.Add(lineContainer);
        var buttons = new HorizontalStackLayout { Spacing = 8 };
        buttons.Children.Add(CreateActionButton("Add product", () => AddStockLine(lineContainer)));
        buttons.Children.Add(CreateButton(editId is null ? "Save purchase" : "Save changes", async () =>
            await SaveStockAsync(editId, company.Text, representative.Text, amountPaid.Text)));
        Workspace.Children.Add(buttons);
        AddStockLine(lineContainer);

        if (editId is null)
            return;

        var purchase = await InventoryApi.GetAsync<InventoryStockRecord>(PageLayout.ApiBaseUrl, $"api/inventory/stock/{Uri.EscapeDataString(editId)}");
        _selectedDate = purchase.Date?.ToLocalTime().Date ?? _selectedDate;
        _rangeTo = purchase.Date is null ? purchase.RangeTo?.ToLocalTime().Date : null;
        datePicker.Date = _selectedDate;
        datePicker.IsEnabled = !_rangeTo.HasValue;
        company.Text = string.IsNullOrWhiteSpace(purchase.CompanyName) ? purchase.Stocks.FirstOrDefault()?.CompanyName : purchase.CompanyName;
        representative.Text = string.IsNullOrWhiteSpace(purchase.RepName) ? purchase.Stocks.FirstOrDefault()?.RepName : purchase.RepName;
        amountPaid.Text = purchase.AmountPaid?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        if (purchase.Stocks.Count > 0)
        {
            lineContainer.Children.Clear();
            _stockEditors.Clear();
            foreach (var row in purchase.Stocks)
            {
                var editor = AddStockLine(lineContainer);
                editor.Product.Text = row.ProductName;
                editor.CartonQty.Text = row.CartonQty?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.PacksPerCarton.Text = row.PacksPerCarton?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.PackQty.Text = row.PackQty?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.PiecesPerPack.Text = row.PcsCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.Pieces.Text = row.PcsQty?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.Cost.Text = row.CostPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.PiecesPrice.Text = row.PcsSalesPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.PackPrice.Text = row.PackSalesPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.CartonPrice.Text = row.CartonSalesPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.Expiry.Text = row.Expiry?.ToLocalTime().ToString("yyyy-MM-dd") ?? string.Empty;
            }
        }
    }

    private StockLineEditor AddStockLine(VerticalStackLayout container)
    {
        var editor = new StockLineEditor
        {
            Product = new Entry { Placeholder = "Product name" },
            CartonQty = NumberEntry("Cartons"),
            PacksPerCarton = NumberEntry("Packs / carton"),
            PackQty = NumberEntry("Packs"),
            PiecesPerPack = NumberEntry("Pieces / pack"),
            Pieces = NumberEntry("Loose pieces"),
            Cost = NumberEntry("Purchase cost"),
            PiecesPrice = NumberEntry("Retail price / pc"),
            PackPrice = NumberEntry("Retail price / pack"),
            CartonPrice = NumberEntry("Retail price / carton"),
            Expiry = new Entry { Placeholder = "Expiry date (YYYY-MM-DD)" }
        };
        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            RowDefinitions =
            [
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 8,
            RowSpacing = 5,
            Padding = 10
        };
        grid.Add(editor.Product);
        grid.Add(editor.CartonQty, 0, 1);
        grid.Add(editor.PacksPerCarton, 1, 1);
        grid.Add(editor.PackQty, 0, 2);
        grid.Add(editor.PiecesPerPack, 1, 2);
        grid.Add(editor.Pieces, 0, 3);
        grid.Add(editor.Cost, 1, 3);
        grid.Add(editor.PiecesPrice, 0, 4);
        grid.Add(editor.PackPrice, 1, 4);
        grid.Add(editor.CartonPrice, 0, 5);
        grid.Add(editor.Expiry, 1, 5);
        grid.Add(CreateActionButton("Remove product", () =>
        {
            container.Children.Remove(grid);
            _stockEditors.Remove(editor);
        }), 1, 6);
        grid.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#1F2937")
            : Color.FromArgb("#F3F4F6");
        container.Children.Add(grid);
        _stockEditors.Add(editor);
        return editor;
    }

    private async Task SaveStockAsync(string? company, string? representative, string? amountPaid)
    {
        var rows = _stockEditors
            .Select((editor, index) =>
            {
                var cartons = ParseNumber(editor.CartonQty.Text);
                var packsPerCarton = ParseNumber(editor.PacksPerCarton.Text);
                var packs = ParseNumber(editor.PackQty.Text);
                var piecesPerPack = ParseNumber(editor.PiecesPerPack.Text);
                var pieces = ParseNumber(editor.Pieces.Text);
                return new
                {
                    sn = index + 1,
                    productName = editor.Product.Text?.Trim() ?? string.Empty,
                    carton = cartons > 0,
                    cartonQty = cartons,
                    packsPerCarton,
                    pack = packs > 0,
                    packQty = packs,
                    pcsCount = piecesPerPack,
                    pcsQty = pieces,
                    totalPcs = cartons * Math.Max(1, packsPerCarton) * Math.Max(1, piecesPerPack)
                        + packs * Math.Max(1, piecesPerPack) + pieces,
                    costPrice = ParseNumber(editor.Cost.Text),
                    pcsSalesPrice = ParseNumber(editor.PiecesPrice.Text),
                    packSalesPrice = ParseNumber(editor.PackPrice.Text),
                    cartonSalesPrice = ParseNumber(editor.CartonPrice.Text),
                    wholesale = false,
                    expiry = editor.Expiry.Text?.Trim() ?? string.Empty
                };
            })
            .Where(row => row.productName.Length > 0 && row.totalPcs > 0)
            .ToList();
        if (rows.Count == 0)
        {
            AddMessage("Enter at least one product and a quantity greater than zero.", true);
            return;
        }
        if (rows.Any(row => row.costPrice < 0))
        {
            AddMessage("Purchase cost cannot be negative.", true);
            return;
        }

        try
        {
            var request = new
            {
                date = _rangeTo.HasValue
                    ? new Dictionary<string, string>
                    {
                        ["from"] = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["to"] = _rangeTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    }
                    : new Dictionary<string, string>
                    {
                        ["date"] = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    },
                companyName = company?.Trim() ?? string.Empty,
                repName = representative?.Trim() ?? string.Empty,
                amountPaid = ParseNumber(amountPaid),
                rows
            };
            var path = _editId is null ? "api/inventory/stock" : $"api/inventory/stock/{Uri.EscapeDataString(_editId)}";
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, _editId is null ? HttpMethod.Post : HttpMethod.Put, path, request);
            var destination = _rangeTo.HasValue
                ? $"/stock/{_selectedDate:yyyy-MM-dd}_to_{_rangeTo.Value:yyyy-MM-dd}"
                : $"/stock/{_selectedDate:yyyy-MM-dd}";
            await PageLayout.NavigateToAsync(destination);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddMessage(exception.Message, true);
        }
    }

    private async Task RenderHistoryAsync()
    {
        var from = string.Empty;
        var to = string.Empty;
        if (!string.Equals(_path, "all", StringComparison.OrdinalIgnoreCase))
        {
            var range = _path.Split("_to_", StringSplitOptions.None);
            from = range[0];
            to = range.Length > 1 ? range[1] : from;
        }

        var dates = CreateDateRangeInputs(from, to);
        Workspace.Children.Add(dates.View);
        Workspace.Children.Add(CreateButton("Apply date range", async () =>
        {
            var start = dates.From.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = dates.To.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            await PageLayout.NavigateToAsync($"/stock/{start}_to_{end}");
        }));

        var query = string.IsNullOrWhiteSpace(from) ? string.Empty : $"&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        AddMessage("Loading stock purchases...");
        var stocks = await InventoryApi.GetAsync<List<InventoryStockRecord>>(PageLayout.ApiBaseUrl, $"api/inventory/stock?history=true{query}");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        Workspace.Children.Add(CreateLabel($"{stocks.Count} purchases · Total {InventoryApi.MoneyText(stocks.Sum(item => item.Total ?? 0))}", 16, true));
        if (stocks.Count == 0)
        {
            AddMessage("No stock purchases found for this date range.");
            return;
        }
        foreach (var stock in stocks)
        {
            var card = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CreateLabel(InventoryApi.DateText(stock.Date ?? stock.RangeFrom), 16, true),
                    CreateLabel($"Supplier: {EmptyAsDash(stock.CompanyName)} · Rep: {EmptyAsDash(stock.RepName)}"),
                    CreateLabel($"Total: {InventoryApi.MoneyText(stock.Total)} · Paid: {InventoryApi.MoneyText(stock.AmountPaid)} · Balance: {InventoryApi.MoneyText(stock.Balance)}")
                }
            };
            Workspace.Children.Add(CreateCard(card));
            Workspace.Children.Add(CreateButton("View purchase", async () => await PageLayout.NavigateToAsync($"/stock/purchase/{Uri.EscapeDataString(stock.Id)}")));
        }
    }

    private async Task RenderPurchaseAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            AddMessage("A stock purchase ID is required.", true);
            return;
        }
        var purchase = await InventoryApi.GetAsync<InventoryStockRecord>(PageLayout.ApiBaseUrl, $"api/inventory/stock/{Uri.EscapeDataString(id)}");
        Workspace.Children.Add(CreateCard(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                CreateLabel($"Purchase date: {InventoryApi.DateText(purchase.Date)}"),
                CreateLabel($"Supplier: {EmptyAsDash(purchase.CompanyName.Length == 0 ? purchase.Stocks.FirstOrDefault()?.CompanyName : purchase.CompanyName)} · Rep: {EmptyAsDash(purchase.RepName.Length == 0 ? purchase.Stocks.FirstOrDefault()?.RepName : purchase.RepName)}"),
                CreateLabel($"Total: {InventoryApi.MoneyText(purchase.Total)} · Paid: {InventoryApi.MoneyText(purchase.AmountPaid)}"),
                CreateLabel($"Balance: {InventoryApi.MoneyText((purchase.Total ?? 0) - (purchase.AmountPaid ?? 0))}", 16, true)
            }
        }));
        foreach (var row in purchase.Stocks)
        {
            Workspace.Children.Add(CreateCard(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CreateLabel(row.ProductName, 16, true),
                    CreateLabel($"{row.TotalPcs.GetValueOrDefault():N0} pcs · Cost {InventoryApi.MoneyText(row.CostPrice)}"),
                    CreateLabel($"Retail: pc {InventoryApi.MoneyText(row.PcsSalesPrice)} · pack {InventoryApi.MoneyText(row.PackSalesPrice)} · carton {InventoryApi.MoneyText(row.CartonSalesPrice)}"),
                    CreateLabel($"Expiry: {InventoryApi.DateText(row.Expiry)}")
                }
            }));
        }
        if (string.Equals(CommerceSessionState.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            Workspace.Children.Add(CreateButton("Edit purchase", async () => await PageLayout.NavigateToAsync($"/stock?edit={Uri.EscapeDataString(purchase.Id)}")));
    }

    private async Task RenderProductsAsync()
    {
        var search = new Entry { Placeholder = "Search stock products" };
        Workspace.Children.Add(search);
        AddMessage("Loading product availability...");
        var products = await InventoryApi.GetAsync<List<InventoryProductAvailability>>(PageLayout.ApiBaseUrl, "api/inventory/products/availability");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        var resultView = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(resultView);
        void UpdateProducts()
        {
            resultView.Children.Clear();
            var query = search.Text?.Trim() ?? string.Empty;
            var filtered = products.Where(item => item.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(150).ToList();
            if (filtered.Count == 0)
            {
                resultView.Children.Add(CreateLabel("No products match this search."));
                return;
            }
            foreach (var product in filtered)
            {
                resultView.Children.Add(CreateCard(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        CreateLabel(product.ProductName, 16, true),
                        CreateLabel($"Available: {(product.AvailablePieces ?? product.AvailablePcs ?? 0):N0} pcs"),
                        CreateLabel($"Expiry: {InventoryApi.DateText(product.ShortestExpiry ?? product.Expiry)}")
                    }
                }));
            }
        }
        search.TextChanged += (_, _) => UpdateProducts();
        UpdateProducts();
    }

    private async Task RenderCountAsync()
    {
        var isAdmin = string.Equals(CommerceSessionState.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin)
            AddMessage("Only administrators can save stock counts. You can still review the product quantities below.");

        var date = new DatePicker { Date = _selectedDate };
        date.DateSelected += async (_, args) =>
        {
            _selectedDate = args.NewDate.Date;
            await PageLayout.NavigateToAsync($"/stock/count?date={_selectedDate:yyyy-MM-dd}");
        };
        Workspace.Children.Add(date);
        _countSearch = new Entry { Placeholder = "Filter products" };
        _countRows = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(_countSearch);
        Workspace.Children.Add(_countRows);
        AddMessage("Loading products to count...");
        var response = await InventoryApi.GetAsync<InventoryCountProductsResponse>(
            PageLayout.ApiBaseUrl,
            $"api/inventory/stock-counts?mode=products&date={_selectedDate:yyyy-MM-dd}");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        _countProducts = response.Products;
        _countEntries.Clear();
        _countSearch.TextChanged += (_, _) => RenderCountRows();
        RenderCountRows();
        if (isAdmin)
            Workspace.Children.Add(CreateButton("Save stock count", SaveCountAsync));
    }

    private void RenderCountRows()
    {
        if (_countRows is null)
            return;
        _countRows.Children.Clear();
        var query = _countSearch?.Text?.Trim() ?? string.Empty;
        var visible = _countProducts
            .Where(product => product.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(150);
        foreach (var product in visible)
        {
            var key = string.IsNullOrWhiteSpace(product.ProductKey) ? product.ProductName : product.ProductKey;
            if (!_countEntries.TryGetValue(key, out var counted))
            {
                counted = NumberEntry("Counted pieces");
                var savedCount = product.SavedCountedPcs ?? product.CountedPcs;
                if (savedCount.HasValue)
                    counted.Text = savedCount.Value.ToString(CultureInfo.InvariantCulture);
                _countEntries[key] = counted;
            }
            _countRows.Children.Add(CreateCard(new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    CreateLabel(product.ProductName, 16, true),
                    CreateLabel($"Expected: {product.ExpectedPcs:N0} pcs · Shelf: {EmptyAsDash(product.ShelfName)}"),
                    counted
                }
            }));
        }
        if (_countRows.Children.Count == 0)
            _countRows.Children.Add(CreateLabel("No products match this search."));
    }

    private async Task SaveCountAsync()
    {
        if (!string.Equals(CommerceSessionState.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            AddMessage("Only administrators can save stock counts.", true);
            return;
        }
        var rows = _countProducts
            .Select(product =>
            {
                var key = string.IsNullOrWhiteSpace(product.ProductKey) ? product.ProductName : product.ProductKey;
                _countEntries.TryGetValue(key, out var entry);
                return new
                {
                    productName = product.ProductName,
                    productKey = key,
                    expectedPcs = product.ExpectedPcs,
                    countedPcs = ParseNullableNumber(entry?.Text)
                };
            })
            .Where(row => row.countedPcs.HasValue)
            .ToList();
        if (rows.Count == 0)
        {
            AddMessage("Enter at least one counted quantity before saving.", true);
            return;
        }
        try
        {
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, HttpMethod.Post, "api/inventory/stock-counts", new
            {
                date = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                rows
            });
            await PageLayout.NavigateToAsync($"/stock/count/{_selectedDate:yyyy-MM-dd}");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddMessage(exception.Message, true);
        }
    }

    private async Task RenderCountHistoryAsync(string token)
    {
        var (from, to) = GetRange(token);
        var query = string.IsNullOrWhiteSpace(from) ? string.Empty : $"?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        AddMessage("Loading count history...");
        var counts = await InventoryApi.GetAsync<List<InventoryCountRecord>>(PageLayout.ApiBaseUrl, $"api/inventory/stock-counts{query}");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        Workspace.Children.Add(CreateLabel($"{counts.Count} saved counts", 16, true));
        if (counts.Count == 0)
        {
            AddMessage("No stock counts found for this date range.");
            return;
        }
        var detailArea = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(detailArea);
        foreach (var count in counts)
        {
            var card = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CreateLabel(InventoryApi.DateText(count.Date), 16, true),
                    CreateLabel($"Shelf: {EmptyAsDash(count.Shelf?.Name)} · Products: {count.TotalLines} · Counted: {count.CountedLines}")
                }
            };
            Workspace.Children.Add(CreateCard(card));
            Workspace.Children.Add(CreateActionButton("View count", () =>
            {
                detailArea.Children.Clear();
                detailArea.Children.Add(CreateLabel($"Count details · {InventoryApi.DateText(count.Date)}", 18, true));
                foreach (var line in count.Lines)
                    detailArea.Children.Add(CreateCard(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            CreateLabel(line.ProductName, 15, true),
                            CreateLabel($"Expected: {line.ExpectedPcs:N0} · Counted: {line.CountedPcs?.ToString("N0") ?? "—"} · Difference: {(line.DifferencePcs ?? line.Difference)?.ToString("N0") ?? "—"}")
                        }
                    }));
            }));
        }
    }

    private void AddHeading(string title, string subtitle)
    {
        Workspace.Children.Add(CreateLabel(title, 28, true));
        Workspace.Children.Add(CreateLabel(subtitle));
    }

    private void AddMessage(string text, bool isError = false) =>
        Workspace.Children.Add(CreateLabel(text, 14, false, isError));

    private static Label CreateLabel(string text, double size = 14, bool bold = false, bool isError = false) =>
        new()
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = isError
                ? (Color)Application.Current!.Resources[Application.Current.RequestedTheme == AppTheme.Dark ? "ThemeDestructiveDark" : "ThemeDestructiveLight"]
                : (Color)Application.Current!.Resources[Application.Current.RequestedTheme == AppTheme.Dark ? "ThemeForegroundDark" : "ThemeForegroundLight"]
        };

    private static Entry NumberEntry(string placeholder) =>
        new() { Placeholder = placeholder, Keyboard = Keyboard.Numeric };

    private static Button CreateButton(string text, Func<Task> onClick)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }

    private static Button CreateActionButton(string text, Action onClick)
    {
        var button = new Button { Text = text };
        button.Clicked += (_, _) => onClick();
        return button;
    }

    private static Border CreateCard(View content) =>
        new()
        {
            Content = content,
            Padding = 12,
            Stroke = Color.FromArgb("#D1D5DB"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
        };

    private static (View View, DatePicker From, DatePicker To) CreateDateRangeInputs(string from, string to)
    {
        var fromPicker = new DatePicker { Date = ParseDate(from) ?? DateTime.Today.AddDays(-7) };
        var toPicker = new DatePicker { Date = ParseDate(to) ?? DateTime.Today };
        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 8
        };
        grid.Add(fromPicker);
        grid.Add(toPicker, 1);
        return (grid, fromPicker, toPicker);
    }

    private static bool IsRangePath(string path) =>
        path.Equals("all", StringComparison.OrdinalIgnoreCase)
        || DateTime.TryParse(path.Split("_to_", StringSplitOptions.None)[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static (string From, string To) GetRange(string token)
    {
        if (token.Equals("all", StringComparison.OrdinalIgnoreCase))
            return (string.Empty, string.Empty);
        var parts = token.Split("_to_", StringSplitOptions.None);
        return (parts[0], parts.Length > 1 ? parts[1] : parts[0]);
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : null;

    private static double ParseNumber(string? value) =>
        double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static double? ParseNullableNumber(string? value) =>
        double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

    private static string EmptyAsDash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string? GetQueryValue(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private sealed class StockLineEditor
    {
        public Entry Product { get; init; } = new();
        public Entry CartonQty { get; init; } = new();
        public Entry PacksPerCarton { get; init; } = new();
        public Entry PackQty { get; init; } = new();
        public Entry PiecesPerPack { get; init; } = new();
        public Entry Pieces { get; init; } = new();
        public Entry Cost { get; init; } = new();
        public Entry PiecesPrice { get; init; } = new();
        public Entry PackPrice { get; init; } = new();
        public Entry CartonPrice { get; init; } = new();
        public Entry Expiry { get; init; } = new();
    }
}
