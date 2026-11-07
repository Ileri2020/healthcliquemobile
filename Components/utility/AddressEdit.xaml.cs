using System.Net.Http;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class AddressEdit : ContentView
{
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty UserIdProperty = BindableProperty.Create(nameof(UserId), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty AddressIdProperty = BindableProperty.Create(nameof(AddressId), typeof(string), typeof(AddressEdit), string.Empty, propertyChanged: OnAddressChanged);
    public static readonly BindableProperty CountryProperty = BindableProperty.Create(nameof(Country), typeof(string), typeof(AddressEdit), "Nigeria");
    public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty CityProperty = BindableProperty.Create(nameof(City), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty AddressProperty = BindableProperty.Create(nameof(Address), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty ZipProperty = BindableProperty.Create(nameof(Zip), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty PhoneProperty = BindableProperty.Create(nameof(Phone), typeof(string), typeof(AddressEdit), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(AddressEdit), false, propertyChanged: OnLoadingChanged);
    public static readonly BindableProperty CanSaveProperty = BindableProperty.Create(nameof(CanSave), typeof(bool), typeof(AddressEdit), true);
    public static readonly BindableProperty SubmitTextProperty = BindableProperty.Create(nameof(SubmitText), typeof(string), typeof(AddressEdit), "Save address");
    public static readonly BindableProperty HeadingProperty = BindableProperty.Create(nameof(Heading), typeof(string), typeof(AddressEdit), "Shipping address");
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(AddressEdit), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(AddressEdit), string.Empty);

    public AddressEdit()
    {
        InitializeComponent();
        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string UserId { get => (string)GetValue(UserIdProperty); set => SetValue(UserIdProperty, value); }
    public string AddressId { get => (string)GetValue(AddressIdProperty); set => SetValue(AddressIdProperty, value); }
    public string Country { get => (string)GetValue(CountryProperty); set => SetValue(CountryProperty, value); }
    public string State { get => (string)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public string City { get => (string)GetValue(CityProperty); set => SetValue(CityProperty, value); }
    public string Address { get => (string)GetValue(AddressProperty); set => SetValue(AddressProperty, value); }
    public string Zip { get => (string)GetValue(ZipProperty); set => SetValue(ZipProperty, value); }
    public string Phone { get => (string)GetValue(PhoneProperty); set => SetValue(PhoneProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool CanSave { get => (bool)GetValue(CanSaveProperty); private set => SetValue(CanSaveProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); private set => SetValue(SubmitTextProperty, value); }
    public string Heading { get => (string)GetValue(HeadingProperty); private set => SetValue(HeadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ICommand SaveCommand { get; }
    public event Action<object?, ShippingAddress>? AddressSaved;
    public event Action<object?, Exception>? SaveFailed;

    private static void OnAddressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AddressEdit)bindable;
        var isNewAddress = string.IsNullOrWhiteSpace((string)newValue);
        control.Heading = isNewAddress ? "Add shipping address" : "Edit shipping address";
        if (!control.IsLoading)
            control.SubmitText = isNewAddress ? "Save address" : "Update address";
    }

    private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AddressEdit)bindable;
        control.CanSave = !(bool)newValue;
        control.SubmitText = (bool)newValue ? "Saving..." : string.IsNullOrWhiteSpace(control.AddressId) ? "Save address" : "Update address";
        (control.SaveCommand as Command)?.ChangeCanExecute();
    }

    private async Task SaveAsync()
    {
        if (IsLoading)
            return;
        if (string.IsNullOrWhiteSpace(UserId))
        {
            SetError("A signed-in user ID is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(State))
        {
            SetError("Enter a street address, city, and state.");
            return;
        }

        var value = new ShippingAddress
        {
            UserId = UserId,
            Country = Country,
            State = State,
            City = City,
            Address = Address,
            Zip = Zip,
            Phone = Phone
        };
        IsLoading = true;
        HasStatus = false;
        try
        {
            var method = string.IsNullOrWhiteSpace(AddressId) ? HttpMethod.Post : HttpMethod.Put;
            var query = $"api/dbhandler?model=shippingAddress{(string.IsNullOrWhiteSpace(AddressId) ? "" : $"&id={Uri.EscapeDataString(AddressId)}")}";
            object payload = string.IsNullOrWhiteSpace(AddressId)
                ? value
                : new
                {
                    id = AddressId,
                    userId = value.UserId,
                    country = value.Country,
                    state = value.State,
                    city = value.City,
                    address = value.Address,
                    zip = value.Zip,
                    phone = value.Phone
                };
            using var response = await UtilityApi.SendAsync(method, UtilityApi.CreateUri(ApiBaseUrl, query), payload);
            AddressSaved?.Invoke(this, value);
            StatusMessage = "Address saved.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception.Message);
            SaveFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasStatus = true;
    }
}
