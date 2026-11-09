using System.Windows.Input;
using EEE.Components.Utility;
using EEE.ViewModels;
using EEE.ClientApp;

namespace EEE.ClientApp.Pages;

public partial class HomePage : ContentPage
{
    public HomePage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ContactCommand = new Command(async () => await PageLayout.NavigateToAsync("/contact"));

        HomeHero.NavigationRequested += async (_, path) => await PageLayout.NavigateToAsync(path);
        HomeHero.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        HomeHero.PrescriptionRequested += async (_, _) => await OpenPrescriptionAsync();
        HomeHero.SpecialOrderRequested += async (_, _) => await OpenSpecialOrderAsync();
        HomeHero.ActionFailed += async (_, exception) =>
            await DisplayAlert("Action unavailable", exception.Message, "OK");

        CommonMeds.AddToCartRequested += (_, product) => CommerceCartState.Shared.Add(product);
        DiscountedDeals.NavigationRequested += async (_, path) => await PageLayout.NavigateToAsync(path);
        DiscountedDeals.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        Partners.BrandSelected += async (_, brand) =>
            await OpenStoreAsync($"brand={Uri.EscapeDataString(brand.Name)}");
        HealthConcerns.ConcernSelected += async (_, concern) =>
            await OpenStoreAsync($"concern={Uri.EscapeDataString(concern.Category)}");
        Categories.CategorySelected += async (_, category) =>
            await OpenStoreAsync($"categoryName={Uri.EscapeDataString(category.Name)}");
        Ingredients.IngredientSelected += async (_, ingredient) =>
            await OpenStoreAsync($"query={Uri.EscapeDataString(ingredient)}");
        Products.AddToCartRequested += (_, product) => CommerceCartState.Shared.Add(product);
        Products.ProductSelected += async (_, product) => await OpenProductAsync(product.Id);
        PrescriptionFlow.AddToCartRequested += (_, item) => CommerceCartState.Shared.Add(item);
        SpecialOrderFlow.AddToCartRequested += (_, item) => CommerceCartState.Shared.Add(item);
        PageLayout.CategorySelected += async (_, category) =>
            await OpenStoreAsync($"categoryName={Uri.EscapeDataString(category.Name)}");

        PageLayout.NavigationFailed += async (_, exception) =>
            await DisplayAlert("Navigation unavailable", exception.Message, "OK");
    }

    public ICommand ContactCommand { get; }

    private async Task OpenStoreAsync(string query) =>
        await PageLayout.NavigateToAsync($"/store?{query}");

    private async Task OpenProductAsync(string productId)
    {
        if (!string.IsNullOrWhiteSpace(productId))
            await PageLayout.NavigateToAsync($"/products/{Uri.EscapeDataString(productId)}");
    }

    private async Task OpenPrescriptionAsync()
    {
        await HomeScroll.ScrollToAsync(PrescriptionSection, ScrollToPosition.Start, true);
        PrescriptionFlow.IsOpen = true;
    }

    private async Task OpenSpecialOrderAsync()
    {
        await HomeScroll.ScrollToAsync(PrescriptionSection, ScrollToPosition.Start, true);
        SpecialOrderFlow.IsOpen = true;
    }

}
