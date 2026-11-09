using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class AnalyticsPage : ContentPage, IQueryAttributable
{
    private bool _inventoryMode;
    private DateTime _from = DateTime.Today.AddDays(-30);
    private DateTime _to = DateTime.Today;
    private DatePicker? _fromPicker;
    private DatePicker? _toPicker;
    private VerticalStackLayout? _results;

    public AnalyticsPage()
    {
        InitializeComponent();
        PageLayout.CurrentPath = "/admin/analytics";
        Loaded += async (_, _) => await RenderAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _inventoryMode = query.TryGetValue("mode", out var value)
            && string.Equals(value?.ToString(), "inventory", StringComparison.OrdinalIgnoreCase);
        PageLayout.CurrentPath = _inventoryMode ? "/inventory/analytics" : "/admin/analytics";
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddLabel(_inventoryMode ? "Inventory analytics" : "Analytics dashboard", 28, true);
        AddLabel(_inventoryMode
            ? "Inventory, sales, purchasing, expense, and profit summary."
            : "Store performance and activity for the selected period.");
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            var isAdmin = string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase);
            var allowed = isAdmin || (_inventoryMode && string.Equals(user?.Role, "staff", StringComparison.OrdinalIgnoreCase));
            if (!allowed)
            {
                AddLabel(_inventoryMode ? "Staff access is required." : "Administrator access is required.", 16, true, true);
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }

            _fromPicker = new DatePicker { Date = _from };
            _toPicker = new DatePicker { Date = _to };
            Workspace.Children.Add(new Label { Text = "From date" });
            Workspace.Children.Add(_fromPicker);
            Workspace.Children.Add(new Label { Text = "To date" });
            Workspace.Children.Add(_toPicker);
            Workspace.Children.Add(CreateButton("Load analytics", LoadAnalyticsAsync));
            _results = new VerticalStackLayout { Spacing = 8 };
            Workspace.Children.Add(_results);
            await LoadAnalyticsAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Analytics could not be loaded: {exception.Message}", error: true);
        }
    }

    private async Task LoadAnalyticsAsync()
    {
        if (_fromPicker is null || _toPicker is null || _results is null)
            return;
        _from = _fromPicker.Date.Date;
        _to = _toPicker.Date.Date;
        if (_from > _to)
        {
            await DisplayAlertAsync("Analytics", "The start date must be on or before the end date.", "OK");
            return;
        }

        _results.Children.Clear();
        AddLabel("Loading analytics...");
        try
        {
            var endpoint = _inventoryMode ? "api/inventory/analytics" : "api/analytics";
            var fromText = _from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toText = _to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var data = await InventoryApi.GetAsync<JsonElement>(
                PageLayout.ApiBaseUrl, $"{endpoint}?from={Uri.EscapeDataString(fromText)}&to={Uri.EscapeDataString(toText)}");
            _results.Children.Clear();
            AddJsonSection(data, _inventoryMode ? "summary" : "kpis");
            AddJsonArrays(data);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            _results.Children.Clear();
            AddLabel($"Analytics could not be loaded: {exception.Message}", error: true);
        }
    }

    private void AddJsonSection(JsonElement data, string key)
    {
        if (!data.TryGetProperty(key, out var summary) || summary.ValueKind != JsonValueKind.Object)
        {
            AddLabel("The analytics service returned no summary data.");
            return;
        }
        AddLabel(key.Equals("kpis", StringComparison.OrdinalIgnoreCase) ? "Key metrics" : "Inventory summary", 20, true);
        foreach (var property in summary.EnumerateObject())
            (_results ?? Workspace).Children.Add(MetricCard(property.Name, FormatValue(property.Value)));
    }

    private void AddJsonArrays(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            return;
        foreach (var property in data.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array || property.Value.GetArrayLength() == 0)
                continue;
            AddLabel($"{Humanize(property.Name)} · {property.Value.GetArrayLength()} records", 18, true);
            foreach (var item in property.Value.EnumerateArray().Take(12))
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var fields = item.EnumerateObject().Take(5)
                        .Select(field => $"{Humanize(field.Name)}: {FormatValue(field.Value)}");
                    (_results ?? Workspace).Children.Add(new Label { Text = string.Join(" · ", fields), FontSize = 13 });
                }
                else
                    (_results ?? Workspace).Children.Add(new Label { Text = FormatValue(item), FontSize = 13 });
            }
        }
    }

    private static Border MetricCard(string title, string value) =>
        new()
        {
            Padding = 12,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = Humanize(title), FontSize = 12 },
                    new Label { Text = value, FontSize = 19, FontAttributes = FontAttributes.Bold }
                }
            }
        };

    private void AddLabel(string text, double size = 14, bool bold = false, bool error = false) =>
        (_results ?? Workspace).Children.Add(new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        });

    private static string FormatValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetDouble(out var number) => number.ToString("N0", CultureInfo.CurrentCulture),
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Null => "—",
        JsonValueKind.Array => $"{value.GetArrayLength()} items",
        JsonValueKind.Object => "Details",
        _ => value.ToString()
    };

    private static string Humanize(string text) =>
        string.Concat(text.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString())).Replace('_', ' ');

    private static Button CreateButton(string text, Func<Task> onClick)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
