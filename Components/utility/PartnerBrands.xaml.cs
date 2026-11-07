using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class PartnerBrands : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private PartnerBrand? _editingBrand;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(PartnerBrands), string.Empty, propertyChanged: OnApiChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(PartnerBrands), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(PartnerBrands), false);
    public static readonly BindableProperty IsEditorOpenProperty = BindableProperty.Create(nameof(IsEditorOpen), typeof(bool), typeof(PartnerBrands), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(PartnerBrands), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(PartnerBrands), string.Empty);
    public static readonly BindableProperty EditNameProperty = BindableProperty.Create(nameof(EditName), typeof(string), typeof(PartnerBrands), string.Empty);
    public static readonly BindableProperty EditOrderProperty = BindableProperty.Create(nameof(EditOrder), typeof(string), typeof(PartnerBrands), "0");

    public PartnerBrands()
    {
        InitializeComponent();
        Brands = [];
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        NewCommand = new Command(StartNew, () => IsAdmin);
        EditCommand = new Command<PartnerBrand>(StartEdit, brand => IsAdmin && brand is not null);
        DeleteCommand = new Command<PartnerBrand>(async brand => await DeleteAsync(brand), brand => IsAdmin && brand is not null);
        SelectCommand = new Command<PartnerBrand>(brand =>
        {
            if (brand is not null)
                BrandSelected?.Invoke(this, brand);
        });
        SaveCommand = new Command(async () => await SaveAsync(), () => IsAdmin && !IsLoading);
        SaveOrderCommand = new Command(async () => await SaveOrderAsync(), () => IsAdmin && !IsLoading);
        CancelCommand = new Command(() => IsEditorOpen = false);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool IsEditorOpen { get => (bool)GetValue(IsEditorOpenProperty); set => SetValue(IsEditorOpenProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public string EditName { get => (string)GetValue(EditNameProperty); set => SetValue(EditNameProperty, value); }
    public string EditOrder { get => (string)GetValue(EditOrderProperty); set => SetValue(EditOrderProperty, value); }
    public string EditorTitle => _editingBrand is null ? "Add partner" : "Edit partner";
    public bool IsEmpty => !IsLoading && Brands.Count == 0;
    public ObservableCollection<PartnerBrand> Brands { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveOrderCommand { get; }
    public ICommand CancelCommand { get; }
    public event Action<object?, Exception>? RequestFailed;
    public event Action<object?, PartnerBrand>? BrandSelected;

    private static void OnApiChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((PartnerBrands)bindable).LoadAsync();

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((PartnerBrands)bindable).UpdateCommands();

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=brand&limit=200"), (object?)null);
            var brands = await response.Content.ReadFromJsonAsync<List<PartnerBrand>>(JsonOptions)
                ?? throw new JsonException("The brand endpoint returned an empty response.");
            Brands.Clear();
            foreach (var brand in brands.OrderBy(item => item.Order).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                Brands.Add(brand);
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void StartNew()
    {
        if (!IsAdmin)
            return;
        _editingBrand = null;
        EditName = string.Empty;
        EditOrder = (Brands.Count + 1).ToString();
        OnPropertyChanged(nameof(EditorTitle));
        IsEditorOpen = true;
    }

    private void StartEdit(PartnerBrand? brand)
    {
        if (!IsAdmin || brand is null)
            return;
        _editingBrand = brand;
        EditName = brand.Name;
        EditOrder = brand.Order.ToString();
        OnPropertyChanged(nameof(EditorTitle));
        IsEditorOpen = true;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName) || !int.TryParse(EditOrder, out var order))
        {
            SetError(new ArgumentException("Enter a company name and a valid display order."));
            return;
        }
        IsLoading = true;
        try
        {
            var method = _editingBrand is null ? HttpMethod.Post : HttpMethod.Put;
            var path = _editingBrand is null
                ? "api/dbhandler?model=brand"
                : $"api/dbhandler?model=brand&id={Uri.EscapeDataString(_editingBrand.Id)}";
            using var response = await UtilityApi.SendAsync(method, UtilityApi.CreateUri(ApiBaseUrl, path),
                new { id = _editingBrand?.Id, name = EditName.Trim(), order });
            IsEditorOpen = false;
            HasStatus = false;
            IsLoading = false;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private async Task DeleteAsync(PartnerBrand? brand)
    {
        if (!IsAdmin || brand is null)
            return;
        try
        {
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete,
                UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=brand&id={Uri.EscapeDataString(brand.Id)}"), (object?)null);
            Brands.Remove(brand);
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task SaveOrderAsync()
    {
        if (!IsAdmin || IsLoading)
            return;
        IsLoading = true;
        try
        {
            var updates = Brands.Select(brand => new { brand.Id, brand.Order }).ToArray();
            using var response = await UtilityApi.SendAsync(HttpMethod.Put,
                UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=brand"), updates);
            IsLoading = false;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private void UpdateCommands()
    {
        (RefreshCommand as Command)?.ChangeCanExecute();
        (NewCommand as Command)?.ChangeCanExecute();
        (EditCommand as Command<PartnerBrand>)?.ChangeCanExecute();
        (DeleteCommand as Command<PartnerBrand>)?.ChangeCanExecute();
        (SaveCommand as Command)?.ChangeCanExecute();
        (SaveOrderCommand as Command)?.ChangeCanExecute();
    }
}
