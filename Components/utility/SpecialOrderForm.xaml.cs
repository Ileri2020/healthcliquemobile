using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class SpecialOrderForm : ContentView
{
    public static readonly BindableProperty TriggerTextProperty = BindableProperty.Create(nameof(TriggerText), typeof(string), typeof(SpecialOrderForm), "Order Scarce / Special Drug");
    public static readonly BindableProperty ShowTriggerProperty = BindableProperty.Create(nameof(ShowTrigger), typeof(bool), typeof(SpecialOrderForm), true);
    public static readonly BindableProperty DrugNameProperty = BindableProperty.Create(nameof(DrugName), typeof(string), typeof(SpecialOrderForm), string.Empty);
    public static readonly BindableProperty QuantityTextProperty = BindableProperty.Create(nameof(QuantityText), typeof(string), typeof(SpecialOrderForm), "1");
    public static readonly BindableProperty DetailsProperty = BindableProperty.Create(nameof(Details), typeof(string), typeof(SpecialOrderForm), string.Empty);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(SpecialOrderForm), false);
    public static readonly BindableProperty IsSubmittingProperty = BindableProperty.Create(nameof(IsSubmitting), typeof(bool), typeof(SpecialOrderForm), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(SpecialOrderForm), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(SpecialOrderForm), string.Empty);

    private FileResult? _prescriptionFile;

    public SpecialOrderForm()
    {
        InitializeComponent();
        OpenCommand = new Command(() => IsOpen = true);
        CloseCommand = new Command(() => IsOpen = false);
        PickPrescriptionCommand = new Command(async () => await PickPrescriptionAsync());
        SubmitCommand = new Command(async () => await SubmitAsync(), () => !IsSubmitting);
    }

    public string TriggerText { get => (string)GetValue(TriggerTextProperty); set => SetValue(TriggerTextProperty, value); }
    public bool ShowTrigger { get => (bool)GetValue(ShowTriggerProperty); set => SetValue(ShowTriggerProperty, value); }
    public string DrugName { get => (string)GetValue(DrugNameProperty); set => SetValue(DrugNameProperty, value); }
    public string QuantityText { get => (string)GetValue(QuantityTextProperty); set => SetValue(QuantityTextProperty, value); }
    public string Details { get => (string)GetValue(DetailsProperty); set => SetValue(DetailsProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsSubmitting { get => (bool)GetValue(IsSubmittingProperty); private set => SetValue(IsSubmittingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasPrescription => _prescriptionFile is not null;
    public string PrescriptionButtonText => HasPrescription ? "Change prescription file" : "Choose prescription (optional)";
    public string DetailsLabel => _prescriptionFile?.FileName ?? string.Empty;
    public string SubmitText => IsSubmitting ? "Adding..." : "Place Special Request";
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand PickPrescriptionCommand { get; }
    public ICommand SubmitCommand { get; }
    public event Action<object?, CommerceCartItem>? AddToCartRequested;
    public event Action<object?, PrescriptionCartRequest>? SpecialOrderRequested;
    public event Action<object?, Exception>? RequestFailed;

    private async Task PickPrescriptionAsync()
    {
        try
        {
            _prescriptionFile = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a prescription",
                FileTypes = FilePickerFileType.Images
            });
            OnPropertyChanged(nameof(HasPrescription));
            OnPropertyChanged(nameof(PrescriptionButtonText));
            OnPropertyChanged(nameof(DetailsLabel));
        }
        catch (Exception exception) when (exception is InvalidOperationException or PermissionException or FeatureNotSupportedException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task SubmitAsync()
    {
        if (IsSubmitting)
            return;
        if (string.IsNullOrWhiteSpace(DrugName))
        {
            SetError(new ArgumentException("Enter a drug or formulation name."));
            return;
        }
        if (!int.TryParse(QuantityText, out var quantity) || quantity < 1)
        {
            SetError(new ArgumentException("Quantity must be a whole number greater than zero."));
            return;
        }

        IsSubmitting = true;
        HasStatus = false;
        UpdateSubmitState();
        try
        {
            var request = new PrescriptionCartRequest
            {
                DrugName = DrugName.Trim(),
                Details = Details.Trim(),
                Quantity = quantity,
                PrescriptionFileName = _prescriptionFile?.FileName
            };
            var item = new CommerceCartItem
            {
                Id = $"special-manual-{Guid.NewGuid():N}",
                Name = $"SPECIAL: {request.DrugName}",
                CustomName = request.DrugName,
                Category = "Extemporaneous",
                Quantity = request.Quantity,
                IsSpecial = true,
                Price = 0
            };
            AddToCartRequested?.Invoke(this, item);
            SpecialOrderRequested?.Invoke(this, request);
            StatusMessage = "Added to cart. A pharmacist will review and price this shortly.";
            HasStatus = true;
            IsOpen = false;
            DrugName = string.Empty;
            QuantityText = "1";
            Details = string.Empty;
            _prescriptionFile = null;
            OnPropertyChanged(nameof(HasPrescription));
            OnPropertyChanged(nameof(PrescriptionButtonText));
            OnPropertyChanged(nameof(DetailsLabel));
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            IsSubmitting = false;
            UpdateSubmitState();
        }
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private void UpdateSubmitState()
    {
        OnPropertyChanged(nameof(SubmitText));
        (SubmitCommand as Command)?.ChangeCanExecute();
    }
}
