using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Advert : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Advert), string.Empty);
    public static readonly BindableProperty EndpointUrlProperty = BindableProperty.Create(nameof(EndpointUrl), typeof(string), typeof(Advert), "api/advertexts");
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(Advert), false);
    public static readonly BindableProperty NewTextProperty = BindableProperty.Create(nameof(NewText), typeof(string), typeof(Advert), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Advert), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanModifyProperty = BindableProperty.Create(nameof(CanModify), typeof(bool), typeof(Advert), true);
    public static readonly BindableProperty HasActiveAdvertsProperty = BindableProperty.Create(nameof(HasActiveAdverts), typeof(bool), typeof(Advert), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Advert), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Advert), string.Empty);

    public Advert()
    {
        InitializeComponent();
        Adverts = [];
        ActiveAdverts = [];
        AddCommand = new Command(async () => await AddAsync(), () => CanModify);
        RefreshCommand = new Command(async () => await LoadAsync(), () => CanModify);
        SaveCommand = new Command<AdvertText>(async advert => await SaveAsync(advert), advert => CanModify && advert is not null);
        DeleteCommand = new Command<AdvertText>(async advert => await DeleteAsync(advert), advert => CanModify && advert is not null);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string EndpointUrl { get => (string)GetValue(EndpointUrlProperty); set => SetValue(EndpointUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public string NewText { get => (string)GetValue(NewTextProperty); set => SetValue(NewTextProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanModify { get => (bool)GetValue(CanModifyProperty); private set => SetValue(CanModifyProperty, value); }
    public bool HasActiveAdverts { get => (bool)GetValue(HasActiveAdvertsProperty); private set => SetValue(HasActiveAdvertsProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<AdvertText> Adverts { get; }
    public ObservableCollection<AdvertText> ActiveAdverts { get; }
    public ICommand AddCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public event Action<object?, Exception>? OperationFailed;

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (Advert)bindable;
        control.CanModify = !(bool)newValue;
        (control.AddCommand as Command)?.ChangeCanExecute();
        (control.RefreshCommand as Command)?.ChangeCanExecute();
        (control.SaveCommand as Command<AdvertText>)?.ChangeCanExecute();
        (control.DeleteCommand as Command<AdvertText>)?.ChangeCanExecute();
    }

    private async Task LoadAsync()
    {
        if (IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, EndpointUrl);
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            await using var stream = await response.Content.ReadAsStreamAsync();
            var values = await JsonSerializer.DeserializeAsync<List<AdvertText>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (values is null)
                throw new JsonException("The advertisement endpoint returned an empty or invalid response.");
            Adverts.Clear();
            foreach (var advert in values.OrderBy(item => item.Order))
                Adverts.Add(advert);
            RefreshActiveAdverts();
            SetStatus($"Loaded {Adverts.Count} advertisement messages.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewText))
        {
            SetStatus("Enter advertisement text before adding.");
            return;
        }
        await RunAdminOperationAsync(async () =>
        {
            var advert = new AdvertText { Text = NewText.Trim(), Order = Adverts.Count, Active = true };
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, UtilityApi.CreateUri(ApiBaseUrl, EndpointUrl), advert);
            var created = await response.Content.ReadFromJsonAsync<AdvertText>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (created is null || string.IsNullOrWhiteSpace(created.Id))
                throw new JsonException("The advertisement API did not return the created record.");
            Adverts.Add(created);
            NewText = string.Empty;
            RefreshActiveAdverts();
            SetStatus("Advertisement added.");
        });
    }

    private async Task SaveAsync(AdvertText? advert)
    {
        if (advert is null || string.IsNullOrWhiteSpace(advert.Id))
        {
            SetStatus("The selected advertisement has no valid ID.");
            return;
        }
        await RunAdminOperationAsync(async () =>
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"{EndpointUrl.TrimEnd('/')}/{Uri.EscapeDataString(advert.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Put, uri, advert);
            RefreshActiveAdverts();
            SetStatus("Advertisement updated.");
        });
    }

    private async Task DeleteAsync(AdvertText? advert)
    {
        if (advert is null || string.IsNullOrWhiteSpace(advert.Id))
        {
            SetStatus("The selected advertisement has no valid ID.");
            return;
        }
        await RunAdminOperationAsync(async () =>
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"{EndpointUrl.TrimEnd('/')}/{Uri.EscapeDataString(advert.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete, uri, (object?)null);
            Adverts.Remove(advert);
            RefreshActiveAdverts();
            SetStatus("Advertisement deleted.");
        });
    }

    private async Task RunAdminOperationAsync(Func<Task> operation)
    {
        if (!IsAdmin)
        {
            SetStatus("Administrator access is required.");
            return;
        }
        if (IsLoading)
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            await operation();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetStatus(exception.Message);
            OperationFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshActiveAdverts()
    {
        ActiveAdverts.Clear();
        foreach (var advert in Adverts.Where(item => item.Active).OrderBy(item => item.Order))
            ActiveAdverts.Add(advert);
        HasActiveAdverts = ActiveAdverts.Count > 0;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
