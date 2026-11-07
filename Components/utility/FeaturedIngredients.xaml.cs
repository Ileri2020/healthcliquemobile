using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class FeaturedIngredients : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(FeaturedIngredients), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(FeaturedIngredients), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(FeaturedIngredients), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(FeaturedIngredients), string.Empty);

    public FeaturedIngredients()
    {
        InitializeComponent();
        Ingredients = [];
        RefreshCommand = new Command(async () => await LoadAsync());
        SelectCommand = new Command<DiscoveryIngredientGroup>(ingredient =>
        {
            if (ingredient is not null)
                IngredientSelected?.Invoke(this, ingredient.Name);
        });
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<IngredientCardView> Ingredients { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectCommand { get; }
    public event Action<object?, string>? IngredientSelected;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((FeaturedIngredients)bindable).LoadAsync();

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=product&include=activeIngredients&minimal=true&limit=500");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var products = await response.Content.ReadFromJsonAsync<List<DiscoveryProduct>>(JsonOptions)
                ?? throw new JsonException("Ingredient source API returned an invalid response.");
            var grouped = products
                .SelectMany(product => product.ActiveIngredients
                    .Select(ingredient => new { Name = ingredient.Name.Trim(), product.ImageUrl }))
                .Where(item => item.Name.Length >= 3)
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new DiscoveryIngredientGroup
                {
                    Name = group.Key,
                    ProductCount = group.Count(),
                    Images = group.Select(item => item.ImageUrl).Where(image => !string.IsNullOrWhiteSpace(image)).Distinct().Take(3).ToArray()
                })
                .OrderByDescending(group => group.ProductCount)
                .ThenBy(group => group.Name)
                .Take(24)
                .ToArray();
            Ingredients.Clear();
            foreach (var ingredient in grouped)
                Ingredients.Add(new IngredientCardView(ingredient));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            StatusMessage = exception.Message;
            HasStatus = true;
            RequestFailed?.Invoke(this, exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public sealed class IngredientCardView
    {
        public IngredientCardView(DiscoveryIngredientGroup ingredient)
        {
            Ingredient = ingredient;
            Name = ingredient.Name;
            ImageUrl = ingredient.Images.FirstOrDefault() ?? string.Empty;
            CountText = $"{ingredient.ProductCount} products";
        }
        public DiscoveryIngredientGroup Ingredient { get; }
        public string Name { get; }
        public string ImageUrl { get; }
        public string CountText { get; }
    }
}
