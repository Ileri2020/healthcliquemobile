using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class StaffPage : ContentPage
{
    private Entry? _name;
    private Entry? _email;
    private Entry? _contact;
    private Label? _status;
    private Label? _created;

    public StaffPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        AddLabel("Register a customer", 28, true);
        AddLabel("Create a customer account directly in HealthClique. Email and phone number are optional.");
        try
        {
            var user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = user;
            if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user?.Role, "staff", StringComparison.OrdinalIgnoreCase))
            {
                AddLabel("Staff or administrator access is required.", 16, true, true);
                return;
            }

            _name = new Entry { Placeholder = "Customer full name" };
            _email = new Entry { Placeholder = "Email address (optional)", Keyboard = Keyboard.Email };
            _contact = new Entry { Placeholder = "Phone number (optional)", Keyboard = Keyboard.Telephone };
            _status = AddLabel(string.Empty, 14, false, true);
            _created = AddLabel(string.Empty, 14, true);
            _status.IsVisible = false;
            _created.IsVisible = false;
            Workspace.Children.Add(_name);
            Workspace.Children.Add(_email);
            Workspace.Children.Add(_contact);
            Workspace.Children.Add(CreateButton("Create customer account", CreateAccountAsync));
            Workspace.Children.Add(CreateButton("Clear form", () =>
            {
                _name.Text = string.Empty;
                _email.Text = string.Empty;
                _contact.Text = string.Empty;
                _status.IsVisible = false;
                _created.IsVisible = false;
                return Task.CompletedTask;
            }, outline: true));
            AddLabel("Staff-created accounts use the temporary password shown after registration. Customers with an email address should change it after signing in.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            AddLabel($"Staff service unavailable: {exception.Message}", error: true);
        }
    }

    private async Task CreateAccountAsync()
    {
        if (_name is null || _email is null || _contact is null || _status is null || _created is null)
            return;
        if (string.IsNullOrWhiteSpace(_name.Text) || _name.Text.Trim().Length < 2 || _name.Text.Trim().Length > 120)
        {
            _status.Text = "Enter a name between 2 and 120 characters.";
            _status.IsVisible = true;
            return;
        }
        if (!string.IsNullOrWhiteSpace(_email.Text)
            && (!System.Net.Mail.MailAddress.TryCreate(_email.Text.Trim(), out var address)
                || !string.Equals(address.Address, _email.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            _status.Text = "Enter a valid email address.";
            _status.IsVisible = true;
            return;
        }
        try
        {
            var result = await InventoryApi.SendAsync<StaffAccountCreateResponse>(
                PageLayout.ApiBaseUrl,
                HttpMethod.Post,
                "api/staff/users",
                new { name = _name.Text.Trim(), email = _email.Text?.Trim(), contact = _contact.Text?.Trim() });
            if (result.User is null)
                throw new JsonException("The staff service did not return the created account.");
            _status.IsVisible = false;
            var emailText = string.IsNullOrWhiteSpace(result.User.Email) ? " · No email provided" : $" · {result.User.Email}";
            _created.Text = $"Account created for {result.User.Name}{emailText}. Temporary password: {result.DefaultPassword}";
            _created.IsVisible = true;
            _name.Text = string.Empty;
            _email.Text = string.Empty;
            _contact.Text = string.Empty;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            _status.Text = exception.Message;
            _status.IsVisible = true;
        }
    }

    private Label AddLabel(string text, double size = 14, bool bold = false, bool error = false)
    {
        var label = new Label
        {
            Text = text,
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = error ? Colors.Red : null
        };
        Workspace.Children.Add(label);
        return label;
    }

    private static Button CreateButton(string text, Func<Task> onClick, bool outline = false)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
