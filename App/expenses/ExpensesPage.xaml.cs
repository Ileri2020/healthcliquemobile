using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class ExpensesPage : ContentPage, IQueryAttributable
{
    private readonly List<ExpenseDraft> _drafts = [];
    private string _path = string.Empty;
    private string? _from;
    private string? _to;
    private DateTime _date = DateTime.Today;
    private Label? _draftTotal;

    public ExpensesPage()
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
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddHeading(IsHistory ? "Expense history" : "Expense ledger",
            IsHistory ? "Saved expense records from the HealthClique service." : "Capture one-off costs and expenses shared over a time frame.");
        AddNavigation();

        try
        {
            if (IsHistory)
            {
                AddRangeFilter();
                var expenses = await LoadExpensesAsync();
                AddExpenseList(expenses);
                return;
            }

            AddExpenseForm();
            var saved = await LoadExpensesAsync();
            AddLabel($"Saved expenses · {saved.Count} record(s)", 20, true);
            AddExpenseList(saved);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Expense service unavailable: {exception.Message}", 14, false, true);
        }
    }

    private bool IsHistory => !string.IsNullOrWhiteSpace(_path);

    private void AddExpenseForm()
    {
        AddLabel("Expense date", 16, true);
        var datePicker = new DatePicker { Date = _date, MaximumDate = DateTime.Today.AddYears(2) };
        datePicker.DateSelected += (_, args) => _date = args.NewDate.Date;
        Workspace.Children.Add(datePicker);

        _draftTotal = AddLabel("Draft total: ₦0", 18, true);
        var rows = new VerticalStackLayout { Spacing = 8 };
        Workspace.Children.Add(rows);
        _drafts.Clear();
        AddDraft(rows);
        AddDraft(rows);
        AddDraft(rows);

        Workspace.Children.Add(CreateButton("Add expense row", () => AddDraft(rows), outline: true));
        Workspace.Children.Add(CreateButton("Save expenses", async () => await SaveAsync(), outline: false));
    }

    private void AddDraft(VerticalStackLayout container)
    {
        var draft = new ExpenseDraft();
        var panel = new VerticalStackLayout { Spacing = 6, Padding = 10 };
        var name = new Entry { Placeholder = "Expense name (e.g. Transport)" };
        var amount = new Entry { Placeholder = "Amount (₦)", Keyboard = Keyboard.Numeric };
        var span = new CheckBox();
        var from = new DatePicker { Date = _date };
        var to = new DatePicker { Date = _date };
        var spanDates = new VerticalStackLayout
        {
            Spacing = 4,
            IsVisible = false,
            Children =
            {
                new Label { Text = "Shared expense date range" },
                from,
                to
            }
        };
        span.CheckedChanged += (_, args) => spanDates.IsVisible = args.Value;
        amount.TextChanged += (_, _) => UpdateDraftTotal();
        var rowGrid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            ColumnSpacing = 8
        };
        rowGrid.Add(name);
        var remove = CreateButton("Remove", () =>
        {
            _drafts.Remove(draft);
            container.Children.Remove(panel);
            UpdateDraftTotal();
        }, outline: true);
        rowGrid.Add(remove, 1, 0);
        panel.Children.Add(rowGrid);
        panel.Children.Add(amount);
        panel.Children.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            Children = { span, new Label { Text = "Shared across dates", VerticalTextAlignment = TextAlignment.Center } }
        });
        panel.Children.Add(spanDates);
        panel.BackgroundColor = Color.FromArgb("#F3F4F6");
        panel.StrokeShape = new RoundRectangle { CornerRadius = 10 };
        container.Children.Add(panel);
        draft.Name = name;
        draft.Amount = amount;
        draft.Span = span;
        draft.SpanFrom = from;
        draft.SpanTo = to;
        _drafts.Add(draft);
    }

    private async Task SaveAsync()
    {
        var validDrafts = _drafts.Where(row =>
            !string.IsNullOrWhiteSpace(row.Name.Text) && !string.IsNullOrWhiteSpace(row.Amount.Text)).ToList();
        if (validDrafts.Count == 0)
        {
            await DisplayAlertAsync("Expenses", "Add an expense name and amount first.", "OK");
            return;
        }
        var rows = new List<ExpenseCreateRow>();
        foreach (var draft in validDrafts)
        {
            if (!double.TryParse(draft.Amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                || !double.IsFinite(amount) || amount < 0)
            {
                await DisplayAlertAsync("Expenses", $"Enter a valid non-negative amount for {draft.Name.Text}.", "OK");
                return;
            }
            if (draft.Span.IsChecked && draft.SpanFrom.Date > draft.SpanTo.Date)
            {
                await DisplayAlertAsync("Expenses", $"The date range for {draft.Name.Text} is invalid.", "OK");
                return;
            }
            rows.Add(new ExpenseCreateRow
            {
                Name = draft.Name.Text!.Trim(),
                Amount = amount,
                Span = draft.Span.IsChecked,
                SpanFrom = draft.Span.IsChecked ? draft.SpanFrom.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
                SpanTo = draft.Span.IsChecked ? draft.SpanTo.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null
            });
        }

        try
        {
            await InventoryApi.SendAsync(PageLayout.ApiBaseUrl, HttpMethod.Post, "api/expenses",
                new { date = _date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), rows });
            await DisplayAlertAsync("Expenses", "Expenses saved.", "OK");
            await RenderAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Unable to save expenses", exception.Message, "OK");
        }
    }

    private async Task<List<ExpenseRecord>> LoadExpensesAsync()
    {
        var path = "api/expenses";
        if (IsHistory && _path.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // No date filter means all saved records.
        }
        else if (IsHistory && (!string.IsNullOrWhiteSpace(_from) || !string.IsNullOrWhiteSpace(_to)))
        {
            var from = _from ?? _to!;
            var to = _to ?? _from!;
            path += $"?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        }
        else if (IsHistory)
        {
            var range = _path.Split("_to_", StringSplitOptions.None);
            var from = range[0];
            var to = range.Length > 1 ? range[1] : from;
            path += $"?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        }
        return await InventoryApi.GetAsync<List<ExpenseRecord>>(PageLayout.ApiBaseUrl, path);
    }

    private void AddRangeFilter()
    {
        var range = _path.Split("_to_", StringSplitOptions.None);
        var fallback = DateTime.TryParse(range[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.Date
            : DateTime.Today;
        var fromPicker = new DatePicker
        {
            Date = DateTime.TryParse(_from, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ? from.Date : fallback
        };
        var toPicker = new DatePicker
        {
            Date = DateTime.TryParse(_to, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to)
                ? to.Date
                : range.Length > 1 && DateTime.TryParse(range[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var rangeTo)
                    ? rangeTo.Date
                    : fallback
        };
        AddLabel("Filter saved records", 16, true);
        Workspace.Children.Add(fromPicker);
        Workspace.Children.Add(toPicker);
        Workspace.Children.Add(CreateButton("Apply date range", async () =>
            await PageLayout.NavigateToAsync($"/expenses/all?from={fromPicker.Date:yyyy-MM-dd}&to={toPicker.Date:yyyy-MM-dd}"), outline: true));
    }

    private void AddExpenseList(List<ExpenseRecord> expenses)
    {
        if (expenses.Count == 0)
        {
            AddLabel("No saved expenses found.", 14);
            return;
        }

        foreach (var expense in expenses.OrderByDescending(item => item.Date))
        {
            var names = string.Join(", ", expense.Items.Select(item => $"{item.Name} (₦{item.Amount:N0})"));
            var spans = expense.Items.Where(item => item.Span)
                .Select(item => item.SpanFrom.HasValue && item.SpanTo.HasValue
                    ? $"{item.SpanFrom.Value.ToLocalTime():MMM d}–{item.SpanTo.Value.ToLocalTime():MMM d}"
                    : "Shared").ToList();
            var panel = new VerticalStackLayout
            {
                Padding = 12,
                Spacing = 5,
                Children =
                {
                    new Label { Text = expense.Date.ToLocalTime().ToString("MMM d, yyyy"), FontAttributes = FontAttributes.Bold },
                    new Label { Text = string.IsNullOrWhiteSpace(names) ? "No expense items" : names },
                    new Label { Text = spans.Count > 0 ? $"Shared spans: {string.Join(", ", spans)}" : "One-day expenses" },
                    new Label { Text = $"Total: ₦{expense.Total:N0}", FontAttributes = FontAttributes.Bold }
                }
            };
            Workspace.Children.Add(new Border
            {
                Padding = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = panel
            });
        }
    }

    private void AddNavigation()
    {
        var links = new HorizontalStackLayout { Spacing = 8 };
        links.Children.Add(CreateButton("New expense", async () => await PageLayout.NavigateToAsync("/expenses")));
        links.Children.Add(CreateButton("All expenses", async () => await PageLayout.NavigateToAsync("/expenses/all")));
        links.Children.Add(CreateButton("Today", async () => await PageLayout.NavigateToAsync($"/expenses/{DateTime.Today:yyyy-MM-dd}")));
        Workspace.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = links, HeightRequest = 48 });
    }

    private void UpdateDraftTotal()
    {
        if (_draftTotal is null)
            return;
        var total = _drafts.Sum(row =>
            double.TryParse(row.Amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                ? Math.Max(0, amount)
                : 0);
        _draftTotal.Text = $"Draft total: ₦{total:N0}";
    }

    private void AddHeading(string title, string subtitle)
    {
        AddLabel(title, 28, true);
        AddLabel(subtitle, 14);
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

    private static Button CreateButton(string text, Func<Task> onClick, bool outline)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }

    private sealed class ExpenseDraft
    {
        public Entry Name { get; set; } = new();
        public Entry Amount { get; set; } = new();
        public CheckBox Span { get; set; } = new();
        public DatePicker SpanFrom { get; set; } = new();
        public DatePicker SpanTo { get; set; } = new();
    }
}
