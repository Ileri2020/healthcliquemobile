using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class UserEdit : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(UserEdit), string.Empty, propertyChanged: OnApiChanged);
    public static readonly BindableProperty UserProfileProperty = BindableProperty.Create(nameof(UserProfile), typeof(UserEditProfile), typeof(UserEdit), defaultValueCreator: _ => new UserEditProfile(), propertyChanged: OnUserChanged);
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(UserEdit), false);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty ContactProperty = BindableProperty.Create(nameof(Contact), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(nameof(Role), typeof(string), typeof(UserEdit), "customer");
    public static readonly BindableProperty AvatarUrlProperty = BindableProperty.Create(nameof(AvatarUrl), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty ProfessionalTypeProperty = BindableProperty.Create(nameof(ProfessionalType), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty RegNumberProperty = BindableProperty.Create(nameof(RegNumber), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty LicenseImageProperty = BindableProperty.Create(nameof(LicenseImage), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty FacilityNameProperty = BindableProperty.Create(nameof(FacilityName), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty FacilityAddressProperty = BindableProperty.Create(nameof(FacilityAddress), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty FacilityRegNumberProperty = BindableProperty.Create(nameof(FacilityRegNumber), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty AddressIdProperty = BindableProperty.Create(nameof(AddressId), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty CountryProperty = BindableProperty.Create(nameof(Country), typeof(string), typeof(UserEdit), "Nigeria");
    public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty CityProperty = BindableProperty.Create(nameof(City), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty AddressProperty = BindableProperty.Create(nameof(Address), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty ZipProperty = BindableProperty.Create(nameof(Zip), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty AddressPhoneProperty = BindableProperty.Create(nameof(AddressPhone), typeof(string), typeof(UserEdit), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(UserEdit), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(UserEdit), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(UserEdit), string.Empty);

    public UserEdit()
    {
        InitializeComponent();
        ProfessionalTypes = ["pharmacist", "doctor", "nurse"];
        OpenCommand = new Command(() =>
        {
            CopyFromProfile();
            IsOpen = true;
        });
        CloseCommand = new Command(() => IsOpen = false);
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsLoading);
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public UserEditProfile UserProfile { get => (UserEditProfile)GetValue(UserProfileProperty); set => SetValue(UserProfileProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public string Email { get => (string)GetValue(EmailProperty); set => SetValue(EmailProperty, value); }
    public string Contact { get => (string)GetValue(ContactProperty); set => SetValue(ContactProperty, value); }
    public string Role { get => (string)GetValue(RoleProperty); private set => SetValue(RoleProperty, value); }
    public string AvatarUrl { get => (string)GetValue(AvatarUrlProperty); set => SetValue(AvatarUrlProperty, value); }
    public string ProfessionalType { get => (string)GetValue(ProfessionalTypeProperty); set => SetValue(ProfessionalTypeProperty, value); }
    public string RegNumber { get => (string)GetValue(RegNumberProperty); set => SetValue(RegNumberProperty, value); }
    public string LicenseImage { get => (string)GetValue(LicenseImageProperty); set => SetValue(LicenseImageProperty, value); }
    public string FacilityName { get => (string)GetValue(FacilityNameProperty); set => SetValue(FacilityNameProperty, value); }
    public string FacilityAddress { get => (string)GetValue(FacilityAddressProperty); set => SetValue(FacilityAddressProperty, value); }
    public string FacilityRegNumber { get => (string)GetValue(FacilityRegNumberProperty); set => SetValue(FacilityRegNumberProperty, value); }
    public string AddressId { get => (string)GetValue(AddressIdProperty); private set => SetValue(AddressIdProperty, value); }
    public string Country { get => (string)GetValue(CountryProperty); set => SetValue(CountryProperty, value); }
    public string State { get => (string)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public string City { get => (string)GetValue(CityProperty); set => SetValue(CityProperty, value); }
    public string Address { get => (string)GetValue(AddressProperty); set => SetValue(AddressProperty, value); }
    public string Zip { get => (string)GetValue(ZipProperty); set => SetValue(ZipProperty, value); }
    public string AddressPhone { get => (string)GetValue(AddressPhoneProperty); set => SetValue(AddressPhoneProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public IReadOnlyList<string> ProfessionalTypes { get; }
    public bool IsProfessional => Role.Equals("professional", StringComparison.OrdinalIgnoreCase);
    public bool IsWholesaler => Role.Equals("wholesaler", StringComparison.OrdinalIgnoreCase);
    public string SaveText => IsLoading ? "Saving..." : "Save Changes";
    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SaveCommand { get; }
    public event Action<object?, UserEditProfile>? ProfileSaved;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnUserChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (UserEdit)bindable;
        control.CopyFromProfile();
        if (oldValue is UserEditProfile oldProfile && newValue is UserEditProfile newProfile
            && string.Equals(oldProfile.Id, newProfile.Id, StringComparison.Ordinal))
            return;
        _ = control.LoadAddressAsync();
    }

    private static void OnApiChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((UserEdit)bindable).LoadAddressAsync();

    private void CopyFromProfile()
    {
        if (UserProfile is null)
            return;
        Name = UserProfile.Name;
        Email = UserProfile.Email;
        Contact = UserProfile.Contact;
        Role = UserProfile.Role;
        AvatarUrl = UserProfile.AvatarUrl;
        ProfessionalType = UserProfile.ProfessionalType;
        RegNumber = UserProfile.RegNumber;
        LicenseImage = UserProfile.LicenseImage;
        FacilityName = UserProfile.FacilityName;
        FacilityAddress = UserProfile.FacilityAddress;
        FacilityRegNumber = UserProfile.FacilityRegNumber;
        OnPropertyChanged(nameof(IsProfessional));
        OnPropertyChanged(nameof(IsWholesaler));
    }

    private async Task LoadAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(UserProfile?.Id))
            return;
        try
        {
            var path = $"api/dbhandler?model=shippingAddress&userId={Uri.EscapeDataString(UserProfile.Id)}";
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, UtilityApi.CreateUri(ApiBaseUrl, path), (object?)null);
            var addresses = await response.Content.ReadFromJsonAsync<List<UserEditAddress>>(JsonOptions)
                ?? throw new JsonException("The shipping address endpoint returned an empty response.");
            var address = addresses.FirstOrDefault();
            if (address is null)
                return;
            AddressId = address.Id;
            Country = address.Country;
            State = address.State;
            City = address.City;
            Address = address.Address;
            Zip = address.Zip;
            AddressPhone = address.Phone;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
    }

    private async Task SaveAsync()
    {
        if (IsLoading)
            return;
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(UserProfile?.Id))
        {
            SetError(new InvalidOperationException("Set ApiBaseUrl and provide a signed-in user before saving."));
            return;
        }
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
        {
            SetError(new ArgumentException("Enter your name and email address."));
            return;
        }

        IsLoading = true;
        HasStatus = false;
        UpdateSaveState();
        try
        {
            var profile = new UserEditProfile
            {
                Id = UserProfile.Id,
                Name = Name.Trim(),
                Email = Email.Trim(),
                Contact = Contact.Trim(),
                Role = Role,
                AvatarUrl = AvatarUrl.Trim(),
                ProfessionalType = ProfessionalType.Trim(),
                RegNumber = RegNumber.Trim(),
                LicenseImage = LicenseImage.Trim(),
                FacilityName = FacilityName.Trim(),
                FacilityAddress = FacilityAddress.Trim(),
                FacilityRegNumber = FacilityRegNumber.Trim()
            };
            using var userResponse = await UtilityApi.SendAsync(HttpMethod.Put,
                UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=user&id={Uri.EscapeDataString(profile.Id)}"), profile);
            var savedUser = await userResponse.Content.ReadFromJsonAsync<UserEditProfile>(JsonOptions)
                ?? throw new JsonException("The profile endpoint returned an empty response.");
            UserProfile = savedUser;
            ProfileSaved?.Invoke(this, savedUser);

            if (HasAddressChanges())
                await SaveAddressAsync(profile.Id);

            StatusMessage = "Profile updated successfully.";
            HasStatus = true;
            IsOpen = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateSaveState();
        }
    }

    private bool HasAddressChanges() =>
        !string.IsNullOrWhiteSpace(Address)
        || !string.IsNullOrWhiteSpace(City)
        || !string.IsNullOrWhiteSpace(State)
        || !string.IsNullOrWhiteSpace(Country);

    private async Task SaveAddressAsync(string userId)
    {
        var payload = new UserEditAddress
        {
            Id = AddressId,
            UserId = userId,
            Country = Country,
            State = State,
            City = City,
            Address = Address,
            Zip = Zip,
            Phone = AddressPhone
        };
        var isNew = string.IsNullOrWhiteSpace(AddressId);
        var method = isNew ? HttpMethod.Post : HttpMethod.Put;
        var path = $"api/dbhandler?model=shippingAddress{(isNew ? string.Empty : $"&id={Uri.EscapeDataString(AddressId)}")}";
        using var response = await UtilityApi.SendAsync(method, UtilityApi.CreateUri(ApiBaseUrl, path), payload);
        var savedAddress = await response.Content.ReadFromJsonAsync<UserEditAddress>(JsonOptions)
            ?? throw new JsonException("The address endpoint returned an empty response.");
        AddressId = savedAddress.Id;
    }

    private void UpdateSaveState()
    {
        OnPropertyChanged(nameof(SaveText));
        (SaveCommand as Command)?.ChangeCanExecute();
    }

    private void SetError(Exception exception)
    {
        HasStatus = true;
        StatusMessage = exception.Message;
        RequestFailed?.Invoke(this, exception);
    }

    private sealed class UserEditAddress
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Country { get; set; } = "Nigeria";
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}

public sealed class EditUser : UserEdit { }
