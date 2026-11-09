using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class SalesPage : ContentPage, IQueryAttributable
{
    private readonly List<SaleLineEditor> _lineEditors = [];
    private readonly Dictionary<string, InventorySalePricing> _stockPricing = new(StringComparer.OrdinalIgnoreCase);
    private string _path = string.Empty;
    private string? _editId;
    private string? _saleId;
    private string? _queryFrom;
    private string? _queryTo;
    private DateTime _selectedDate = DateTime.Today;
    private DateTime? _rangeTo;
    public SalesPage()
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
        _saleId = GetQueryValue(query, "id");
        _queryFrom = GetQueryValue(query, "from");
        _queryTo = GetQueryValue(query, "to");
        if (DateTime.TryParse(GetQueryValue(query, "date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            _selectedDate = date.Date;
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        try
        {
            Workspace.Children.Clear();
            AddHeading("Sales", "Record sales and review saved transactions.");
            AddNavigation();
            AddMessage("Checking inventory access...");

            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (user is null
                || (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(user.Role, "staff", StringComparison.OrdinalIgnoreCase)))
            {
                Workspace.Children.Clear();
                AddHeading("Sales", "Sales is available to signed-in staff and administrators.");
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }
            if (!string.IsNullOrWhiteSpace(_editId) && !string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                Workspace.Children.Clear();
                AddHeading("Sales", "Only administrators can edit saved sales.");
                AddNavigation();
                return;
            }

            Workspace.Children.Clear();
            AddHeading(GetPageTitle(), "Connected to the HealthClique inventory service.");
            AddNavigation();
            if (string.Equals(_path, "daily", StringComparison.OrdinalIgnoreCase))
                await RenderHistoryAsync("daily");
            else if (string.Equals(_path, "unstocked", StringComparison.OrdinalIgnoreCase))
                await RenderUnstockedAsync();
            else if (_path.StartsWith("view/", StringComparison.OrdinalIgnoreCase))
                await RenderSaleDetailAsync(_path["view/".Length..]);
            else if (!string.IsNullOrWhiteSpace(_saleId))
                await RenderSaleDetailAsync(_saleId);
            else if (IsRangePath(_path))
                await RenderHistoryAsync("range");
            else
                await RenderSaleFormAsync(_editId);
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
        _path.Equals("unstocked", StringComparison.OrdinalIgnoreCase) ? "Unstocked sales"
        : _path.Equals("daily", StringComparison.OrdinalIgnoreCase) ? "Daily sales"
        : _path.StartsWith("view/", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(_saleId) ? "Sale details"
        : IsRangePath(_path) ? "Sales history"
        : string.IsNullOrWhiteSpace(_editId) ? "Record a sale" : "Edit sale";

    private void AddNavigation()
    {
        var links = new HorizontalStackLayout { Spacing = 8 };
        links.Children.Add(CreateButton("New sale", async () => await PageLayout.NavigateToAsync("/sales")));
        links.Children.Add(CreateButton("Today", async () => await PageLayout.NavigateToAsync($"/sales/daily?date={DateTime.Today:yyyy-MM-dd}")));
        links.Children.Add(CreateButton("All sales", async () => await PageLayout.NavigateToAsync("/sales/all")));
        links.Children.Add(CreateButton("Unstocked", async () => await PageLayout.NavigateToAsync("/sales/unstocked")));
        Workspace.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = links, HeightRequest = 48 });
    }

    private async Task RenderSaleFormAsync(string? editId)
    {
        try
        {
            var catalog = await InventoryApi.GetAsync<InventorySalesCatalog>(
                PageLayout.ApiBaseUrl,
                "api/inventory/sales/catalog");
            _stockPricing.Clear();
            foreach (var (name, pricing) in catalog.StockPricing)
                _stockPricing[name.Trim()] = pricing;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddMessage($"Product pricing could not be loaded. Enter prices manually. {exception.Message}", true);
        }

        Workspace.Children.Add(CreateLabel("Sale date"));
        var datePicker = new DatePicker { Date = _selectedDate, MaximumDate = DateTime.Today.AddYears(2) };
        datePicker.DateSelected += (_, args) =>
        {
            _selectedDate = args.NewDate.Date;
            _rangeTo = null;
        };
        Workspace.Children.Add(datePicker);

        var customer = new Entry { Placeholder = "Customer name (optional)" };
        var staff = new Entry
        {
            Placeholder = $"Staff: {CommerceSessionState.CurrentUser?.Name ?? "Signed-in user"}",
            IsReadOnly = true
        };
        Workspace.Children.Add(customer);
        Workspace.Children.Add(staff);

        var payment = new Picker { Title = "Payment method", ItemsSource = new[] { "cash", "pos", "cash&pos" }, SelectedIndex = 0 };
        var cashPaid = NumberEntry("Cash paid");
        var posPaid = NumberEntry("POS / transfer paid");
        var change = NumberEntry("Change");
        Workspace.Children.Add(payment);
        Workspace.Children.Add(cashPaid);
        Workspace.Children.Add(posPaid);
        Workspace.Children.Add(change);

        var lineContainer = new VerticalStackLayout { Spacing = 8 };
        var linesHeading = CreateLabel("Products", 19, true);
        Workspace.Children.Add(linesHeading);
        Workspace.Children.Add(lineContainer);
        var buttons = new HorizontalStackLayout { Spacing = 8 };
        buttons.Children.Add(CreateActionButton("Add product", () => AddSaleLine(lineContainer)));
        buttons.Children.Add(CreateButton(editId is null ? "Save sale" : "Save changes", async () =>
            await SaveSaleAsync(editId, customer.Text, payment.SelectedItem?.ToString() ?? string.Empty, cashPaid.Text, posPaid.Text, change.Text)));
        Workspace.Children.Add(buttons);

        AddSaleLine(lineContainer);
        if (editId is null)
            return;

        AddMessage("Loading saved sale...");
        var record = await InventoryApi.GetAsync<InventorySaleRecord>(PageLayout.ApiBaseUrl, $"api/inventory/sales/{Uri.EscapeDataString(editId)}");
        _selectedDate = record.Date?.ToLocalTime().Date ?? record.RangeFrom?.ToLocalTime().Date ?? _selectedDate;
        _rangeTo = record.Date is null ? record.RangeTo?.ToLocalTime().Date : null;
        datePicker.Date = _selectedDate;
        datePicker.IsEnabled = !_rangeTo.HasValue;
        var savedRows = record.Sales;
        if (savedRows.Count > 0)
        {
            lineContainer.Children.Clear();
            _lineEditors.Clear();
            foreach (var row in savedRows)
            {
                var editor = AddSaleLine(lineContainer);
                editor.Product.Text = row.ProductName;
                editor.Quantity.Text = (row.TotalPcs ?? row.Qty)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                editor.Price.Text = (row.SalesPrice ?? row.Price)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            }
            customer.Text = savedRows.FirstOrDefault()?.CustomerName ?? record.CustomerName;
        }
        payment.SelectedItem = string.IsNullOrWhiteSpace(record.PaymentMethod) ? "cash" : record.PaymentMethod;
        cashPaid.Text = record.CashPaid?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        posPaid.Text = record.PosPayment?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        change.Text = record.Change?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private SaleLineEditor AddSaleLine(VerticalStackLayout container)
    {
        var editor = new SaleLineEditor
        {
            Product = new Entry { Placeholder = "Product name", ReturnType = ReturnType.Next },
            Quantity = NumberEntry("Pcs quantity"),
            Price = NumberEntry("Price per piece")
        };
        editor.Product.Unfocused += (_, _) =>
        {
            var productName = editor.Product.Text?.Trim() ?? string.Empty;
            if (editor.Price.Text is null || editor.Price.Text.Trim().Length == 0)
            {
                if (_stockPricing.TryGetValue(productName, out var pricing)
                    && pricing.PcsSalesPrice is double price)
                    editor.Price.Text = price.ToString(CultureInfo.InvariantCulture);
            }
        };
        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)],
            ColumnSpacing = 8,
            RowSpacing = 6,
            Padding = 10
        };
        grid.Add(editor.Product);
        Grid.SetColumnSpan(editor.Product, 2);
        grid.Add(editor.Quantity, 0, 1);
        grid.Add(editor.Price, 1, 1);
        grid.Add(CreateActionButton("Remove product", () =>
        {
            container.Children.Remove(grid);
            _lineEditors.Remove(editor);
        }), 1, 2);
        grid.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#1F2937")
            : Color.FromArgb("#F3F4F6");
        container.Children.Add(grid);
        _lineEditors.Add(editor);
        return editor;
    }

    private async Task SaveSaleAsync(string? editId, string? customerName, string payment, string? cashText, string? posText, string? changeText)
    {
        var rows = _lineEditors
            .Select((editor, index) => new
            {
                sn = index + 1,
                productName = editor.Product.Text?.Trim() ?? string.Empty,
                pcsQty = ParseNumber(editor.Quantity.Text),
                totalPcs = ParseNumber(editor.Quantity.Text),
                salesPrice = ParseNumber(editor.Price.Text),
                total = ParseNumber(editor.Quantity.Text) * ParseNumber(editor.Price.Text)
            })
            .Where(row => row.productName.Length > 0 && row.pcsQty > 0)
            .ToList();
        if (rows.Count == 0)
        {
            AddMessage("Enter at least one product and a quantity greater than zero.", true);
            return;
        }
        if (rows.Any(row => row.salesPrice < 0))
        {
            AddMessage("Sales price cannot be negative.", true);
            return;
        }

        try
        {
            var section = new
            {
                customerName = customerName?.Trim() ?? string.Empty,
                paymentMethod = payment,
                cashPaid = ParseNumber(cashText),
                posPayment = ParseNumber(posText),
                change = ParseNumber(changeText),
                rows
            };
            var date = _rangeTo.HasValue
                ? new Dictionary<string, string>
                {
                    ["from"] = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["to"] = _rangeTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                }
                : new Dictionary<string, string>
                {
                    ["date"] = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                };
            var request = new
            {
                date,
                sections = new[] { section }
            };
            var path = editId is null ? "api/inventory/sales" : $"api/inventory/sales/{Uri.EscapeDataString(editId)}";
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, editId is null ? HttpMethod.Post : HttpMethod.Put, path, request);
            var destination = _rangeTo.HasValue
                ? $"/sales/{_selectedDate:yyyy-MM-dd}_to_{_rangeTo.Value:yyyy-MM-dd}"
                : $"/sales/daily?date={_selectedDate:yyyy-MM-dd}";
            await PageLayout.NavigateToAsync(destination);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddMessage(exception.Message, true);
        }
    }

    private async Task RenderHistoryAsync(string mode)
    {
        var from = string.Empty;
        var to = string.Empty;
        if (mode == "daily")
        {
            if (!string.IsNullOrWhiteSpace(_queryFrom) && !string.IsNullOrWhiteSpace(_queryTo))
            {
                from = _queryFrom;
                to = _queryTo;
                var rangeInputs = CreateDateRangeInputs(from, to);
                Workspace.Children.Add(rangeInputs.View);
                Workspace.Children.Add(CreateButton("Apply date range", async () =>
                    await PageLayout.NavigateToAsync($"/sales/daily?from={rangeInputs.From.Date:yyyy-MM-dd}&to={rangeInputs.To.Date:yyyy-MM-dd}")));
            }
            else
            {
                from = _selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                to = from;
                var datePicker = new DatePicker { Date = _selectedDate };
                datePicker.DateSelected += async (_, args) =>
                {
                    _selectedDate = args.NewDate.Date;
                    await PageLayout.NavigateToAsync($"/sales/daily?date={_selectedDate:yyyy-MM-dd}");
                };
                Workspace.Children.Add(datePicker);
            }
        }
        else if (_path != "all")
        {
            var dateParts = _path.Split("_to_", StringSplitOptions.None);
            from = dateParts[0];
            to = dateParts.Length > 1 ? dateParts[1] : from;
        }

        if (mode == "range")
        {
            var rangeInputs = CreateDateRangeInputs(from, to);
            Workspace.Children.Add(rangeInputs.View);
            Workspace.Children.Add(CreateButton("Apply date range", async () =>
            {
                var start = rangeInputs.From.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var end = rangeInputs.To.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                await PageLayout.NavigateToAsync($"/sales/{start}_to_{end}");
            }));
        }

        var query = string.IsNullOrWhiteSpace(from) ? string.Empty : $"?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        AddMessage("Loading sales...");
        var sales = await InventoryApi.GetAsync<List<InventorySaleRecord>>(PageLayout.ApiBaseUrl, $"api/inventory/sales{query}");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        var total = sales.Sum(sale => sale.Total ?? sale.Rows.Sum(row => row.Amount ?? row.Total ?? 0));
        Workspace.Children.Add(CreateLabel($"{sales.Count} transactions · Total {InventoryApi.MoneyText(total)}", 16, true));
        if (sales.Count == 0)
        {
            AddMessage("No sales found for this date range.");
            return;
        }

        foreach (var sale in sales)
        {
            var detail = new VerticalStackLayout { Spacing = 4 };
            detail.Children.Add(CreateLabel(sale.CustomerName.Length == 0 ? "Sale" : sale.CustomerName, 17, true));
            detail.Children.Add(CreateLabel($"{InventoryApi.DateText(sale.Date ?? sale.RangeFrom)} · {sale.Rows.Count} products · {InventoryApi.MoneyText(sale.Total)}"));
            detail.Children.Add(CreateLabel($"Payment: {string.IsNullOrWhiteSpace(sale.PaymentMethod) ? "—" : sale.PaymentMethod} · Staff: {string.IsNullOrWhiteSpace(sale.StaffName) ? "—" : sale.StaffName}"));
            Workspace.Children.Add(CreateCard(detail));
            Workspace.Children.Add(CreateButton("View sale", async () => await PageLayout.NavigateToAsync($"/sales/view/{Uri.EscapeDataString(sale.Id)}")));
        }
    }

    private async Task RenderSaleDetailAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            AddMessage("A sale ID is required.", true);
            return;
        }
        var sale = await InventoryApi.GetAsync<InventorySaleRecord>(PageLayout.ApiBaseUrl, $"api/inventory/sales/{Uri.EscapeDataString(id)}");
        var date = sale.Date ?? sale.RangeFrom;
        Workspace.Children.Add(CreateCard(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                CreateLabel($"Date: {InventoryApi.DateText(date)}"),
                CreateLabel($"Customer: {(string.IsNullOrWhiteSpace(sale.CustomerName) ? sale.Sales.FirstOrDefault()?.CustomerName : sale.CustomerName)}"),
                CreateLabel($"Staff: {sale.StaffName}"),
                CreateLabel($"Payment: {sale.PaymentMethod}"),
                CreateLabel($"Cash: {InventoryApi.MoneyText(sale.CashPaid)} · POS: {InventoryApi.MoneyText(sale.PosPayment)}"),
                CreateLabel($"Change: {InventoryApi.MoneyText(sale.Change)} · Total: {InventoryApi.MoneyText(sale.Total)}", 16, true)
            }
        }));
        foreach (var row in sale.Sales)
            Workspace.Children.Add(CreateCard(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CreateLabel(row.ProductName, 16, true),
                    CreateLabel($"{row.TotalPcs ?? row.Qty ?? 0:N0} pcs × {InventoryApi.MoneyText(row.SalesPrice ?? row.Price)} = {InventoryApi.MoneyText(row.Total)}")
                }
            }));
        if (string.Equals(CommerceSessionState.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            Workspace.Children.Add(CreateButton("Edit sale", async () => await PageLayout.NavigateToAsync($"/sales?edit={Uri.EscapeDataString(sale.Id)}")));
    }

    private async Task RenderUnstockedAsync()
    {
        AddMessage("Loading unstocked sales...");
        var report = await InventoryApi.GetAsync<InventoryUnstockedReport>(PageLayout.ApiBaseUrl, "api/inventory/products/unstocked-sales");
        Workspace.Children.RemoveAt(Workspace.Children.Count - 1);
        Workspace.Children.Add(CreateLabel($"{report.Rows.Count} unstocked sale rows · {report.LossRows.Count} loss rows", 16, true));
        foreach (var row in report.Rows)
        {
            Workspace.Children.Add(CreateCard(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CreateLabel(row.ProductName, 16, true),
                    CreateLabel($"{InventoryApi.DateText(row.Date)} · Qty {row.Quantity.GetValueOrDefault():N0} · {InventoryApi.MoneyText(row.Amount)}")
                }
            }));
        }
        if (report.Rows.Count == 0)
            AddMessage("No unstocked sales were found.");
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
        var start = ParseDate(from) ?? DateTime.Today.AddDays(-7);
        var end = ParseDate(to) ?? DateTime.Today;
        var fromPicker = new DatePicker { Date = start };
        var toPicker = new DatePicker { Date = end };
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

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : null;

    private static double ParseNumber(string? value) =>
        double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static string? GetQueryValue(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private sealed class SaleLineEditor
    {
        public Entry Product { get; init; } = new();
        public Entry Quantity { get; init; } = new();
        public Entry Price { get; init; } = new();
    }
}
