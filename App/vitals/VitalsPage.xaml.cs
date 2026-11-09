using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class VitalsPage : ContentPage
{
    private readonly List<string> _customers = [];
    private Picker? _customerPicker;
    private Entry? _search;
    private Entry? _systolic;
    private Entry? _diastolic;
    private Entry? _pulse;
    private Entry? _bloodSugar;
    private DatePicker? _date;
    private VerticalStackLayout? _readingList;
    private string? _selectedCustomer;

    public VitalsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddLabel("Vitals", 30, true);
        AddLabel("Record blood pressure and pulse, and review a customer's saved readings.");
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user?.Role, "staff", StringComparison.OrdinalIgnoreCase))
            {
                AddLabel("Only authorized staff can record or view customer vital readings.", 16, true, true);
                return;
            }

            _customers.Clear();
            _customers.AddRange(await InventoryApi.GetAsync<List<string>>(PageLayout.ApiBaseUrl, "api/inventory/customers"));
            _search = new Entry { Placeholder = "Search customer name" };
            _customerPicker = new Picker { Title = "Select customer", ItemsSource = _customers };
            _search.TextChanged += (_, args) =>
            {
                var match = _customers.Where(name => string.IsNullOrWhiteSpace(args.NewTextValue)
                    || name.Contains(args.NewTextValue.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                _customerPicker.ItemsSource = match;
                _customerPicker.SelectedItem = null;
                _selectedCustomer = null;
                _readingList?.Children.Clear();
            };
            _customerPicker.SelectedIndexChanged += async (_, _) =>
            {
                _selectedCustomer = _customerPicker.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(_selectedCustomer))
                    await LoadReadingsAsync(_selectedCustomer);
            };
            Workspace.Children.Add(_search);
            Workspace.Children.Add(_customerPicker);
            AddLabel("Reading date");
            _date = new DatePicker { Date = DateTime.Today, MaximumDate = DateTime.Today };
            Workspace.Children.Add(_date);
            AddLabel("Measurements", 20, true);
            _systolic = NumberEntry("Systolic pressure (30–300 mmHg)");
            _diastolic = NumberEntry("Diastolic pressure (20–200 mmHg)");
            _pulse = NumberEntry("Pulse (20–250 bpm)");
            _bloodSugar = NumberEntry("Blood sugar (optional, mg/dL)");
            Workspace.Children.Add(_systolic);
            Workspace.Children.Add(_diastolic);
            Workspace.Children.Add(_pulse);
            Workspace.Children.Add(_bloodSugar);
            Workspace.Children.Add(CreateButton("Save reading", SaveReadingAsync));
            AddLabel("Reading history", 20, true);
            _readingList = new VerticalStackLayout { Spacing = 8 };
            Workspace.Children.Add(_readingList);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Vitals service unavailable: {exception.Message}", error: true);
        }
    }

    private async Task LoadReadingsAsync(string customerName)
    {
        if (_readingList is null)
            return;
        _readingList.Children.Clear();
        try
        {
            var readings = await InventoryApi.GetAsync<List<VitalReading>>(
                PageLayout.ApiBaseUrl, $"api/vitals?customerName={Uri.EscapeDataString(customerName)}");
            if (readings.Count == 0)
            {
                _readingList.Children.Add(new Label { Text = "No saved readings for this customer." });
                return;
            }
            foreach (var reading in readings.OrderByDescending(item => item.RecordedAt))
            {
                var bloodSugar = reading.BloodSugar.HasValue ? $" · Blood sugar {reading.BloodSugar.Value:N1} mg/dL" : string.Empty;
                _readingList.Children.Add(new Border
                {
                    Padding = 12,
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            new Label { Text = reading.RecordedAt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture), FontAttributes = FontAttributes.Bold },
                            new Label { Text = $"Blood pressure {reading.Systolic}/{reading.Diastolic} mmHg · Pulse {reading.Pulse} bpm{bloodSugar}" }
                        }
                    }
                });
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            _readingList.Children.Add(new Label { Text = $"Unable to load readings: {exception.Message}", TextColor = Colors.Red });
        }
    }

    private async Task SaveReadingAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedCustomer) || _systolic is null || _diastolic is null || _pulse is null || _date is null)
        {
            await DisplayAlertAsync("Vitals", "Select a customer before saving.", "OK");
            return;
        }
        if (!int.TryParse(_systolic.Text, out var systolic)
            || !int.TryParse(_diastolic.Text, out var diastolic)
            || !int.TryParse(_pulse.Text, out var pulse)
            || systolic is < 30 or > 300
            || diastolic is < 20 or > 200
            || systolic <= diastolic
            || pulse is < 20 or > 250)
        {
            await DisplayAlertAsync("Vitals", "Enter valid blood pressure and pulse values within the displayed ranges.", "OK");
            return;
        }
        double? sugar = null;
        if (!string.IsNullOrWhiteSpace(_bloodSugar?.Text))
        {
            if (!double.TryParse(_bloodSugar.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                || !double.IsFinite(parsed) || parsed is < 20 or > 1000)
            {
                await DisplayAlertAsync("Vitals", "Blood sugar must be between 20 and 1000 mg/dL.", "OK");
                return;
            }
            sugar = parsed;
        }

        try
        {
            await InventoryApi.SendAsync<VitalReading>(PageLayout.ApiBaseUrl, HttpMethod.Post, "api/vitals", new
            {
                customerName = _selectedCustomer,
                date = _date.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                systolic,
                diastolic,
                pulse,
                bloodSugar = sugar
            });
            _systolic.Text = string.Empty;
            _diastolic.Text = string.Empty;
            _pulse.Text = string.Empty;
            _bloodSugar!.Text = string.Empty;
            await LoadReadingsAsync(_selectedCustomer);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            await DisplayAlertAsync("Unable to save reading", exception.Message, "OK");
        }
    }

    private static Entry NumberEntry(string placeholder) =>
        new() { Placeholder = placeholder, Keyboard = Keyboard.Numeric };

    private void AddLabel(string text, double size = 14, bool bold = false, bool error = false) =>
        Workspace.Children.Add(new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        });

    private static Button CreateButton(string text, Func<Task> onClick)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
