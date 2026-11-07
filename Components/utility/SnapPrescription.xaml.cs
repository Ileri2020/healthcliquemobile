using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class SnapPrescription : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private FileResult? _imageFile;
    private bool _isAddingToCart;

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(SnapPrescription), string.Empty);
    public static readonly BindableProperty TriggerTextProperty = BindableProperty.Create(nameof(TriggerText), typeof(string), typeof(SnapPrescription), "Snap Prescription");
    public static readonly BindableProperty ShowTriggerProperty = BindableProperty.Create(nameof(ShowTrigger), typeof(bool), typeof(SnapPrescription), true);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(SnapPrescription), false);
    public static readonly BindableProperty ManualTextProperty = BindableProperty.Create(nameof(ManualText), typeof(string), typeof(SnapPrescription), string.Empty);
    public static readonly BindableProperty ImagePathProperty = BindableProperty.Create(nameof(ImagePath), typeof(string), typeof(SnapPrescription), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(SnapPrescription), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(SnapPrescription), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(SnapPrescription), string.Empty);

    public SnapPrescription()
    {
        InitializeComponent();
        Groups = [];
        OpenCommand = new Command(() => IsOpen = true);
        PickImageCommand = new Command(async () => await PickImageAsync());
        CaptureCommand = new Command(async () => await CaptureImageAsync());
        RemoveImageCommand = new Command(RemoveImage);
        ProcessCommand = new Command(async () => await ProcessAsync(), () => !IsLoading);
        ResetCommand = new Command(Reset);
        SelectSpecialCommand = new Command<PrescriptionMatchGroupView>(group =>
        {
            if (group is not null)
                group.SpecialSelected = !group.SpecialSelected;
        });
        AddSelectedCommand = new Command(async () => await AddSelectedAsync(), () => !_isAddingToCart);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string TriggerText { get => (string)GetValue(TriggerTextProperty); set => SetValue(TriggerTextProperty, value); }
    public bool ShowTrigger { get => (bool)GetValue(ShowTriggerProperty); set => SetValue(ShowTriggerProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string ManualText { get => (string)GetValue(ManualTextProperty); set => SetValue(ManualTextProperty, value); }
    public string ImagePath { get => (string)GetValue(ImagePathProperty); private set => SetValue(ImagePathProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasImage => _imageFile is not null;
    public bool HasGroups => Groups.Count > 0;
    public string IdentifyText => IsLoading ? "Identifying..." : "Identify Products";
    public ObservableCollection<PrescriptionMatchGroupView> Groups { get; }
    public ICommand OpenCommand { get; }
    public ICommand PickImageCommand { get; }
    public ICommand CaptureCommand { get; }
    public ICommand RemoveImageCommand { get; }
    public ICommand ProcessCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SelectSpecialCommand { get; }
    public ICommand AddSelectedCommand { get; }
    public event Action<object?, CommerceCartItem>? AddToCartRequested;
    public event Action<object?, Exception>? RequestFailed;

    private async Task PickImageAsync()
    {
        try
        {
            _imageFile = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a prescription image",
                FileTypes = FilePickerFileType.Images
            });
            await UpdateImagePathAsync();
        }
        catch (Exception exception) when (exception is FilePickerException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task CaptureImageAsync()
    {
        try
        {
            _imageFile = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Capture prescription"
            });
            await UpdateImagePathAsync();
        }
        catch (Exception exception) when (exception is PermissionException or InvalidOperationException or TaskCanceledException or FeatureNotSupportedException)
        {
            SetError(exception);
        }
    }

    private async Task UpdateImagePathAsync()
    {
        if (_imageFile is null)
            return;
        var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}_{Path.GetFileName(_imageFile.FileName)}");
        await using var source = await _imageFile.OpenReadAsync();
        await using var destination = File.Create(path);
        await source.CopyToAsync(destination);
        ImagePath = path;
        OnPropertyChanged(nameof(HasImage));
    }

    private void RemoveImage()
    {
        _imageFile = null;
        ImagePath = string.Empty;
        OnPropertyChanged(nameof(HasImage));
    }

    private async Task ProcessAsync()
    {
        if (IsLoading)
            return;
        if (_imageFile is null && string.IsNullOrWhiteSpace(ManualText))
        {
            SetError(new ArgumentException("Choose a prescription image or enter a product list."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl before analyzing a prescription."));
            return;
        }

        IsLoading = true;
        HasStatus = false;
        Groups.Clear();
        UpdateCommands();
        try
        {
            string? imageData = null;
            if (_imageFile is not null)
            {
                await using var stream = await _imageFile.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                imageData = Convert.ToBase64String(buffer.ToArray());
            }
            using var parseResponse = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/products/parse-prescription"),
                new
                {
                    image = imageData,
                    mimeType = _imageFile?.ContentType ?? "image/jpeg",
                    manualText = ManualText.Trim()
                });
            var parsed = await parseResponse.Content.ReadFromJsonAsync<PrescriptionParseResponse>(JsonOptions)
                ?? throw new JsonException("The prescription analysis endpoint returned an empty response.");
            if (parsed.Products.Count == 0)
                throw new JsonException("No products were identified.");

            var searchRequest = parsed.Products.Select(product => new
            {
                name = $"{product.Name} {product.Form}".Trim(),
                quantity = product.Quantity,
                grams = product.Grams,
                form = product.Form,
                activeIngredients = product.ActiveIngredients,
                category = product.Category
            }).ToArray();
            using var searchResponse = await UtilityApi.SendAsync(HttpMethod.Post,
                UtilityApi.CreateUri(ApiBaseUrl, "api/products/search-batch"), new { products = searchRequest });
            var search = await searchResponse.Content.ReadFromJsonAsync<PrescriptionSearchResponse>(JsonOptions)
                ?? throw new JsonException("The product matching endpoint returned an empty response.");
            foreach (var group in search.Results)
                Groups.Add(new PrescriptionMatchGroupView(group));
            HasStatus = true;
            StatusMessage = "Review the options and select products to add to your cart.";
            OnPropertyChanged(nameof(HasGroups));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IdentifyText));
            UpdateCommands();
        }
    }

    private Task AddSelectedAsync()
    {
        if (_isAddingToCart)
            return Task.CompletedTask;
        _isAddingToCart = true;
        UpdateCommands();
        try
        {
            var count = 0;
            foreach (var group in Groups)
            {
                var selected = group.Options.Where(option => option.IsSelected).ToList();
                foreach (var option in selected)
                {
                    var quantity = option.RequestedQuantity > 0 ? option.RequestedQuantity : 1;
                    var product = new CommerceProduct
                    {
                        Id = option.Id,
                        Name = option.Name,
                        Price = option.Price,
                        Images = option.Images,
                        Category = new CommerceCategory { Name = group.Category }
                    };
                    AddToCartRequested?.Invoke(this, new CommerceCartItem
                    {
                        ProductId = option.Id,
                        Product = product,
                        Name = option.Name,
                        Price = option.Price,
                        ImageUrl = option.ImageUrl,
                        Category = group.Category,
                        Quantity = quantity
                    });
                    count++;
                }

                if (group.SpecialSelected)
                {
                    var specialName = $"REQ: {group.Name}";
                    AddToCartRequested?.Invoke(this, new CommerceCartItem
                    {
                        Id = $"special-{Guid.NewGuid():N}",
                        Name = specialName,
                        CustomName = group.Name,
                        Price = 0,
                        Quantity = 1,
                        Category = "Special",
                        IsSpecial = true
                    });
                    count++;
                }
            }

            if (count == 0)
            {
                SetError(new InvalidOperationException("Select at least one product or request a pharmacist to prepare/find an item."));
                return Task.CompletedTask;
            }
            IsOpen = false;
            Reset();
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            _isAddingToCart = false;
            UpdateCommands();
        }
        return Task.CompletedTask;
    }

    private void Reset()
    {
        ImagePath = string.Empty;
        _imageFile = null;
        ManualText = string.Empty;
        Groups.Clear();
        HasStatus = false;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(HasGroups));
    }

    private void UpdateCommands()
    {
        (ProcessCommand as Command)?.ChangeCanExecute();
        (AddSelectedCommand as Command)?.ChangeCanExecute();
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private sealed class PrescriptionParseResponse
    {
        public List<PrescriptionIdentifiedItem> Products { get; set; } = [];
    }

    private sealed class PrescriptionSearchResponse
    {
        public List<PrescriptionMatchGroup> Results { get; set; } = [];
    }
}

