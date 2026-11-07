using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class AdminBulkManager : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(AdminBulkManager), string.Empty);
    public static readonly BindableProperty EndpointUrlProperty = BindableProperty.Create(nameof(EndpointUrl), typeof(string), typeof(AdminBulkManager), "api/admin/bulk");
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(AdminBulkManager), false);
    public static readonly BindableProperty ModelProperty = BindableProperty.Create(nameof(Model), typeof(string), typeof(AdminBulkManager), "product", propertyChanged: OnModelChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(AdminBulkManager), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanExportProperty = BindableProperty.Create(nameof(CanExport), typeof(bool), typeof(AdminBulkManager), true);
    public static readonly BindableProperty CanImportProperty = BindableProperty.Create(nameof(CanImport), typeof(bool), typeof(AdminBulkManager), true);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(AdminBulkManager), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(AdminBulkManager), string.Empty);
    public static readonly BindableProperty HasErrorsProperty = BindableProperty.Create(nameof(HasErrors), typeof(bool), typeof(AdminBulkManager), false);

    public AdminBulkManager()
    {
        InitializeComponent();
        Models = ["product", "category", "brand", "activeIngredient", "stock"];
        Errors = [];
        ExportCommand = new Command(async () => await ExportAsync(), () => CanExport);
        ImportCommand = new Command(async () => await ImportAsync(), () => CanImport);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string EndpointUrl { get => (string)GetValue(EndpointUrlProperty); set => SetValue(EndpointUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public string Model { get => (string)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanExport { get => (bool)GetValue(CanExportProperty); private set => SetValue(CanExportProperty, value); }
    public bool CanImport { get => (bool)GetValue(CanImportProperty); private set => SetValue(CanImportProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasErrors { get => (bool)GetValue(HasErrorsProperty); private set => SetValue(HasErrorsProperty, value); }
    public ObservableCollection<string> Models { get; }
    public ObservableCollection<string> Errors { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public event Action<object?, Exception>? OperationFailed;

    private static void OnModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AdminBulkManager)bindable;
        control.ClearStatus();
        control.UpdateCommandAvailability();
    }

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AdminBulkManager)bindable;
        control.UpdateCommandAvailability();
    }

    private void UpdateCommandAvailability()
    {
        CanExport = !IsLoading;
        CanImport = !IsLoading && Model != "stock";
        (ExportCommand as Command)?.ChangeCanExecute();
        (ImportCommand as Command)?.ChangeCanExecute();
    }

    private async Task ExportAsync()
    {
        if (!IsAdmin)
        {
            SetStatus("Administrator access is required.");
            return;
        }
        IsLoading = true;
        ClearStatus();
        try
        {
            var uri = CreateEndpointUri();
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var json = await response.Content.ReadAsStringAsync();
            var csv = ToCsv(json);
            var fileName = $"{Model}-export-{DateTime.Now:yyyyMMddHHmmss}.csv";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, csv, new UTF8Encoding(false));
            await Share.Default.RequestAsync(new ShareFileRequest("Export data", new ShareFile(path)));
            SetStatus("Export ready to share.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or IOException or UnauthorizedAccessException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ImportAsync()
    {
        if (Model == "stock")
        {
            SetStatus("Stock import is not supported by the server endpoint.");
            return;
        }
        if (!IsAdmin)
        {
            SetStatus("Administrator access is required.");
            return;
        }
        IsLoading = true;
        ClearStatus();
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a CSV file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = [".csv"],
                    [DevicePlatform.Android] = ["text/csv", "text/comma-separated-values"],
                    [DevicePlatform.iOS] = ["public.comma-separated-values-text"]
                })
            });
            if (file is null)
                return;

            var uri = CreateEndpointUri();
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var csv = await reader.ReadToEndAsync();
            var rows = ParseCsv(csv);
            if (rows.Count < 2)
                throw new InvalidOperationException("The selected CSV must contain a header and at least one data row.");
            var headers = rows[0];
            var data = rows.Skip(1)
                .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
                .Select(row => headers.Select((header, index) => new { header, value = index < row.Count ? row[index] : string.Empty })
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new { data });
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BulkImportResult>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result is null)
                throw new JsonException("The import endpoint returned an empty or invalid result.");
            foreach (var error in result.Errors)
                Errors.Add(error);
            HasErrors = Errors.Count > 0;
            SetStatus($"Import complete: {result.Created} created, {result.Updated} updated, {result.Errors.Count} errors.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Uri CreateEndpointUri()
    {
        var separator = EndpointUrl.Contains('?') ? "&" : "?";
        return UtilityApi.CreateUri(ApiBaseUrl, $"{EndpointUrl}{separator}model={Uri.EscapeDataString(Model)}");
    }

    private static string ToCsv(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rows = document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array
                ? data.EnumerateArray().ToArray()
                : throw new JsonException("The bulk export endpoint must return an object containing a data array.");
        if (rows.Length == 0)
            return string.Empty;
        if (rows.Any(row => row.ValueKind != JsonValueKind.Object))
            throw new JsonException("The export endpoint returned a row that is not a JSON object.");

        var headers = rows.SelectMany(row => row.EnumerateObject().Select(property => property.Name)).Distinct(StringComparer.Ordinal).ToArray();
        var output = new StringBuilder();
        output.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            output.AppendLine(string.Join(",", headers.Select(header =>
                row.TryGetProperty(header, out var value) ? EscapeCsv(value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString()) : string.Empty)));
        }
        return output.ToString();
    }

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;
        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (inQuotes)
            {
                if (character == '"' && index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else if (character == '"')
                    inQuotes = false;
                else
                    value.Append(character);
            }
            else if (character == '"' && value.Length == 0)
                inQuotes = true;
            else if (character == ',')
            {
                row.Add(value.ToString());
                value.Clear();
            }
            else if (character == '\r' || character == '\n')
            {
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                    index++;
                row.Add(value.ToString());
                value.Clear();
                rows.Add(row);
                row = [];
            }
            else
                value.Append(character);
        }

        if (inQuotes)
            throw new InvalidOperationException("The CSV file contains an unterminated quoted field.");
        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }
        return rows;
    }

    private static string EscapeCsv(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";

    private void ClearStatus()
    {
        HasStatus = false;
        StatusMessage = string.Empty;
        Errors.Clear();
        HasErrors = false;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
