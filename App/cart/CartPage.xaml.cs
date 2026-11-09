using System.Net.Http;
using System.Text.Json;
using EEE.Components.Utility;

namespace EEE.ClientApp.Pages;

public partial class CartPage : ContentPage, IQueryAttributable
{
    private string? _selectedCartId;
    private CommerceChatUser? _user;

    public CartPage()
    {
        InitializeComponent();
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlertAsync("Navigation unavailable", exception.Message, "OK");
        Loaded += async (_, _) => await RenderAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _selectedCartId = GetQueryValue(query, "id");
        if (IsLoaded)
            _ = RenderAsync();
    }

    private async Task RenderAsync()
    {
        Workspace.Children.Clear();
        Workspace.Children.Add(new Label
        {
            Text = "Your cart",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#176B57")
        });

        try
        {
            _user = await UtilityApi.GetSignedInUserAsync(PageLayout.ApiBaseUrl);
            CommerceSessionState.CurrentUser = _user;
            if (!string.IsNullOrWhiteSpace(_selectedCartId))
            {
                await RenderCartDetailsAsync(_selectedCartId);
                return;
            }

            var cart = new CartClient
            {
                ApiBaseUrl = PageLayout.ApiBaseUrl,
                UserId = _user?.Id ?? string.Empty,
                UserRole = _user?.Role ?? "customer",
                Items = CommerceCartState.Shared.Items
            };
            cart.RequestFailed += async (_, exception) =>
                await DisplayAlertAsync("Cart request failed", exception.Message, "OK");
            Workspace.Children.Add(cart);

            if (_user is null)
            {
                Workspace.Children.Add(new Label { Text = "Sign in to save your order and continue to checkout." });
                Workspace.Children.Add(CreateButton("Open my account", async () => await PageLayout.NavigateToAsync("/account")));
                return;
            }

            await RenderOrderHistoryAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            Workspace.Children.Add(new Label
            {
                Text = $"Cart service unavailable: {exception.Message}",
                TextColor = Colors.Red
            });
        }
    }

    private async Task RenderOrderHistoryAsync()
    {
        Workspace.Children.Add(new Label
        {
            Text = "Saved orders",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold
        });
        var path = string.Equals(_user?.Role, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_user?.Role, "staff", StringComparison.OrdinalIgnoreCase)
            ? "api/dbhandler?model=cart&status=paid,unconfirmed"
            : $"api/dbhandler?model=cart&userId={Uri.EscapeDataString(_user!.Id)}";
        var carts = await InventoryApi.GetAsync<List<CommerceCart>>(PageLayout.ApiBaseUrl, path);
        if (carts.Count == 0)
        {
            Workspace.Children.Add(new Label { Text = "No saved orders yet." });
            return;
        }

        foreach (var order in carts.OrderByDescending(item => item.Payment is not null))
        {
            var description = $"{order.Name ?? "Order"} · {order.Status} · ₦{order.Total:N0}";
            var card = new Border
            {
                Padding = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label { Text = description, FontAttributes = FontAttributes.Bold },
                        new Label { Text = $"{order.Products.Count} item(s)", FontSize = 13 }
                    }
                }
            };
            Workspace.Children.Add(card);
            Workspace.Children.Add(CreateButton("View order", async () =>
                await PageLayout.NavigateToAsync($"/cart?id={Uri.EscapeDataString(order.Id)}")));
        }
    }

    private async Task RenderCartDetailsAsync(string cartId)
    {
        Workspace.Children.Add(CreateButton("Back to cart", async () => await PageLayout.NavigateToAsync("/cart")));
        var details = new CartDetails
        {
            ApiBaseUrl = PageLayout.ApiBaseUrl,
            CartId = cartId,
            UserId = _user?.Id ?? string.Empty,
            UserRole = _user?.Role ?? "customer",
            IsAdmin = string.Equals(_user?.Role, "admin", StringComparison.OrdinalIgnoreCase)
        };
        details.RequestFailed += async (_, exception) =>
            await DisplayAlertAsync("Order request failed", exception.Message, "OK");
        Workspace.Children.Add(details);
    }

    private static string? GetQueryValue(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) ? Uri.UnescapeDataString(value?.ToString() ?? string.Empty) : null;

    private static Button CreateButton(string text, Func<Task> onClick)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await onClick();
        return button;
    }
}
