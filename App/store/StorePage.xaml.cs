using System.Windows.Input;
using EEE.Components.Utility;
using EEE.ClientApp;

namespace EEE.ClientApp.Pages;

public partial class StorePage : ContentPage, IQueryAttributable
{
    public StorePage()
    {
        InitializeComponent();
        BindingContext = this;
        ContactCommand = new Command(async () => await PageLayout.NavigateToAsync("/contact"));
        OpenSpecialOrderCommand = new Command(async () =>
        {
            await StoreScroll.ScrollToAsync(SpecialOrderSection, ScrollToPosition.Start, true);
            SpecialOrderForm.IsOpen = true;
        });

        StoreSearch.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        StoreSearch.SearchFailed += async (_, exception) =>
            await DisplayAlert("Search unavailable", exception.Message, "OK");
        StoreProducts.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        StoreProducts.AddToCartRequested += (_, product) => CommerceCartState.Shared.Add(product);
        StoreProducts.RequestFailed += async (_, exception) =>
            await DisplayAlert("Store unavailable", exception.Message, "OK");
        SpecialOrderForm.AddToCartRequested += (_, item) => CommerceCartState.Shared.Add(item);
        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlert("Navigation unavailable", exception.Message, "OK");
    }

    public ICommand ContactCommand { get; }
    public ICommand OpenSpecialOrderCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("brand", out var brand))
            StoreProducts.BrandFilter = Convert.ToString(brand) ?? string.Empty;
        if (query.TryGetValue("categoryName", out var categoryName))
            StoreProducts.CategoryFilter = Convert.ToString(categoryName) ?? string.Empty;
        if (query.TryGetValue("concern", out var concern))
            StoreProducts.ConcernFilter = Convert.ToString(concern) ?? string.Empty;
        if (query.TryGetValue("discounted", out var discounted)
            && bool.TryParse(Convert.ToString(discounted), out var isDiscounted))
            StoreProducts.IsDiscounted = isDiscounted;
        if (query.TryGetValue("query", out var search))
        {
            var searchText = Convert.ToString(search) ?? string.Empty;
            StoreSearch.SearchText = searchText;
            StoreProducts.SearchFilter = searchText;
        }

        _ = StoreProducts.ApplyFiltersAsync();
    }

    private async Task OpenProductAsync(string productId)
    {
        if (!string.IsNullOrWhiteSpace(productId))
            await PageLayout.NavigateToAsync($"/products/{Uri.EscapeDataString(productId)}");
    }
}