public sealed class PrescriptionMatchGroupView : INotifyPropertyChanged
{
    private bool _specialSelected;

    public PrescriptionMatchGroupView(PrescriptionMatchGroup group)
    {
        Name = group.IdentifiedItem.Name;
        Category = group.IdentifiedItem.Category;
        RequestSummary = $"Req. Qty: {group.IdentifiedItem.Quantity} • {group.IdentifiedItem.Grams} • {Category}";
        Options = group.Options.Select(option => new PrescriptionOptionView(option)).ToList();
        if (Options.Count == 1)
            Options[0].IsSelected = true;
    }

    public string Name { get; }
    public string Category { get; }
    public string RequestSummary { get; }
    public List<PrescriptionOptionView> Options { get; }
    public bool ShowSpecialOption => Options.Count == 0;
    public string SpecialButtonText => SpecialSelected ? "Item requested" : "Request pharmacist to prepare/find";
    public bool SpecialSelected
    {
        get => _specialSelected;
        set
        {
            if (_specialSelected == value)
                return;
            _specialSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpecialButtonText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class PrescriptionOptionView : INotifyPropertyChanged
{
    private bool _isSelected;

    public PrescriptionOptionView(PrescriptionMatchOption option)
    {
        Id = option.Id;
        Name = option.Name;
        Price = option.Price;
        PriceText = option.PriceText;
        Images = option.Images;
        ImageUrl = option.ImageUrl;
        RequestedQuantity = option.RequestedQuantity;
        Details = string.Join(" • ", new[] { option.Brand?.Name, option.IngredientSummary, option.Description }.Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    public string Id { get; }
    public string Name { get; }
    public double Price { get; }
    public string PriceText { get; }
    public string[] Images { get; }
    public string ImageUrl { get; }
    public int RequestedQuantity { get; }
    public string Details { get; }
    public bool HasOptions => true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
