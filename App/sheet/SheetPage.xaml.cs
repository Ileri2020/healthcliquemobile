using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class SheetPage : ContentPage, IQueryAttributable
{
    private static readonly string[] Models =
        ["product", "category", "brand", "vendor", "activeIngredient", "healthConcern", "stock", "bulkPrice", "productVendor", "user"];
    private readonly List<JsonElement> _records = [];
    private string _model = "product";
    private string _searchText = string.Empty;
    private int _page;
    private const int PageSize = 50;
    private int _total;
    private bool _initialized;
    private VerticalStackLayout? _results;

    public SheetPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RenderAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("model", out var model)
            && Models.Contains(model?.ToString(), StringComparer.OrdinalIgnoreCase))
            _model = model!.ToString()!;
        if (IsLoaded)
            _ = LoadDataAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddLabel("Product and inventory data sheet", 28, true);
        AddLabel("Browse the catalog dataset from the HealthClique admin service.");
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                AddLabel("Administrator access is required.", 16, true, true);
                Workspace.Children.Add(CreateButton("Open administration", async () => await PageLayout.NavigateToAsync("/admin")));
                return;
            }

            var modelPicker = new Picker { Title = "Data type", ItemsSource = Models.ToList(), SelectedItem = _model };
            modelPicker.SelectedIndexChanged += async (_, _) =>
            {
                if (modelPicker.SelectedItem is string selected)
                {
                    _model = selected;
                    _page = 0;
                    await LoadDataAsync();
                }
            };
            var search = new Entry { Placeholder = "Search records by name" };
            search.TextChanged += (_, args) => _searchText = args.NewTextValue?.Trim() ?? string.Empty;
            Workspace.Children.Add(modelPicker);
            Workspace.Children.Add(search);
            Workspace.Children.Add(CreateButton("Search", async () =>
            {
                _page = 0;
                await LoadDataAsync();
            }));
            _results = new VerticalStackLayout { Spacing = 8 };
            Workspace.Children.Add(_results);
            _initialized = true;
            await LoadDataAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Data sheet unavailable: {exception.Message}", error: true);
        }
    }

    private async Task LoadDataAsync()
    {
        if (!_initialized || _results is null)
            return;
        try
        {
            var query = $"api/sheet?model={Uri.EscapeDataString(_model)}&limit={PageSize}&offset={_page * PageSize}&search={Uri.EscapeDataString(_searchText)}&details=true";
            var result = await InventoryApi.GetAsync<JsonElement>(PageLayout.ApiBaseUrl, query);
            if (result.ValueKind != JsonValueKind.Object
                || !result.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array)
                throw new JsonException("The data sheet endpoint returned an invalid response.");
            _records.Clear();
            _records.AddRange(data.EnumerateArray().Select(item => item.Clone()));
            _total = result.TryGetProperty("total", out var total) && total.TryGetInt32(out var count) ? count : _records.Count;
            RenderRecords();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            _results.Children.Clear();
            AddResultLabel($"Unable to load records: {exception.Message}", error: true);
        }
    }

    private void RenderRecords()
    {
        if (_results is null)
            return;
        _results.Children.Clear();
        AddResultLabel($"{_model} · {_total:N0} record(s) · Page {_page + 1}", 16, true);

        if (_records.Count == 0)
            AddResultLabel("No records match this search.");
        foreach (var record in _records)
        {
            var fields = record.ValueKind == JsonValueKind.Object
                ? record.EnumerateObject().Where(property => property.Name is not "id" and not "version").Take(8)
                    .Select(property => $"{Humanize(property.Name)}: {Summarize(property.Value)}")
                : [record.ToString()];
            var details = new VerticalStackLayout { Spacing = 4 };
            foreach (var field in fields)
                details.Children.Add(new Label { Text = field, FontSize = 13 });
            _results.Children.Add(new Border
            {
                Padding = 10,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = details
            });
        }

        var paging = new HorizontalStackLayout { Spacing = 8 };
        paging.Children.Add(CreateButton("Previous", async () =>
        {
            if (_page > 0)
            {
                _page--;
                await LoadDataAsync();
            }
        }));
        paging.Children.Add(CreateButton("Next", async () =>
        {
            if ((_page + 1) * PageSize < _total)
            {
                _page++;
                await LoadDataAsync();
            }
        }));
        _results.Children.Add(paging);
        _results.Children.Add(CreateButton("Open admin tools", async () => await PageLayout.NavigateToAsync("/admin"), outline: true));
    }

    private void AddLabel(string text, double size = 14, bool bold = false, bool error = false) =>
        Workspace.Children.Add(new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        });

    private void AddResultLabel(string text, double size = 14, bool bold = false, bool error = false) =>
        _results?.Children.Add(new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        });

    private static string Summarize(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => "—",
        JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Take(5).Select(Summarize)),
        JsonValueKind.Object => value.TryGetProperty("name", out var name) ? name.ToString() : "Details",
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        _ => value.ToString()
    };

    private static string Humanize(string text) =>
        string.Concat(text.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString())).Replace('_', ' ');

    private static Button CreateButton(string text, Func<Task> onClick, bool outline = false)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
